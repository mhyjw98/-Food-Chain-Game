# 방 코드 서버

방 코드 → 호스트 주소를 중개하는 Node.js 서버. Unity 쪽 호출부는 `Food_Chain_Game/Assets/Script/Network/RoomHost.cs`, `RoomJoin.cs`.

## 실행

```
npm install
npm start
```

### 자동 재시작 (pm2, 권장)

서버는 요청 처리 중 오류(DB 오류 포함)가 나도 500으로 답하고 계속 돈다. 요청 밖의 예상 못 한 오류에서는 기록을 남기고 종료하므로, 오래 켜 둘 때는 pm2로 실행해 자동으로 다시 켜지게 한다.

```
npm install -g pm2
npm run pm2                  # = pm2 start ecosystem.config.js
pm2 logs room-code-server    # 로그 보기
pm2 restart room-code-server # 코드를 고친 뒤 다시 켜기
pm2 stop room-code-server
pm2 save; pm2 startup        # (선택) PC를 켤 때 자동 실행
```

재시작해도 방 정보는 DB 파일(`rooms.db`)에 남아 있고, 살아 있는 방은 다음 하트비트로 그대로 이어진다.

## 환경 변수

| 이름 | 기본값 | 설명 |
|---|---|---|
| `PORT` | 3000 | 포트 (Unity 쪽은 `StreamingAssets/config.json`의 `ServerPort`, 기본 3000) |
| `ROOM_DB_PATH` | `rooms.db` (이 폴더) | SQLite 파일 경로 |
| `ROOM_TTL_SECONDS` | 60 | 하트비트/출입 보고가 이 시간 동안 없으면 방 삭제. `RoomHost`의 하트비트 간격(기본 20초)보다 충분히 길어야 함 |
| `CLEANUP_INTERVAL_SECONDS` | 30 | 만료된 방 정리 주기 |
| `MAX_PLAYER_LIMIT` | 20 | 등록 가능한 최대 정원 |
| `DEFAULT_GAME_PORT` | 7777 | 방장이 게임 포트를 보내지 않았을 때 쓰는 값 |
| `RATE_WINDOW_SECONDS` | 60 | 요청 횟수를 세는 단위 시간 |
| `RATE_LIMIT_REGISTER` | 10 | 같은 주소의 방 등록 횟수 제한(단위 시간당) |
| `RATE_LIMIT_LOOKUP` | 30 | 같은 주소의 코드 조회 횟수 제한 |
| `RATE_LIMIT_HOST` | 120 | 같은 주소의 출입·하트비트·삭제 횟수 제한 |
| `LOG_VERBOSE` | (없음) | `1`이면 로그에 방 코드·주소를 그대로 남김. 기본은 코드 앞 3글자만, 주소는 생략 |
| `CORS_ORIGIN` | (없음) | 브라우저에서 부를 때만 허용할 출처(쉼표로 여러 개). 기본은 CORS 허용 안 함(Unity 클라이언트는 필요 없음) |
| `TRUST_PROXY` | (없음) | 리버스 프록시 뒤에서 실행할 때만 설정 (예: `1`). 호스트 주소를 `X-Forwarded-For`에서 읽음 |

## API

호스트 전용 요청은 등록 때 받은 토큰을 `X-Room-Token` 헤더로 보낸다. 횟수 제한을 넘으면 모든 요청이 429(`Retry-After` 헤더 포함).

| 호출 | 요청 | 응답 |
|---|---|---|
| `POST /register` | `{ code, maxPlayer, port? }` (port: 방장의 게임 포트) | 200 `{ token }` / 409 코드 중복 / 400 형식 오류 |
| `GET /lookup/{code}` | — | 200 `{ address, port }` / 404 없음·만료 / 423 게임 진행 중 / 403 정원 초과 |
| `POST /comeandgoing` | `{ code, delta }` (±1), 토큰 | 200 / 404 방 없음·토큰 불일치 / 401 토큰 없음 |
| `POST /heartbeat` | `{ code, inGame? }`, 토큰 (inGame: 게임 진행 중 여부) | 200 / 404 / 401 / 400 |
| `DELETE /delete/{code}` | 토큰 | 200 / 404 / 401 |

- 호스트 주소는 요청 본문이 아니라 `/register`를 보낸 쪽의 실제 접속 주소로 저장한다.
- 호스트가 이 서버와 같은 기기(루프백)에서 등록한 경우 `/lookup`은 입장자가 이 서버에 접속할 때 쓴 주소를 돌려준다.
- 방장은 게임 시작·로비 복귀 때 `/heartbeat`에 `inGame`을 바로 보내고, 20초마다 보내는 하트비트에도 같은 값을 싣는다.
- 방 코드는 영문 대문자·숫자 10자.
