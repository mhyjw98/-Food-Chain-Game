# 리뷰 진행 기록 (요약)

세션 1(네트워크 & 방 구조) 검토 및 이후 실제 플레이테스트에서 나온 버그들의 **현재 상태**만 정리. 상세 원인 분석/삽질 과정은 git 이력 또는 이전 버전 참고.

---

## 세션 1 리뷰 이슈 — 처리 현황

| 항목 | 상태 |
|---|---|
| `OnServerDisconnect`가 1명만 나가도 `RoomSessionData.Reset()` 호출 | ✅ 수정 — 방에 아무도 안 남았을 때만 리셋 + `roomHost.DeleteRoom()` 호출하도록 변경 |
| `ServerAssignCharacters` do-while 무한루프 가능성 | ✅ 수정 — 풀 소진 시 경고 로그 + 중복 배정으로 대체 |
| `_isCleaningUp` 플래그가 재설정 안 됨 | ✅ 수정 — `OnStartServer`/`OnStartClient`에서 재무장 |
| 색상 잠금이 퇴장 시 안 풀림 | ✅ 수정 — `RpcColorReleased` RPC로 명시적 해제 |
| `PreviousHostId` 죽은 코드 | ✅ 제거함 (실제 호스트 마이그레이션 기능은 미구현 — 필요 시 별도 논의) |
| `CmdSetColor` 등 null/범위 체크 누락 4건 | ✅ 전부 방어 코드 추가 |
| `chatManagerPrefab` 미사용 의심 | ⏸ 보류 (ChatManager가 씬에 기본 배치돼 있어 당장 문제 없음) |
| `RoomManager` God 클래스 경향, 네이밍/중복 등 제안 항목들 | ⏸ 미처리 (우선순위 낮음, 필요 시 별도 리팩토링 세션) |

---

## 플레이테스트에서 발견된 버그

### 1. 로비 복귀 시 RoomPlayer가 안 보이던 문제 — ✅ 해결 확인됨
- **원인:** 표시/숨김을 Mirror의 클라이언트 훅(`OnClientEnterRoom`)에 맡겼는데, 이 훅이 `NetworkClient.isConnected`가 false인 타이밍엔 호출을 건너뛰어서 로비 복귀 시 재호출되지 않았음(콘솔 로그로 확인).
- **수정:** 서버가 씬 전환을 확실히 감지하는 시점(`RoomManager.OnRoomServerSceneChanged`)에서 `RoomPlayer.RpcSetRoomVisualsActive()` RPC로 직접 표시/숨김 + 위치 재배치를 지시하도록 변경.
- 사용자 테스트로 정상 동작 확인됨.

### 2. 네트워크 지연(호스트 랙) 이후 방 생성이 안 되던 문제 — 원인 2개로 분리
- **2-A (진짜 버그, ✅ 수정함): 호스트가 끊긴 클라이언트를 영영 감지 못함**
  `RoomManager.prefab`의 `disconnectInactiveConnections`가 꺼져 있어서, 유일한 감지 수단인 KCP 트랜스포트 자체 타임아웃(10초)만 있었는데 그마저 안 걸렸음(콘솔 로그로 "퇴장" 처리가 한 번도 안 찍히는 것 확인). → `disconnectInactiveConnections: 1` 활성화, `disconnectInactiveTimeout`을 60→15초로 단축해서 Mirror 자체 감지 로직이 독립적인 안전망 역할을 하도록 함.
- **2-B (버그 아님, 테스트 환경 한계): `SocketException: 포트 이미 사용 중`**
  원본 프로젝트와 ParrelSync 클론이 `ProjectSettings`(포트 설정 포함)를 공유해서 **항상 같은 포트(7777)** 를 씀. 호스트의 방은 한 명이 끊겼다고 자동으로 닫히지 않는 게 정상이라, 같은 기기에서 다른 인스턴스가 새로 호스팅하려 하면 항상 충돌함. **실제 배포(서로 다른 기기)에서는 발생하지 않는 문제.** 같은 기기에서 두 호스트를 동시에 테스트해야 한다면 클론 쪽 포트를 분리하는 별도 작업 필요(현재 미적용).
  - 보조 조치: `NetworkCoordinator.StartHost/StartClient`에 실패 시 0.5초 간격 최대 5회 재시도 로직 추가(일시적 지연 상황 대비, 포트 충돌 자체는 해결 못함).

