# 방 코드 서버

방 코드 → 호스트 주소를 중개하는 Node.js 서버. Unity 쪽 호출부는 `Food_Chain_Game/Assets/Script/Network/RoomHost.cs`, `RoomJoin.cs`.

## 실행

```
npm install
npm start
```

## 환경 변수

| 이름 | 기본값 | 설명 |
|---|---|---|
| `PORT` | 3000 | 포트 (Unity 쪽은 3000 고정) |
| `ROOM_DB_PATH` | `rooms.db` (이 폴더) | SQLite 파일 경로 |
| `ROOM_TTL_SECONDS` | 60 | 하트비트/출입 보고가 이 시간 동안 없으면 방 삭제. `RoomHost`의 하트비트 간격(기본 20초)보다 충분히 길어야 함 |
| `CLEANUP_INTERVAL_SECONDS` | 30 | 만료된 방 정리 주기 |
| `MAX_PLAYER_LIMIT` | 20 | 등록 가능한 최대 정원 |
| `TRUST_PROXY` | (없음) | 리버스 프록시 뒤에서 실행할 때만 설정 (예: `1`). 호스트 주소를 `X-Forwarded-For`에서 읽음 |

## API

호스트 전용 요청은 등록 때 받은 토큰을 `X-Room-Token` 헤더로 보낸다.

| 호출 | 요청 | 응답 |
|---|---|---|
| `POST /register` | `{ code, maxPlayer }` | 200 `{ token }` / 409 코드 중복 / 400 형식 오류 |
| `GET /lookup/{code}` | — | 200 호스트 주소(텍스트) / 404 없음·만료 / 403 정원 초과 |
| `POST /comeandgoing` | `{ code, delta }` (±1), 토큰 | 200 / 404 방 없음·토큰 불일치 / 401 토큰 없음 |
| `POST /heartbeat` | `{ code }`, 토큰 | 200 / 404 / 401 |
| `DELETE /delete/{code}` | 토큰 | 200 / 404 / 401 |

- 호스트 주소는 요청 본문이 아니라 `/register`를 보낸 쪽의 실제 접속 주소로 저장한다.
- 호스트가 이 서버와 같은 기기(루프백)에서 등록한 경우 `/lookup`은 입장자가 이 서버에 접속할 때 쓴 주소를 돌려준다.
- 방 코드는 영문 대문자·숫자 10자.
