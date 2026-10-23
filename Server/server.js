const express = require("express");
const cors = require("cors");
const crypto = require("crypto");
const path = require("path");
const sqlite3 = require("sqlite3").verbose();
const app = express();

// ==========================
// 설정 (환경 변수로 조절)
// ==========================
function intEnv(name, fallback) {
    const value = Number.parseInt(process.env[name], 10);
    return Number.isInteger(value) && value > 0 ? value : fallback;
}

const PORT = intEnv("PORT", 3000);
const DB_PATH = process.env.ROOM_DB_PATH || path.join(__dirname, "rooms.db");
// 이 시간(초) 동안 하트비트/출입 보고가 없는 방은 죽은 방으로 보고 지운다.
// 클라이언트(RoomHost)의 하트비트 간격보다 충분히 길어야 한다
const ROOM_TTL_SECONDS = intEnv("ROOM_TTL_SECONDS", 60);
const CLEANUP_INTERVAL_SECONDS = intEnv("CLEANUP_INTERVAL_SECONDS", 30);
const MAX_PLAYER_LIMIT = intEnv("MAX_PLAYER_LIMIT", 20);
// 방장이 게임 포트를 보내지 않았을 때 쓰는 값 (Unity ConfigManager.DefaultGamePort와 같게)
const DEFAULT_GAME_PORT = intEnv("DEFAULT_GAME_PORT", 7777);

// 요청 횟수 제한: 같은 주소에서 RATE_WINDOW_SECONDS 동안 보낼 수 있는 요청 수
const RATE_WINDOW_SECONDS = intEnv("RATE_WINDOW_SECONDS", 60);
const RATE_LIMIT_REGISTER = intEnv("RATE_LIMIT_REGISTER", 10);
const RATE_LIMIT_LOOKUP = intEnv("RATE_LIMIT_LOOKUP", 30);
const RATE_LIMIT_HOST = intEnv("RATE_LIMIT_HOST", 120);

// 1이면 로그에 방 코드·주소를 그대로 남긴다 (문제를 추적할 때만)
const LOG_VERBOSE = process.env.LOG_VERBOSE === "1";

// 브라우저에서 이 서버를 부를 일이 있을 때만 허용할 출처를 쉼표로 적는다 (예: CORS_ORIGIN=https://example.com).
// Unity 클라이언트는 브라우저가 아니라서 CORS가 필요 없으므로 기본은 허용하지 않는다
const CORS_ORIGIN = process.env.CORS_ORIGIN;

// nginx 등 리버스 프록시 뒤에서 실행할 때만 설정한다 (예: TRUST_PROXY=1).
// 설정하지 않으면 X-Forwarded-For를 믿지 않고 실제 접속 주소를 호스트 주소로 쓴다
if (process.env.TRUST_PROXY) {
    const trustProxy = process.env.TRUST_PROXY;
    app.set("trust proxy", /^\d+$/.test(trustProxy) ? Number(trustProxy) : trustProxy);
}

const CODE_PATTERN = /^[A-Z0-9]{10}$/;
const TOKEN_PATTERN = /^[0-9a-f]{48}$/;
const TOKEN_HEADER = "X-Room-Token";

// Middleware
if (CORS_ORIGIN) {
    app.use(cors({ origin: CORS_ORIGIN.split(",").map((origin) => origin.trim()).filter(Boolean) }));
}
app.use(express.json({ limit: "1kb" }));

// ==========================
// 공용 함수
// ==========================
function nowSeconds() {
    return Math.floor(Date.now() / 1000);
}

// 이 값보다 updated_at이 작은 방은 만료된 방
function expiryThreshold() {
    return nowSeconds() - ROOM_TTL_SECONDS;
}

function isValidCode(code) {
    return typeof code === "string" && CODE_PATTERN.test(code);
}

// 등록 때 발급한 토큰. 방을 만든 호스트만 인원수 변경/삭제/하트비트를 할 수 있게 한다
function getToken(req) {
    const token = req.get(TOKEN_HEADER);
    return typeof token === "string" && TOKEN_PATTERN.test(token) ? token : null;
}

// IPv4 접속이 "::ffff:1.2.3.4" 형태로 들어오는 경우 앞부분을 떼어낸다
function normalizeIp(ip) {
    if (typeof ip !== "string") return "";
    return ip.startsWith("::ffff:") ? ip.slice("::ffff:".length) : ip;
}

