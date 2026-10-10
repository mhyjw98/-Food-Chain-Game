# Node.js 방 코드 서버 점검 목록 (2026-10-10 점검 완료, 수정 대기)

방 코드 → 호스트 주소를 중개하는 Node.js 서버(`C:\Users\USER\room-code-server\server.js`, 저장소 밖)를 2026-10-10에 점검했다. 결과는 `review-progress-2.md`의 "Node 서버 리뷰"(N-1~N-5) 참고.
이 문서는 **서버 코드를 받았을 때 바로 리뷰와 보완을 시작하기 위한** 자료다. 근거는 `review-progress-2.md`의 "멀티플레이 구조 점검" 참고.

## 서버 코드를 받으면 진행할 순서

1. 받은 코드를 읽기 전에 아래 "받아야 할 정보"가 빠져 있으면 먼저 요청한다.
2. "클라이언트가 사용하는 API" 표와 실제 라우트를 대조해 경로·메서드·요청 본문·응답 코드가 일치하는지 확인한다.
3. "반드시 확인할 것" → "인원수 집계" → "보안·안정성" 순으로 점검한다. 등급과 기록 형식은 `review-rules.md`를 따르고, 결과는 `review-progress-2.md`에 "Node 서버 리뷰" 섹션으로 추가한다.
4. 치명적 항목은 사용자에게 먼저 보고하고 수정 방향을 확인한다.
5. 서버를 고칠 때는 "서버 수정과 짝이 되는 클라이언트 수정" 표를 보고 **Unity 쪽도 같이** 고친다. 한쪽만 바꾸면 방 생성/입장이 깨진다.
6. 수정 후 확인 항목을 `review-tests-2.md`에 추가한다.

## 받아야 할 정보

- [ ] 서버 소스 전체(라우트 파일, 진입점)와 `package.json`
- [ ] 방 정보 저장 방식: 메모리 / 파일 / DB
- [ ] 실행 환경: 어느 기기에서 도는지, 리버스 프록시(nginx 등)나 클라우드 로드밸런서 뒤에 있는지, 포트
- [ ] 외부 공개 여부(인터넷에서 접근 가능한지, 사설망 전용인지)
- [ ] 서버 코드를 이 저장소에 넣을지(예: `Server/`) 여부 — 넣는다면 `.gitignore`의 `*.json` 규칙이 `package.json`까지 무시하므로 예외 처리 필요

## 클라이언트가 사용하는 API (Unity 쪽 코드 기준)

서버 주소: `http://{config.json의 IP}:3000` (포트 3000은 `RoomHost.cs:15`, `RoomJoin.cs:15`에 하드코딩)

| 호출 | Unity 쪽 위치 | 요청 | 클라이언트가 기대하는 응답 |
|---|---|---|---|
| `POST /register` | `RoomHost.RegisterNewRoom` ← `TitleUI.StartHost` | JSON `{ code, maxPlayer }` | 200 + `{ token }` / 409 = 코드 중복(새 코드로 재시도) |
| `POST /comeandgoing` | `RoomHost.ComeAndGoing` ← `RoomManager` 입장/퇴장 | JSON `{ code, delta }` (입장 +1 / 퇴장 −1) + 토큰 | 200 = 성공 |
| `POST /heartbeat` | `RoomHost.StartHeartbeat` ← `RoomManager.OnStartServer` | JSON `{ code }` + 토큰 | 200 = 성공 / 404 = 방 없음 |
| `GET /lookup/{code}` | `RoomJoin.GetIpFromCode` ← `TitleUI.JoinByCode` | — | 200 + **본문 전체를 IP 문자열로 사용** / 404 = 코드 없음·만료 / 403 = 정원 초과 |
| `DELETE /delete/{code}` | `RoomHost.DeleteRoom` ← 방이 비었을 때, 서버 종료 시 | 토큰 | 200 / 404(이미 삭제됨)도 성공 처리 |

- 위 표는 2026-10-10 수정 후 기준. 토큰은 `X-Room-Token` 헤더. 서버 코드는 저장소 루트 `Server/`에 있고 API 설명은 `Server/README.md`.
- 아래 점검 항목 중 N-1~N-5로 수정된 것과 남은 것은 `review-progress-2.md`의 "Node 서버 리뷰 > 조치 상태 / 남은 것" 참고.

