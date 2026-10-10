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
app.use(cors());
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

function isLoopback(ip) {
    return ip === "::1" || ip.startsWith("127.");
}

// ==========================
// SQLite 연결
// ==========================
const CREATE_TABLE_SQL = `
    CREATE TABLE IF NOT EXISTS rooms (
        code TEXT PRIMARY KEY,
        ip TEXT NOT NULL,
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

// rooms 테이블 준비. 토큰/갱신 시각이 없는 예전 구조의 테이블은 지우고 새로 만든다
// (예전 방은 주인을 확인할 방법이 없어 넘겨받을 수 없다)
function initDb(done) {
    db.all("PRAGMA table_info(rooms)", (err, columns) => {
        if (err) return done(err);

        const isOldSchema = columns.length > 0 && !columns.some((column) => column.name === "token");
        if (!isOldSchema) return db.run(CREATE_TABLE_SQL, done);

        console.log("📌 예전 구조의 rooms 테이블을 지우고 새로 만듭니다");
        db.run("DROP TABLE rooms", (dropErr) => {
            if (dropErr) return done(dropErr);
            db.run(CREATE_TABLE_SQL, done);
        });
    });
}

// 만료된 방 정리 (호스트가 창 닫기·크래시로 삭제 요청 없이 사라진 경우)
function cleanupExpiredRooms() {
    db.run("DELETE FROM rooms WHERE updated_at < ?", [expiryThreshold()], function (err) {
        if (err) return console.error("❌ [만료 정리 실패]", err.message);
        if (this.changes > 0) console.log(`🧹 [만료 정리] ${this.changes}개 방 삭제`);
    });
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
app.post("/register", (req, res) => {
    const { code, maxPlayer } = req.body ?? {};

    // 요청 값은 검증을 통과한 뒤에만 로그에 남긴다 (임의 문자열이 로그에 섞이지 않게)
    if (!isValidCode(code)) {
        return res.status(400).send("code 형식이 올바르지 않습니다");
    }
    if (!Number.isInteger(maxPlayer) || maxPlayer < 1 || maxPlayer > MAX_PLAYER_LIMIT) {
        return res.status(400).send("maxPlayer 값이 올바르지 않습니다");
    }

    console.log(`\n[POST /register] code: ${code}, maxPlayer: ${maxPlayer}`);

    // 호스트 주소는 요청 본문이 아니라 실제 접속 주소를 쓴다
    const hostIp = normalizeIp(req.ip);
    if (!hostIp) {
        return res.status(400).send("요청자 주소를 확인할 수 없습니다");
    }

    const token = crypto.randomBytes(24).toString("hex");

    // 같은 코드의 만료된 방이 남아 있으면 먼저 지운다.
    // 살아 있는 방이면 아래 INSERT가 PRIMARY KEY 충돌로 실패해 409가 된다 (덮어쓰지 않음)
    db.run("DELETE FROM rooms WHERE code = ? AND updated_at < ?", [code, expiryThreshold()], (deleteErr) => {
        if (deleteErr) {
            console.error("❌ [등록 실패] DB 오류:", deleteErr.message);
            return res.status(500).send("DB 오류");
        }

        const sql = `
            INSERT INTO rooms (code, ip, current, max, token, updated_at)
            VALUES (?, ?, 0, ?, ?, ?)
        `;

        db.run(sql, [code, hostIp, maxPlayer, token, nowSeconds()], (err) => {
            if (err) {
                if (err.code === "SQLITE_CONSTRAINT") {
                    console.log(`❌ [등록 실패] 이미 사용 중인 코드: ${code}`);
                    return res.status(409).send("이미 사용 중인 코드");
                }
                console.error("❌ [등록 실패] DB 오류:", err.message);
                return res.status(500).send("DB 오류");
            }

            console.log(`✅ [등록 성공] ${code} → ${hostIp}`);
            res.json({ token });
        });
    });
});


// ==========================
// 방 코드로 IP 조회
// ==========================
app.get("/lookup/:code", (req, res) => {
    const code = req.params.code;

    if (!isValidCode(code)) {
        return res.status(404).send("코드 없음");
    }

    console.log(`\n[GET /lookup/${code}] 요청 도착`);

    const sql = "SELECT ip, current, max FROM rooms WHERE code = ? AND updated_at >= ?";

    db.get(sql, [code, expiryThreshold()], (err, row) => {
        if (err) {
            console.error("❌ [조회 실패] DB 오류:", err.message);
            return res.status(500).send("DB 오류");
        }

        if (!row) {
            console.log("❌ [조회 실패] 존재하지 않는 코드:", code);
            return res.status(404).send("코드 없음");
        }

        if (row.current >= row.max) {
            console.log("❌ [조회 실패] 방 가득 참");
            return res.status(403).send("방 가득 참");
        }

        // 호스트가 이 서버와 같은 기기에서 등록했다면 저장된 주소는 루프백이라 다른 기기에서는 쓸 수 없다.
        // 그 경우 입장자가 이 서버에 접속할 때 쓴 주소가 곧 호스트 기기의 주소다
        const address = isLoopback(row.ip) ? req.hostname : row.ip;

        console.log(`📡 [조회 성공] ${code} → IP 전달: ${address}`);
        res.type("text/plain").send(address);
    });
});


// ==========================
// 방 출입 (인원 증가/감소)
// ==========================
app.post("/comeandgoing", (req, res) => {
    const { code, delta } = req.body ?? {};
    const token = getToken(req);

    if (!isValidCode(code) || (delta !== 1 && delta !== -1)) {
        return res.status(400).send("code 또는 delta 값이 올바르지 않습니다");
    }
    if (!token) {
        return res.status(401).send("토큰이 필요합니다");
    }

    console.log(`\n[POST /comeandgoing] code: ${code}, delta: ${delta}`);

    // 읽고 나서 쓰면 동시 요청에서 집계가 어긋나므로 한 문장으로 갱신한다 (0 ~ max 범위로 보정)
    const sql = `
        UPDATE rooms
        SET current = MAX(0, MIN("max", current + ?)), updated_at = ?
        WHERE code = ? AND token = ?
    `;

    db.run(sql, [delta, nowSeconds(), code, token], function (err) {
        if (err) {
            console.error("❌ [방 출입 실패]", err.message);
            return res.status(500).send("DB 업데이트 오류");
        }
        if (this.changes === 0) {
            console.log("❌ [방 출입 실패] 방이 없거나 토큰 불일치:", code);
            return res.status(404).send("방이 없거나 권한이 없습니다");
        }

        console.log(`👥 [방 출입] code: ${code}, delta: ${delta}`);
        if (delta > 0) return res.sendStatus(200);

        // 마지막 사람이 나가 0명이 되면 방 삭제
        db.run("DELETE FROM rooms WHERE code = ? AND token = ? AND current <= 0", [code, token], function (deleteErr) {
            if (deleteErr) {
                console.error("❌ [방 삭제 실패]", deleteErr.message);
                return res.status(500).send("DB 삭제 오류");
            }
            if (this.changes > 0) console.log(`🗑️ [방 삭제 완료] code: ${code}`);
            res.sendStatus(200);
        });
    });
});


// ==========================
// 하트비트 (방이 살아 있음을 알림)
// ==========================
app.post("/heartbeat", (req, res) => {
    const { code } = req.body ?? {};
    const token = getToken(req);

    if (!isValidCode(code)) {
        return res.status(400).send("code 형식이 올바르지 않습니다");
    }
    if (!token) {
        return res.status(401).send("토큰이 필요합니다");
    }

    db.run("UPDATE rooms SET updated_at = ? WHERE code = ? AND token = ?", [nowSeconds(), code, token], function (err) {
        if (err) {
            console.error("❌ [하트비트 실패]", err.message);
            return res.status(500).send("DB 오류");
        }
        if (this.changes === 0) {
            return res.status(404).send("방이 없거나 권한이 없습니다");
        }
        res.sendStatus(200);
    });
});


// ==========================
// 방 삭제
// ==========================
app.delete("/delete/:code", (req, res) => {
    const code = req.params.code;
    const token = getToken(req);

    if (!isValidCode(code)) {
        return res.status(404).send("해당 코드 없음");
    }
    if (!token) {
        return res.status(401).send("토큰이 필요합니다");
    }

    console.log(`\n[DELETE /delete/${code}] 요청 도착`);

    db.run("DELETE FROM rooms WHERE code = ? AND token = ?", [code, token], function (err) {
        if (err) {
            console.error("❌ [삭제 실패]", err.message);
            return res.status(500).send("DB 오류");
        }

        if (this.changes === 0) {
            console.log(`❌ [삭제 실패] 방이 없거나 토큰 불일치: ${code}`);
            return res.status(404).send("해당 코드 없음");
        }

        console.log(`🗑️ [삭제 성공] 방 코드: ${code}`);
        res.sendStatus(200);
    });
});


// ==========================
// 오류 처리 (잘못된 JSON, 본문 크기 초과 등) — 스택을 응답에 싣지 않는다
// ==========================
app.use((err, req, res, next) => {
    if (res.headersSent) return next(err);

    const isClientError = err.status >= 400 && err.status < 500;
    if (!isClientError) console.error("❌ [처리되지 않은 오류]", err);

    res.status(isClientError ? err.status : 500).send(isClientError ? "잘못된 요청" : "서버 오류");
});


// ==========================
// 서버 시작
// ==========================
initDb((err) => {
    if (err) {
        console.error("❌ 테이블 준비 오류:", err.message);
        process.exit(1);
    }
    console.log("📌 rooms 테이블 준비 완료");

    // 서버가 꺼져 있던 동안에는 하트비트를 받을 수 없었으므로, 남아 있는 방의 만료 시계를 지금부터 다시 센다.
    // 그러지 않으면 재시작 직후 살아 있는 방까지 전부 만료로 지워진다 (죽은 방은 TTL 뒤에 정리됨)
    db.run("UPDATE rooms SET updated_at = ?", [nowSeconds()], (refreshErr) => {
        if (refreshErr) console.error("❌ 방 갱신 시각 초기화 실패:", refreshErr.message);

        setInterval(cleanupExpiredRooms, CLEANUP_INTERVAL_SECONDS * 1000);

        app.listen(PORT, "0.0.0.0", () => {
            console.log(`\nRoom Code 서버 실행 중 (포트 ${PORT}, 외부 접속 허용)`);
        });
    });
});