function isValidPort(port) {
    return Number.isInteger(port) && port >= 1 && port <= 65535;
}

function isLoopback(ip) {
    return ip === "::1" || ip.startsWith("127.");
}

// 로그에는 방 코드 앞 3글자와 주소 생략만 남긴다 (로그 파일만 보고 남의 방에 들어가거나 방장 주소를 알 수 없게)
function logCode(code) {
    return LOG_VERBOSE ? code : `${String(code).slice(0, 3)}*******`;
}

function logAddress(address) {
    return LOG_VERBOSE ? address : "(주소 생략)";
}

// 같은 주소에서 오는 요청 수를 창(RATE_WINDOW_SECONDS) 단위로 센다. 넘으면 429
function rateLimit(label, limit) {
    const windowMs = RATE_WINDOW_SECONDS * 1000;
    const hits = new Map();

    // 창이 끝난 기록은 주기적으로 지운다 (주소가 계속 쌓이지 않게)
    setInterval(() => {
        const now = Date.now();
        for (const [key, entry] of hits) {
            if (entry.resetAt <= now) hits.delete(key);
        }
    }, windowMs).unref();

    return (req, res, next) => {
        const key = normalizeIp(req.ip) || "unknown";
        const now = Date.now();

        let entry = hits.get(key);
        if (!entry || entry.resetAt <= now) {
            entry = { count: 0, resetAt: now + windowMs };
            hits.set(key, entry);
        }

        entry.count++;
        if (entry.count > limit) {
            if (entry.count === limit + 1) console.log(`⛔ [요청 제한] ${label}: ${logAddress(key)}`);
            res.set("Retry-After", String(Math.ceil((entry.resetAt - now) / 1000)));
            return res.status(429).send("요청이 너무 많습니다");
        }
        next();
    };
}

const limitRegister = rateLimit("등록", RATE_LIMIT_REGISTER);
const limitLookup = rateLimit("조회", RATE_LIMIT_LOOKUP);
// 출입·하트비트·삭제는 방장이 방 하나에 계속 보내는 요청이라 넉넉하게 둔다
const limitHost = rateLimit("방장 요청", RATE_LIMIT_HOST);

// ==========================
// SQLite 연결
// ==========================
const CREATE_TABLE_SQL = `
    CREATE TABLE IF NOT EXISTS rooms (
        code TEXT PRIMARY KEY,
        ip TEXT NOT NULL,
        port INTEGER NOT NULL DEFAULT ${DEFAULT_GAME_PORT},
        in_game INTEGER NOT NULL DEFAULT 0,
        current INTEGER NOT NULL DEFAULT 0,
        max INTEGER NOT NULL,
        token TEXT NOT NULL,
        updated_at INTEGER NOT NULL
    )
`;

const db = new sqlite3.Database(DB_PATH, (err) => {
    if (err) {
        console.error("❌ SQLite 연결 실패:", err.message);
        process.exit(1);
    }
    console.log("✅ SQLite 연결됨");
});

// DB 호출을 Promise로 감싼다. 요청 처리 함수가 async라서, 안에서 던져진 예외(DB 오류 포함)는
// Express 5가 아래 오류 처리로 넘겨 500으로 답한다. 예전처럼 콜백 안 예외로 프로세스가 죽지 않는다
function dbRun(sql, params = []) {
    return new Promise((resolve, reject) => {
        db.run(sql, params, function (err) {
            if (err) reject(err);
            else resolve({ changes: this.changes });
        });
    });
}

function dbGet(sql, params = []) {
    return new Promise((resolve, reject) => {
        db.get(sql, params, (err, row) => (err ? reject(err) : resolve(row)));
    });
}

function dbAll(sql, params = []) {
    return new Promise((resolve, reject) => {
        db.all(sql, params, (err, rows) => (err ? reject(err) : resolve(rows)));
    });
}