- 방 코드는 클라이언트(`CodeManager.GenerateCode`, 영문 대문자+숫자 10자)가 만들어 보낸다. 중복 검사는 그 클라이언트의 로컬 메모리에서만 한다.
- 호출 시점: 등록은 방 생성 버튼 직후(호스트 시작 전), +1은 `OnServerAddPlayer`(호스트 본인 포함), −1은 입장 처리됐던 연결이 끊길 때, 삭제는 방에 아무도 없게 됐을 때.

## 반드시 확인할 것

- [ ] **A-3. 호스트 주소를 무엇으로 저장하는가.** 클라이언트는 `/register`의 `ip`에 자기 IP가 아니라 **`config.json`의 IP(= 이 Node 서버 주소)** 를 넣어 보낸다(`TitleUI.cs:179`). 서버가 본문의 `ip`를 그대로 저장하면 입장자는 Node 서버 기기로 접속하게 되어, 호스트가 Node 서버와 같은 기기일 때만 동작한다.
  - 서버가 요청자의 실제 주소(`req.ip` 등)를 쓰고 있으면 문제없음. 프록시 뒤라면 `X-Forwarded-For`/`trust proxy` 설정까지 확인
  - 본문 값을 쓰고 있으면 서버가 요청자 주소를 쓰도록 수정(권장)
- [ ] **B-2. 죽은 방 정리.** `/delete`는 호스트가 정상 종료할 때만 호출된다. 창 닫기·크래시·단절 시에는 오지 않으므로 만료 처리(일정 시간 갱신이 없으면 삭제)가 있는지 확인
- [ ] **게임 진행 중 상태.** 서버는 방이 로비인지 게임 중인지 모른다. 게임 중인 방을 조회해도 200이 내려가고, 접속한 뒤에야 게임 서버가 거부한다(클라이언트에는 "게임이 진행 중인 방에는 입장할 수 없습니다." 표시)
- [ ] **`/lookup` 응답 형식.** 클라이언트는 본문 전체를 그대로 `networkAddress`에 넣는다(`RoomJoin.cs:60`). 서버가 JSON이나 따옴표·개행이 붙은 문자열을 보내면 접속 실패

## 인원수 집계

- [ ] `delta` 누적이 0 미만이나 `maxPlayer` 초과로 가지 않도록 서버가 보정하는지
- [ ] 같은 방에 대한 동시 요청에서 집계가 어긋나지 않는지
- [ ] `/lookup`의 403 판정이 `현재 인원 >= maxPlayer` 기준인지 (호스트 본인도 +1로 집계됨)
- [ ] 존재하지 않는 코드에 대한 `/comeandgoing`·`/delete` 처리
- 참고: 클라이언트가 "입장 처리 없이 −1만 보내던" 문제와 서버(Mirror) 측 정원 미검사는 2026-10-09에 수정함

## 보안·안정성

- [ ] **인증 없음.** 방 코드만 알면 누구나 `/delete/{code}`로 방을 지우거나 `/comeandgoing`으로 인원수를 조작할 수 있음 → 등록 시 토큰을 발급해 삭제·집계·하트비트 요청에 요구
- [ ] 삭제가 `GET`(프리페치·크롤러로 호출될 수 있음) → `DELETE` 또는 `POST`
- [ ] `/register`에서 이미 존재하는 코드를 거부하는지(409 등)
- [ ] 입력 검증: `code` 형식·길이, `maxPlayer` 범위, `delta`가 ±1인지, 본문 크기 제한
- [ ] 요청 횟수 제한(코드 무차별 대입, 방 대량 등록)
- [ ] 평문 HTTP → 외부 공개 시 HTTPS 필요 여부
- [ ] 서버 재시작 시 방 목록이 사라지는지와 그때의 동작(호스트는 살아 있는데 코드가 404가 됨)
- [ ] 처리되지 않은 예외로 프로세스가 죽지 않는지, 로그에 IP 등 민감 정보가 남는지