### 3. TMP 폰트 깨짐 (`�` 대체 문자 경고 다량 발생) — ✅ 수정함
- **원인:** `Assets/Script` 하위 62개 파일이 UTF-8이 아닌 CP949로 저장되어 있어, 컴파일러가 한글을 `�`로 잘못 디코딩해서 문자열 상수에 그대로 박힘.
- **수정:** 62개 파일 전체 CP949 → UTF-8(BOM) 변환.
- **부수 발견:** `Resource/config.json`이 `Assets/` 밖에 있어 ParrelSync 클론·빌드 출력에 자동 포함 안 됨 → 클론에서 `IP: null`인 깨진 설정이 생성돼 Node 서버 통신 전체가 실패하는 문제로 이어짐(문제 2와 겹치는 부분 있었음). `Assets/StreamingAssets/config.json`으로 이전하고, 설정 누락/비어있음 시 조용히 깨진 기본값을 만드는 대신 명확한 에러를 남기도록 `ConfigManager`/`RoomHost`/`RoomJoin` 수정.

### 4. 생존자 0명일 때 게임 종료 조건 — ✅ 추가함
- 기존엔 "포식자 전멸"만 종료 조건이었음. `GameMamager.CheckGameOver(bool wasPredator)`로 바꿔서 모든 죽음마다 체크하고, "포식자 전멸" 또는 "생존자 0명" 중 하나라도 만족하면 종료되도록 수정.

---

---

## 세션 2 — 플레이어 이동 & 시야(POV) 시스템 리뷰 [2026-09-18]

**범위:** `PlayerMove.cs`, `FollowCamera.cs`, `VisionAnchor.cs`, `LocalVisionLight.cs`, `FOVMaskController2D.cs`, `VisionUIController.cs`, `VisionSystem.cs` (+ `GamePlayer.prefab` 대조 확인)

### 경고
- **이동이 완전히 클라이언트 권위.** `GamePlayer.prefab`의 NetworkTransform이 `syncDirection: ClientToServer`로 확인됨 — 서버가 위치를 검증하지 않아 스피드핵/텔레포트 치트에 취약(review-plan 세션2 점검 포인트와 일치).
- **`LocalVisionLight.TransitionToVision`에 `fovMask`/`fovMask.GetComponent<VisionUIController>()` null 체크 없음.** 설정 누락 시 낮/밤 전환 시점에 NRE.
- **시야 반경 값(11/6/8) 중복.** `GameMamager.GetVisionForAnimal`과 `VisionUIController.IsHeadVisible`에 각각 하드코딩. 후자는 다람쥐/포식자 예외를 반영 안 해서, 실제 렌더링되는 시야(FOV 마스크)와 UI 가시성 판정(이름표 등)이 서로 어긋날 수 있음 — 다람쥐가 밤에도 시야 11인데 UI 판정은 6으로 계산되는 식.

### 제안
- **`VisionSystem.cs`는 씬/프리팹 어디에도 연결되지 않은 죽은 코드**(GUID로 확인). `GameMamager`+`LocalVisionLight`가 하는 낮/밤 전환과 로직이 겹쳐 보여 헷갈리기 쉬움 — 제거하거나 용도 확인 필요.
- `FOVMaskController2D.BuildMask()`가 0.05초마다 최대 240회 레이캐스트 — 의도된 성능/품질 트레이드오프로 보이나 참고용으로 기록.
- `PlayerMove.cs`의 `using Unity.VisualScripting;` 등 미사용으로 보이는 import 정리 여지.

### 조치 상태
- 전부 **미조치** — 사용자 확인/우선순위 결정 대기 중

---

## 아직 확인 필요한 것

- [ ] 클라이언트가 끊긴 뒤 15초 정도 기다리면 호스트 쪽에 퇴장 처리(메시지, 인원 수 감소)가 정상적으로 되는지
- [ ] 피식자만 전멸 / 포식자만 전멸 각각의 경우 게임이 정상 종료되는지, 기존 승리 조건 판정에 영향 없는지
- [ ] 새로 빌드한 빌드 폴더에서 `Assets/StreamingAssets/config.json`이 정상적으로 포함되어 있는지 (수동으로 복사할 필요 없어짐)
- [ ] 콘솔에 `[ConfigManager]`/`[RoomHost]`/`[RoomJoin]` 에러가 안 뜨는지
- [ ] (선택) 같은 기기에서 두 호스트를 동시에 테스트할 일이 많다면, 클론 전용 포트 분리 작업 진행 여부 결정