// rooms 테이블 준비. 토큰/갱신 시각이 없는 예전 구조의 테이블은 지우고 새로 만든다
// (예전 방은 주인을 확인할 방법이 없어 넘겨받을 수 없다)
async function initDb() {
    const columns = await dbAll("PRAGMA table_info(rooms)");

    const isOldSchema = columns.length > 0 && !columns.some((column) => column.name === "token");
    if (isOldSchema) {
        console.log("📌 예전 구조의 rooms 테이블을 지우고 새로 만듭니다");
        await dbRun("DROP TABLE rooms");
        await dbRun(CREATE_TABLE_SQL);
        return;
    }

    if (columns.length === 0) {
        await dbRun(CREATE_TABLE_SQL);
        return;
    }

    // 나중에 생긴 칸이 없는 테이블은 칸만 추가한다 (기존 방은 기본값)
    const added = [
        ["port", `INTEGER NOT NULL DEFAULT ${DEFAULT_GAME_PORT}`],
        ["in_game", "INTEGER NOT NULL DEFAULT 0"],
    ].filter(([name]) => !columns.some((column) => column.name === name));

    for (const [name, definition] of added) {
        console.log(`📌 rooms 테이블에 ${name} 칸을 추가합니다`);
        await dbRun(`ALTER TABLE rooms ADD COLUMN ${name} ${definition}`);
    }
}

// 만료된 방 정리 (호스트가 창 닫기·크래시로 삭제 요청 없이 사라진 경우)
async function cleanupExpiredRooms() {
    try {
        const { changes } = await dbRun("DELETE FROM rooms WHERE updated_at < ?", [expiryThreshold()]);
        if (changes > 0) console.log(`🧹 [만료 정리] ${changes}개 방 삭제`);
    } catch (err) {
        console.error("❌ [만료 정리 실패]", err.message);
    }
}


// ==========================
// 테스트용 엔드포인트
// ==========================
app.get("/test", (req, res) => {
    res.send("OK");
});


// ==========================
// 방 코드 등록
// ==========================
app.post("/register", limitRegister, async (req, res) => {
    const { code, maxPlayer } = req.body ?? {};
    // 게임 포트는 선택 값 (예전 클라이언트는 보내지 않음). 같은 기기에서 여러 방장이 방을 열 때 서로 다른 포트를 쓴다
    const port = req.body?.port === undefined || req.body?.port === 0 ? DEFAULT_GAME_PORT : req.body.port;

    // 요청 값은 검증을 통과한 뒤에만 로그에 남긴다 (임의 문자열이 로그에 섞이지 않게)
    if (!isValidCode(code)) {
        return res.status(400).send("code 형식이 올바르지 않습니다");
    }
    if (!Number.isInteger(maxPlayer) || maxPlayer < 1 || maxPlayer > MAX_PLAYER_LIMIT) {
        return res.status(400).send("maxPlayer 값이 올바르지 않습니다");
    }
    if (!isValidPort(port)) {
        return res.status(400).send("port 값이 올바르지 않습니다");
    }

    console.log(`\n[POST /register] code: ${logCode(code)}, maxPlayer: ${maxPlayer}, port: ${port}`);

    // 호스트 주소는 요청 본문이 아니라 실제 접속 주소를 쓴다
    const hostIp = normalizeIp(req.ip);
    if (!hostIp) {
        return res.status(400).send("요청자 주소를 확인할 수 없습니다");
    }

    const token = crypto.randomBytes(24).toString("hex");

    // 같은 코드의 만료된 방이 남아 있으면 먼저 지운다.
    // 살아 있는 방이면 아래 INSERT가 PRIMARY KEY 충돌로 실패해 409가 된다 (덮어쓰지 않음)
    await dbRun("DELETE FROM rooms WHERE code = ? AND updated_at < ?", [code, expiryThreshold()]);

    try {
        await dbRun(
            `INSERT INTO rooms (code, ip, port, current, max, token, updated_at)
             VALUES (?, ?, ?, 0, ?, ?, ?)`,
            [code, hostIp, port, maxPlayer, token, nowSeconds()]
        );
    } catch (err) {
        if (err.code === "SQLITE_CONSTRAINT") {
            console.log(`❌ [등록 실패] 이미 사용 중인 코드: ${logCode(code)}`);
            return res.status(409).send("이미 사용 중인 코드");
        }
        throw err;
    }

    console.log(`✅ [등록 성공] ${logCode(code)} → ${logAddress(hostIp)}:${port}`);
    res.json({ token });
});