## 서버 수정과 짝이 되는 클라이언트 수정

| 서버에서 바꾸는 것 | Unity 쪽에서 같이 바꿀 것 |
|---|---|
| 호스트 주소를 요청자 IP로 저장 | `TitleUI.StartHost`에서 `ip` 전송 제거(또는 무시됨을 주석), `RoomHost.RoomData` 정리 |
| 하트비트 API 추가(죽은 방 만료) | `RoomHost`에 주기 호출 코루틴 추가(간격은 `[SerializeField]`), 방 삭제 시 중단 |
| 방 상태(로비/게임 중) 추가 | `RoomManager.StartGameDelayed`(게임 시작)와 `OnRoomServerSceneChanged("GameRoom")`(로비 복귀)에서 상태 갱신 호출, `RoomJoin`에 새 응답 코드 처리와 안내 문구 |
| 등록 시 토큰 발급 | 토큰을 `RoomSessionData`에 보관하고 `ComeAndGoing`/`DeleteRoom`/하트비트에 첨부 |
| 삭제 메서드 변경 | `RoomHost.DeleteRoomCoroutine`의 `UnityWebRequest.Get` → `Delete`/`Post` |
| 코드 중복 시 409 | `TitleUI.StartHost`가 실패를 받아 코드를 다시 만들어 재등록 |
| `/lookup` 응답을 JSON으로 | `RoomJoin.GetIpFromCode`의 본문 파싱 |

## 서버와 무관하게 Unity 쪽에 남아 있는 문제 (서버 리뷰 때 함께 수정)

코드로 확인한 것. 서버 응답 규격이 정해져야 제대로 고칠 수 있어 미뤄 둠.

- [ ] **방 등록 실패를 무시하고 호스트를 시작함.** `TitleUI.StartHost`가 `RegisterRoom`을 보내자마자 결과를 기다리지 않고 `RequestStartHost()` 호출. 등록이 실패해도 방은 열리지만 그 코드로는 아무도 들어올 수 없고, 호스트 화면에는 코드가 정상처럼 표시됨 → 등록 성공을 확인한 뒤 호스트 시작, 실패 시 안내
- [ ] **웹 요청에 타임아웃이 없음.** `RoomHost`/`RoomJoin`의 `UnityWebRequest`에 `timeout` 미설정 → 서버가 응답하지 않으면 입장 버튼을 누른 뒤 오래 멈춘 것처럼 보임
- [ ] **방 코드를 URL에 그대로 붙임.** `/lookup/{code}`, `/delete/{code}`에 사용자 입력을 이스케이프 없이 삽입(`RoomJoin.cs:45`) → `UnityWebRequest.EscapeURL` 적용 + 입력 형식(영문 대문자·숫자 10자) 검증
- [ ] `RoomJoin.GetIpFromCode`의 분기 순서: 응답 코드가 0인 네트워크 오류가 마지막 `else if`에서야 걸러짐. 응답 본문 공백 제거(`Trim`) 없음
- [ ] `RoomHost`/`RoomJoin`의 `isConfigValid`가 `Awake`에서 한 번만 계산됨, 포트 3000 하드코딩 → `config.json`에 포트 추가 검토
- [ ] `UnityWebRequest`를 `using`/`Dispose` 없이 사용(네이티브 리소스 누수)
- [ ] `CodeManager`의 `codeToIp` 딕셔너리는 로컬에서만 쓰는 값이라 실질적 의미가 없음(중복 검사는 서버가 해야 함)

## 접속 구조 관련 (서버 코드와 별개로 결정 필요)

- [ ] 플레이어 중 한 명이 호스트가 되는 방식이라, 호스트의 UDP 7777(KCP) 포트가 외부에서 열려 있어야 다른 사람이 접속할 수 있음. 공유기 뒤의 일반 사용자는 포트 포워딩 없이는 호스트가 될 수 없음 → 릴레이 서버 또는 전용 서버 도입 여부
- [ ] 호스트가 나가면 방이 사라짐(호스트 이전 없음), 튕긴 플레이어는 그 판에 재접속 불가 → 유지할지 결정