// ==========================
// 방 코드로 IP 조회
// ==========================
app.get("/lookup/:code", limitLookup, async (req, res) => {
    const code = req.params.code;

    if (!isValidCode(code)) {
        return res.status(404).send("코드 없음");
    }

    console.log(`\n[GET /lookup/${logCode(code)}] 요청 도착`);

    const row = await dbGet(
        "SELECT ip, port, in_game, current, max FROM rooms WHERE code = ? AND updated_at >= ?",
        [code, expiryThreshold()]
    );

    if (!row) {
        console.log("❌ [조회 실패] 존재하지 않는 코드:", logCode(code));
        return res.status(404).send("코드 없음");
    }

    // 게임 중인 방은 접속해도 게임 서버가 거부하므로 여기서 바로 막는다 (Unity RoomJoin이 안내 문구로 바꿈)
    if (row.in_game) {
        console.log("❌ [조회 실패] 게임 진행 중");
        return res.status(423).send("게임 진행 중");
    }

    if (row.current >= row.max) {
        console.log("❌ [조회 실패] 방 가득 참");
        return res.status(403).send("방 가득 참");
    }

    // 호스트가 이 서버와 같은 기기에서 등록했다면 저장된 주소는 루프백이라 다른 기기에서는 쓸 수 없다.
    // 그 경우 입장자가 이 서버에 접속할 때 쓴 주소가 곧 호스트 기기의 주소다
    const address = isLoopback(row.ip) ? req.hostname : row.ip;

    const port = isValidPort(row.port) ? row.port : DEFAULT_GAME_PORT;

    // Unity RoomJoin.TryParseLookup이 읽는 형식
    console.log(`📡 [조회 성공] ${logCode(code)} → 주소 전달: ${logAddress(address)}:${port}`);
    res.json({ address, port });
});


// ==========================
// 방 출입 (인원 증가/감소)
// ==========================
app.post("/comeandgoing", limitHost, async (req, res) => {
    const { code, delta } = req.body ?? {};
    const token = getToken(req);

    if (!isValidCode(code) || (delta !== 1 && delta !== -1)) {
        return res.status(400).send("code 또는 delta 값이 올바르지 않습니다");
    }
    if (!token) {
        return res.status(401).send("토큰이 필요합니다");
    }

    console.log(`\n[POST /comeandgoing] code: ${logCode(code)}, delta: ${delta}`);

    // 읽고 나서 쓰면 동시 요청에서 집계가 어긋나므로 한 문장으로 갱신한다 (0 ~ max 범위로 보정)
    const { changes } = await dbRun(
        `UPDATE rooms
         SET current = MAX(0, MIN("max", current + ?)), updated_at = ?
         WHERE code = ? AND token = ?`,
        [delta, nowSeconds(), code, token]
    );

    if (changes === 0) {
        console.log("❌ [방 출입 실패] 방이 없거나 토큰 불일치:", logCode(code));
        return res.status(404).send("방이 없거나 권한이 없습니다");
    }

    console.log(`👥 [방 출입] code: ${logCode(code)}, delta: ${delta}`);
    if (delta > 0) return res.sendStatus(200);

    // 마지막 사람이 나가 0명이 되면 방 삭제
    const deleted = await dbRun("DELETE FROM rooms WHERE code = ? AND token = ? AND current <= 0", [code, token]);
    if (deleted.changes > 0) console.log(`🗑️ [방 삭제 완료] code: ${logCode(code)}`);
    res.sendStatus(200);
});


// ==========================
// 하트비트 (방이 살아 있음을 알림)
// ==========================
// inGame(선택): 방장이 게임 시작·로비 복귀 때 바로 보내고, 주기적인 하트비트에도 실어 보낸다 (한 번 실패해도 다음 하트비트로 맞춰짐)
app.post("/heartbeat", limitHost, async (req, res) => {
    const { code, inGame } = req.body ?? {};
    const token = getToken(req);

    if (!isValidCode(code)) {
        return res.status(400).send("code 형식이 올바르지 않습니다");
    }
    if (inGame !== undefined && typeof inGame !== "boolean") {
        return res.status(400).send("inGame 값이 올바르지 않습니다");
    }
    if (!token) {
        return res.status(401).send("토큰이 필요합니다");
    }

    const { changes } = inGame === undefined
        ? await dbRun("UPDATE rooms SET updated_at = ? WHERE code = ? AND token = ?", [nowSeconds(), code, token])
        : await dbRun("UPDATE rooms SET updated_at = ?, in_game = ? WHERE code = ? AND token = ?", [nowSeconds(), inGame ? 1 : 0, code, token]);

    if (changes === 0) {
        return res.status(404).send("방이 없거나 권한이 없습니다");
    }
    res.sendStatus(200);
});


// ==========================
// 방 삭제
// ==========================
app.delete("/delete/:code", limitHost, async (req, res) => {
    const code = req.params.code;
    const token = getToken(req);

    if (!isValidCode(code)) {
        return res.status(404).send("해당 코드 없음");
    }
    if (!token) {
        return res.status(401).send("토큰이 필요합니다");
    }

    console.log(`\n[DELETE /delete/${logCode(code)}] 요청 도착`);

    const { changes } = await dbRun("DELETE FROM rooms WHERE code = ? AND token = ?", [code, token]);

    if (changes === 0) {
        console.log(`❌ [삭제 실패] 방이 없거나 토큰 불일치: ${logCode(code)}`);
        return res.status(404).send("해당 코드 없음");
    }

    console.log(`🗑️ [삭제 성공] 방 코드: ${logCode(code)}`);
    res.sendStatus(200);
});


// ==========================
// 오류 처리 (잘못된 JSON, 본문 크기 초과, DB 오류 등) — 스택을 응답에 싣지 않는다
// ==========================
app.use((err, req, res, next) => {
    if (res.headersSent) return next(err);

    const isClientError = err.status >= 400 && err.status < 500;
    if (!isClientError) console.error(`❌ [처리되지 않은 오류] ${req.method} ${req.path.split("/").slice(0, 2).join("/")}`, err);

    res.status(isClientError ? err.status : 500).send(isClientError ? "잘못된 요청" : "서버 오류");
});


// ==========================
// 프로세스 수준 오류·종료
// ==========================
// 요청 밖에서 난 예외는 상태를 장담할 수 없으므로 기록하고 종료한다. pm2 등으로 실행하면 바로 다시 켜진다 (README 참고).
// 방 정보는 DB 파일에 있고, 재시작 시 만료 시계를 다시 세므로 살아 있는 방은 유지된다
process.on("uncaughtException", (err) => {
    console.error("❌ [치명적 오류] 서버를 종료합니다:", err);
    process.exit(1);
});

process.on("unhandledRejection", (reason) => {
    console.error("❌ [처리되지 않은 Promise 오류]", reason);
});

let server = null;
function shutdown(signal) {
    console.log(`\n${signal} 받음 — 서버를 종료합니다`);
    const closeDb = () => db.close(() => process.exit(0));
    if (server) server.close(closeDb);
    else closeDb();
    // 연결이 남아 닫히지 않으면 강제로 끝낸다
    setTimeout(() => process.exit(0), 5000).unref();
}
process.on("SIGINT", () => shutdown("SIGINT"));
process.on("SIGTERM", () => shutdown("SIGTERM"));


// ==========================
// 서버 시작
// ==========================
(async () => {
    try {
        await initDb();
        console.log("📌 rooms 테이블 준비 완료");
    } catch (err) {
        console.error("❌ 테이블 준비 오류:", err.message);
        process.exit(1);
    }

    // 서버가 꺼져 있던 동안에는 하트비트를 받을 수 없었으므로, 남아 있는 방의 만료 시계를 지금부터 다시 센다.
    // 그러지 않으면 재시작 직후 살아 있는 방까지 전부 만료로 지워진다 (죽은 방은 TTL 뒤에 정리됨)
    try {
        await dbRun("UPDATE rooms SET updated_at = ?", [nowSeconds()]);
    } catch (refreshErr) {
        console.error("❌ 방 갱신 시각 초기화 실패:", refreshErr.message);
    }

    setInterval(cleanupExpiredRooms, CLEANUP_INTERVAL_SECONDS * 1000);

    server = app.listen(PORT, "0.0.0.0", () => {
        console.log(`\nRoom Code 서버 실행 중 (포트 ${PORT}, 외부 접속 허용)`);
    });
})();
