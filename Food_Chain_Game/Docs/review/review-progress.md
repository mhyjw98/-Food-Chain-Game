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

## [2026-10-09] 세션 3 — 상호작용 & 조사(수사) 오브젝트 리뷰

**범위:** `Scanner.cs`, `InvestigationObject.cs`, `InvestigationTrigger.cs`, `MissionTrigger.cs`, `MissionObject.cs`, `InvestigationLog.cs`, `InvestigationRecord.cs` (+ 호출부 대조: `GamePlayer.cs`, `PlayerMove.cs`, `GamePlayUI.cs`, `GameMamager.cs`, `GamePlay.unity`/`GamePlayer.prefab`)

### 치명적 (전부 서버 검증 누락 = 치팅 가능) → 조치 상태: 논의 필요
- [GameMamager.cs:466] `RequestPlayerType`가 `requiresAuthority = false`에 검증이 전혀 없음. 조사 오브젝트 근처인지, 조력자(`canScan`)인지, 살아있는지, 5초 진행을 했는지 모두 클라이언트만 확인 → 아무 클라이언트나 임의 netId로 호출하면 `animalType`/`isPredator`를 즉시 받음.
- [GamePlayer.cs:181] `CmdScan`이 `canScan`, `scanCount < maxScanCount`, 탐색 페이즈, 생존 여부를 서버에서 확인하지 않음(횟수 제한은 `ContextMenuUI`의 버튼 비활성화뿐). `NetworkServer.spawned[targetNetId]` 인덱서라 잘못된 netId면 서버 예외.
- [GamePlayer.cs:598] `CmdCompleteMission`이 클라이언트 선언만으로 완료 처리. 슬롯이 없거나 이미 완료여도 핸들러는 그대로 실행됨 → 전갈이 `Scorpion_Poison` 완료를 직접 보내면 미션 없이 독(역공) 획득. `CmdStartMission`/`CmdCancelMission` 포함 생존·거리·진행 상태 검증 없음.
- [GamePlayer.cs:389] `CmdAttack`에 거리 검증 없음. 사거리(`scanRadius`)는 클라이언트 `Scanner`만 판단 → 맵 어디서든 임의 netId 공격 가능. (상세는 세션 4)
- [GamePlayer.cs:26-29, Corpse.cs:9] `animalType`/`predatorType`/`isPredator`와 시체의 `KillerType`이 전원에게 SyncVar로 동기화됨. 스캔·조사·시체 조사(3초)가 전부 "이미 가진 정보를 UI로 보여주는 연출"이라 역할 은닉이 데이터 수준에서 성립하지 않음. 위 항목들을 고쳐도 이게 남으면 의미가 없어 설계 결정이 먼저 필요.

### 경고
- [MissionObject.cs:49] `OnMouseDown` 경로가 E키 경로의 검사를 우회. `UIManager.CanInteract()`와 사망 검사(`Scanner.cs:163`)가 없어 죽은 피식자가 클릭으로 미션 시작 가능, 모달 중복 Push, UI 위 클릭 관통 가능.
- [InvestigationObject.cs:23] `Awake`에서 `SetEnabled(false)` 후 켜는 코드가 어디에도 없음 → 자식 트리거(3x3)가 영구 비활성이라 하이라이트/클릭 경로 전체가 죽은 코드. 현재는 루트 콜라이더 + `Scanner` 경로로만 동작.
- [GamePlayUI.cs:369, 459, 491] 조사 UI의 Modal Push 1회에 Pop 2회. 5초 완료 시점에 잠금이 풀려 결과창이 떠 있는 동안 이동/상호작용 가능하고, 닫을 때의 두 번째 Pop이 그사이 열린 다른 모달(미션 등)의 카운트를 깎을 수 있음.
- [GamePlayUI.cs:430-455] 플레이어 조사는 5초 동안 거리 이탈 체크가 없고(시체 조사는 있음) 횟수 제한도 없음(`scanCount` 미사용). 대상 목록에 사망자 포함.
- [GamePlayUI.cs:209-211, 432-433] null이면 로그만 찍고 바로 역참조. `TargetStartCorpseScan`의 `corpse`는 클라이언트에 아직 스폰 안 됐으면 null로 도착 → NRE.
- [Corpse.cs:7, GamePlayUI.cs:298] `[SyncVar] List<uint> scanPlayers`를 클라이언트가 직접 `Add`. 동기화되지 않고 우연히 로컬 메모로만 동작(서버는 누가 조사했는지 모름).
- [GamePlayer.cs:603-608] 완료 핸들러가 `_missionCompleteHandlers` 루프와 `AbilityDispatcher`에서 두 번 호출됨. 지금은 멱등이라 무해하나 `BadgerAbility.ApplyPenalty` 구현 시 2회 적용됨.
- [MissionObject.cs:25-28] `Awake` 직접 등록 + 코루틴 등록으로 `MissionRegistry`에 중복 등록(`Register`에 중복 검사 없음).
- [Scanner.cs:42] `myPlayer` null 체크 없음. [Scanner.cs:169-180] `prevTarget`이 비워지지 않아 대상이 없어도 0.1초마다 `SetKillUI(false)` 호출, `SetKillUI`는 끄는 호출에도 인디케이터를 Instantiate하고 프리팹 null 체크 없음(`GamePlayer.cs:526`).
- [Scanner.cs:72] 키 통일 이후 대상 선택이 "종류 무관, 피벗까지 최단거리" 하나뿐. 시체/미션/조사 오브젝트가 겹치면 고를 수 없고, 큰 오브젝트는 피벗이 멀어 밀림. `hasAttacked` 상태에서도 킬 UI 표시.
- [InvestigationTrigger.cs:12, MissionTrigger.cs:12] `parent` null이면 에러 로그 후 트리거 진입 시 NRE.
- [확인 필요] `CmdScan`은 스컹크 위장(`disguisedAs`)을 반영하는데 조사 오브젝트(`RequestPlayerType`)는 무시함 — 의도인지 확인.

### 제안
- `MissionObject`/`InvestigationObject`, `MissionTrigger`/`InvestigationTrigger`가 거의 복붙(에러 문구도 `MissionObjectTrigger` 그대로) → 공통 베이스로 통합 여지.
- 매직 넘버: 시체 조사 거리 `2.0f`(GamePlayer.cs:235)와 `4`(GamePlayUI.cs:234) 중복, 조사 시간 `3`/`5`초, 킬 UI 오프셋 `1.5f`, 하이라이트 `Color.green` → `[SerializeField]`로.
- `Scanner`: `OverlapCircleAll` 초당 10회 할당 → NonAlloc 버전. public 필드와 동일 프로퍼티 중복, `playersInRange`/`corpsesInRange` 미사용, 빈 분기, 미사용 using(`Linq`, `Unity.VisualScripting`).
- `InvestigationRecord` 필드 네이밍 혼용(`Day`/`WasNight` vs camelCase), `InvestigationLog` 싱글톤 중복 가드/`OnDestroy` 정리 없음.

### 다음 세션에서 볼 것
- 세션 4: `CmdAttack` 거리 검증, `CmdSetDisguise`/`CmdChangeZone` 무검증, `Corpse` 동기화 범위, 역할 은닉 설계
- 세션 5: `MissionRegistry` 등록/해제 생명주기, 미션 완료의 서버 검증 방식
- 세션 8: 조사 목록 재열람 시 `ActivePlayerScanResult`가 시체용 필드를 그대로 쓰는 부분

### 조치 상태
- ✅ **치명적 1번 수정 (2026-10-09)** — `GameMamager.RequestPlayerType` 제거. `GamePlayer.CmdBeginInvestigation`/`CmdCancelInvestigation` + 서버 코루틴 `ServerCoInvestigate`로 교체. 서버가 생존·조력자(`canScan`)·조사 오브젝트 거리(`InvestigationObject.IsAnyInRange`, `interactRange` 기본 2)를 시작 시점부터 `investigationDuration`(기본 5초) 동안 매 프레임 확인하고, 끝까지 유지됐을 때만 본인에게 결과 전송. 자기 자신/존재하지 않는 대상은 거부.
- ✅ 같이 수정: 조사 UI Modal Push/Pop 불균형(닫을 때 한 번만 Pop), 진행 중 창 닫기 시 서버 취소, 조건 이탈 시 "탐색 실패" 표시, null 로그 후 역참조 제거, `TryStartInvestigation`의 생존/조력자/null 가드, 조사 시간 `5` 하드코딩 제거.
- ✅ **치명적 2번 수정 (2026-10-09)** — `CmdScan`에 서버 검증 추가: 생존, `canScan`, `scanCount < maxScanCount`, 자기 자신 제외, 대상 존재 여부(`TryGetValue`). 탐색 페이즈 검사는 현재 페이즈 자체가 없어 넣지 않음(사용자 결정). 위장 값이 비정상이어도 서버 예외 없이 실제 동물로 대체. `TargetReceiveScanResult`의 인덱서/null 역참조도 방어.
- ⚠ 참고: `ContextMenuUI.cs:38,40`의 스캔/예측 버튼은 아직 `IsExplorationPhase`를 요구하는데, `RoundTime.Exploration` 진입 코드가 주석 처리돼 있어(`GameMamager.cs:247`) 정상 UI로는 버튼이 항상 비활성. → **결정(사용자):** 스캔 전용 오브젝트를 추가해 거기서 탐색하도록 추후 수정 예정이므로 지금은 그대로 둠. 죽은 플레이어의 스캔 차단도 현행 유지.
- ✅ **치명적 3번 수정 (2026-10-09, 최소 검증)** — `CmdStartMission`/`CmdCompleteMission`/`CmdCancelMission`에 서버 검증 추가. 완료는 ①슬롯 존재 ②서버가 시작을 승인한 `InProgress` 상태 ③생존(오소리는 사망 후에도 허용, `CanDoMissions`) ④해당 종류 미션 오브젝트 근처(`MissionRegistry.IsAnyInRange`, `MissionObject.interactRange` 기본 4)일 때만 처리. 거부 시 `TargetMissionCompleteRejected`로 클라이언트가 꺼버린 미션 오브젝트를 다시 켬. 완료 핸들러 2회 호출 제거(`AbilityDispatcher` 삭제), `MissionRegistry` 중복 등록 제거, 클릭(`OnMouseDown`) 경로에 `CanInteract()`/사망 검사 추가.
  - **남은 한계:** 미니게임을 실제로 풀었는지는 여전히 클라이언트 선언. 오브젝트 근처에서 시작→완료를 직접 보내면 통과함. 미션별 서버 판정은 세션 5~6에서 구조를 보고 결정.
- ✅ **치명적 5번 수정 (2026-10-09)** — 역할/사망 정보가 전원에게 동기화되던 문제.
  - `GamePlayer`: `characterName`, `animalType`, `predatorType`, `isPredator`, `isFly`, `canScan`, `canPredict`, `isDisguise`, `maxScanCount`를 SyncVar에서 제외(서버 + 본인만 보유). 본인은 스폰 후 `CmdRequestRole` → `TargetReceiveRole`로 캐릭터명을 받아 `SetAnimalType`으로 동일하게 계산. 다른 플레이어의 값은 클라이언트에서 항상 기본값(`AnimalType.None` 등). `IsRoleReady` 추가, `GameMamager.SetUIWhenReady`와 캐릭터 소개 연출은 역할 수신 후 진행.
  - `lastKillerType`/`lastDeathReason`/`lastKillerNetId`: 서버만 쓰는 값이라 SyncVar 제외.
  - `RoomPlayer.assignedCharacter`: SyncVar 제외(서버 전용). 게임 시작 전에 전원의 배정 캐릭터가 노출되고 있었음.
  - `Corpse`: 모든 SyncVar 제거(서버 전용 데이터). 시체 조사는 조사 오브젝트와 같은 구조로 변경 — `CmdScanCorpse`가 생존·조력자(`canScan`)·거리(`corpseScanRange` 기본 2)를 `corpseScanDuration`(기본 3초) 동안 서버에서 확인한 뒤 `TargetCorpseScanResult`로 본인에게만 사망 정보 전송. 기존에는 조력자 여부를 서버가 확인하지 않았음. `scanPlayers`(클라이언트가 직접 수정하던 SyncVar 리스트) 제거, 재조사 방지는 로컬 조사 기록으로 판단.
  - 사전 확인: 클라이언트 코드에서 **다른 플레이어**의 역할 필드를 읽는 곳은 없음(전부 로컬 플레이어 대상) → 호출부 변경 없이 적용 가능했음.
- ⏭ **치명적 4번(`CmdAttack` 거리 검증)은 세션 4로 이관** (능력별 공격 판정 구조를 같이 봐야 함 — 사용자 결정)
- ⏸ 경고 항목 중 위에서 같이 고치지 않은 것은 **미조치**

### 세션 4로 넘기는 것 (역할 은닉 관련 잔여 누출)
아래는 여전히 전원에게 동기화되어 역할을 추정할 수 있는 값. 값이 게임 중 바뀌는 것들이라 "본인에게만 동기화"하려면 오너 전용 컴포넌트(`syncMode = Owner`) 분리 등 구조 결정이 필요 → 프리팹 수정(Unity MCP)이 필요해 이번엔 손대지 않음.
- `GamePlayer`: `hasEate`/`hasAttacked`/`hungryStreak`(포식자 추정), `scanCount`(조력자 추정), `predictedWinner`/`predictedAs`/`predictedCorrectly`, `disguisedAs`, `Missions` SyncList(미션 종류로 캐릭터 추정), `homeZone`(거주지 → 캐릭터 후보 축소, 의도된 공개인지 확인)
- 능력 컴포넌트: `ScorpionAbility.hasPoison`, `BadgerAbility.revengeTargetNetId`/`revengeActive`, `PreySymbiosisAbility.partnerNetId` 등
- `CmdSetDisguise`/`CmdChangeZone`/`CmdSetPredict` 무검증, `CmdSendWhisper` 대상 null 체크 없음
- ⚠ Unity MCP가 이 세션에 연결돼 있지 않아 컴파일은 `dotnet build Assembly-CSharp.csproj`로 확인(오류 0). Mirror Weaver는 에디터에서만 돌므로 에디터 콘솔 확인 필요.

---

## [2026-10-09] 세션 4 — 포식자/피식자 능력 시스템 리뷰

**범위:** `WolfAbility.cs`, `HawkAbility.cs`, `CrocodileAbility.cs`, `BadgerAbility.cs`, `ScorpionAbility.cs`, `HelperAbility.cs`, `PreySymbiosisAbility.cs`, `PlayerAbility.cs`, `CharacterData.cs`, `CharacterZoneData.cs`, `PlayerDeathHandler.cs`, `Corpse.cs` (+ `GamePlayer.cs`의 `CmdAttack`/`Die`/사망 처리, `GameMamager.cs` 라운드 체크, `GamePlayer.prefab` 컴포넌트 구성 대조)

### 치명적 → 조치 상태: 논의 필요
- **[GamePlayer.cs `CmdAttack`] 공격 판정 결과가 조합에 따라 틀림.** 죽일지 말지를 공격자 쪽 능력(늑대·매)과 피격자 쪽 능력(악어·조력자·공생·전갈)이 제각각 `killTarget`/`killAttacker`에 덮어쓰는 구조이고 기본 규칙이 없음. 코드 추적 결과:
  - 악어·하이에나가 **스컹크/여우/오소리**를 공격하면 아무 일도 없음(이 셋은 피격 처리 능력이 없고, 악어·하이에나는 공격자 쪽 처리가 없음). `hasAttacked`도 안 켜져 무한 재시도 가능. 악어→늑대/매/하이에나, 하이에나→늑대/매도 무반응.
  - 늑대가 **독 있는 전갈**을 공격하면 둘 다 죽음(늑대가 켠 `killTarget`을 전갈이 안 끔).
  - 매가 **독 없는 전갈**을 공격하면 둘 다 죽음(`PredatorPriority.CanAttack`이 대상이 전갈이면 무조건 false → 매 사망 확정, 전갈은 독이 없어 자기도 사망). 매는 전갈 공격 시 항상 죽음.
  - 7인 기본 풀(타조·얼룩말·늑대·오소리·전갈·까마귀·스컹크)에서는 늑대×전갈 건이 바로 발생.
- **[GamePlayer.cs `CmdAttack`] 서버 검증 부족** (세션 3에서 이관): 거리, 자기 자신 대상, 라운드 진행 여부(`isRoundActive`) 검사 없음.
- **[BadgerAbility.cs:8-9] `revengeTargetNetId`/`revengeActive`가 전원에게 SyncVar로 동기화** → 오소리가 죽는 순간 모든 클라이언트가 살해자의 netId를 받음. 클라이언트에서 읽는 코드는 없음(서버 전용 값).
- **[PreySymbiosisAbility.cs:243,248] 파트너 피격 알림 RPC에 `attackerNetId`를 실어 보냄.** UI는 쓰지 않는데 값은 전송되어 변조 클라이언트가 공격자를 알 수 있음.
- **[GamePlayer.cs `CmdSendWhisper`] 유령→생존자 귓속말이 막혀 있지 않음.** 일반 채팅은 유령→생존자를 차단하는데 귓속말은 생존 검사가 없어, 죽은 플레이어가 살해자 정보를 산 사람에게 전달 가능. 대상이 플레이어가 아니면 서버 NRE.
- **[GamePlayer.cs `CmdSetDisguise`/`CmdSetPredict`/`CmdChangeZone`] 무검증.** 누구나 아무 때나 `disguisedAs`를 바꿔 스캔 결과를 속일 수 있고, `CmdSetPredict(uint)`는 `canPredict` 확인 없이 `spawned[]` 인덱서 사용(서버 예외).
- (세션 3에서 넘어온 잔여 누출 목록은 위 "세션 4로 넘기는 것" 참고 — 오너 전용 동기화 구조 결정 필요)

### 경고
- [GameMamager.cs:232-235, 407] 능력 `enabled`는 서버가 게임 시작 2.5초 뒤 `ServerActivate`에서야 정함. 프리팹에서는 7개 능력이 전부 켜져 있어 그 전에는 모든 능력이 켜진 상태이고, 2.5초 시점에 아직 스폰 안 된(로딩 느린) 플레이어는 `players`에 영영 안 들어가 미션·능력 설정이 누락됨. (세션 7에서 상세)
- [GamePlayer.cs `OnAliveChanged`] 시체 생성·유령 전환·게임 종료 체크 같은 **서버 로직이 SyncVar 훅 안에 있음.** 훅은 호스트 모드에서만 서버 쪽에서 불리므로 전용 서버로 바꾸면 전부 동작 안 함. `Die()`로 옮겨야 함. 같은 훅에서 `GamePlayUI.Instance`, `LocalPlayer` null 체크 없음.
- [HawkAbility.cs:35, CrocodileAbility.cs:33, ScorpionAbility.cs:48] `if (local == null) GetComponent<GamePlayer>();` — 결과를 대입하지 않아 아무 효과 없음.
- [GamePlayer.cs `CmdAttack`] 공격자 능력 루프는 try/catch가 있는데 피격자 루프는 없고 `target.Abilities` null 체크도 없음.
- [CharacterZoneData.cs] 키가 옛 캐릭터명(`Lion`, `Mouse`, `Rabbit`, `Deer`, `Otter`, `Snake`, `Mallard`, `Eagle`, `Chameleon`)으로 남아 있어 현재 13종 중 4종(악어·악어새·까마귀·하이에나)만 매칭 → 나머지 9종은 `homeZone`이 기본값(Forest). 거주지는 `CharacterConfig.HomeTerritory`에도 따로 있어 이중 관리.
- [GamePlayer.cs `SpawnCorpse`, GamePlayUI 시체 결과] 시체의 `ElapsedSec`에 `GameMamager.timer`(남은 시간 카운트다운)를 넣고 "N초 경과"로 표시 → 실제로는 남은 시간이 표시됨.
- [GamePlayer.cs `SetAnimalType`, GamePlayUI.cs:152] 캐릭터 플래그 불일치: `isDisguise`는 스컹크만 true인데 위장 UI는 여우에게 뜸. 여우는 `canScan = false`인데 `maxScanCount = 2`. `isDisguise`/`canPredict`는 서버 검증에 쓰이지 않음.
- [BadgerAbility.cs:79] `ApplyPenalty`가 로그만 찍는 빈 구현 → 복수 미션을 완료해도 효과 없음. 살해자가 없는 죽음(굶음·미션 실패)에도 미션 목록을 지우고 복수 미션을 넣음.
- [GameMamager.cs:358] `BreakSymbiosisIfAny`는 `Die()` 안에서 이미 하는 파트너 해제를 한 번 더 함(중복).
- [GameMamager.cs:279-282] 밤 체크 도중 마지막 포식자가 굶어 죽으면 `CheckGameOver`가 즉시 승패를 계산하고, 그 뒤에 피식자 미션 실패 사망이 처리됨 → 그날 죽어야 할 피식자가 생존 상태로 승패 판정됨. (세션 7에서 상세)
- [GamePlayer.cs `ApplyVisibility`] 유령은 클라이언트에서 렌더러만 끄고 위치는 계속 동기화됨.

### 제안
- 공격 판정을 한 곳으로: `CmdAttack`에서 `PredatorPriority` 기반 기본 결과를 먼저 정하고, 능력은 예외(전갈 독, 공생 알림)만 덮어쓰게. 지금은 `AttackContext.cancelAttack`/`reason`을 쓰는 능력이 없음.
- 늑대/매/악어/조력자 능력은 `OnStartServer`/`OnStartClient`가 복붙이고 네트워크 상태가 없음 → 플레이어마다 NetworkBehaviour 7개를 붙이는 대신 일반 클래스(전략 패턴)나 공통 베이스로. `HelperAbility`는 다람쥐에도 적용돼 이름이 실제 역할("피격 시 사망하는 피식자")과 다름.
- 캐릭터 데이터가 5곳에 흩어짐: `SetAnimalType`의 130줄 switch, `CharacterConfig`, `CharacterZoneData`(string 키), `MaxHungryRounds`, `PredatorPriority` → 하나의 테이블(ScriptableObject)로 통합. `PredatorType.Scorpion`인데 `isPredator = false`인 것도 혼동 요소.
- 죽은 코드: `PlayerDeathHandler.cs`(using만 있는 빈 파일), `ISpecialMissionProvider`/`SpecialMission`, `ScorpionAbility.NeedsPoisonMission`, `Corpse.deathServerTime`, `CmdAttack` 하단 주석 블록, 미사용 using(`System.Reflection`, `UnityEngine.TextCore.Text`).
- 인터페이스 메서드의 `[Server]`(`IAnimalAbility`)는 효과 없음 — 구현부에 붙여야 함. 매직 넘버: 유령 시야 `11`, 투명도 `150`.

### 다음 세션에서 볼 것
- 세션 5: 전갈 독 미션 슬롯 삽입/삭제와 미션 배정(`SetMissionsToAllPlayers`)의 상호작용, 미션 완료의 서버 판정 방식
- 세션 7: `RoundFlow`의 고정 2.5초 대기(플레이어 누락), 밤 체크 순서와 승패 판정 시점, 캐릭터 풀이 인원수별로 고정 순서(7인이면 항상 같은 7종, 포식자는 늑대 1명)인 점, 승리 조건 구현
- 세션 8: 채팅/귓속말(길이 제한 없음, 메시지마다 `FindObjectsOfType`)

### 조치 상태 (2026-10-09 수정)
- ✅ **공격 판정 규칙 통합** — `CmdAttack`이 기본 결과를 정하고 능력은 예외만 덮어씀. 규칙(사용자 확정): ①피식자는 포식자의 공격을 받으면 사망 ②포식자끼리는 서열(늑대 > 악어 > 매 > 하이에나)이 더 높은 쪽만 공격 성공 ③전갈은 독이 없으면 피식자 판정으로 사망(공격자는 `hasEate` 획득), 독이 있으면 전갈은 살고 공격자만 사망(독 소모). 늑대/매/악어/조력자/공생 능력의 개별 `killTarget` 처리는 제거, 전갈만 독 반격 예외 유지.
  - **확정(사용자):** 서열이 낮은 포식자가 높은 포식자를 공격하면 **공격자가 사망**(`DeathReason.CounterKill`, 살해자는 피격자). 모든 포식자에 일괄 적용. 같은 서열(캐릭터 중복 배정 시에만 발생)은 아무도 죽지 않고 기회만 소모.
  - 성공/실패와 무관하게 `hasAttacked`를 켜도록 변경(무반응 공격 무한 재시도 방지).
- ✅ **`CmdAttack` 서버 검증** — 라운드 진행 중(`isRoundActive`), 자기 자신 제외, 거리(`attackRange` 기본 3, 인스펙터 조절) 확인. 피격자 능력 루프에도 try/catch·null 체크 적용.
- ✅ **기타 Command 검증** — `CmdSetDisguise`(생존 + **여우만**(임시, 사용자 결정), 유효한 동물만), `CmdSetPredict` 2종(생존 + `canPredict`, 대상 검증, 인덱서 제거), `CmdChangeZone`(정의된 구역만), `CmdSendWhisper`(대상 null 체크, **유령→생존자 차단**), 채팅/귓속말 공통으로 빈 메시지 거부 + `maxChatLength`(기본 200) 초과분 절단.
- ✅ **살해자 정보 차단** — `BadgerAbility.revengeTargetNetId`/`revengeActive` SyncVar 제거(서버 전용). 유령 귓속말 차단 포함. 이제 살해자 동물 종류는 조력자의 시체 조사 결과로만 전달됨.
- ✅ **`CharacterZoneData`** — 현재 캐릭터 13종 이름으로 교체(값은 `CharacterConfig.HomeTerritory`와 동일).
- ✅ `ScorpionAbility`의 `local` 복구 코드 대입 누락 수정(매·악어는 해당 코드 자체가 제거됨).
- ✅ **시체 "N초 경과" 수정** — 남은 시간(`timer` 카운트다운)을 그대로 저장하던 것을 실제 경과 시간으로 변경. `GameMamager.PhaseElapsedSeconds`(낮/밤 길이 − 남은 시간) 추가, 시체 생성과 플레이어 탐색 기록 양쪽에 적용. `SpawnCorpse`의 `GameMamager.Instance` null 체크 추가.
- ⏸ **보류(사용자 결정):** 타조·얼룩말 피격 알림의 `attackerNetId` 전송은 임시로 유지.
- ⏸ 미조치: 능력 활성화 타이밍·`OnAliveChanged` 훅 안의 서버 로직·승패 판정 시점(세션 7), 여우/스컹크 플래그 불일치(위장은 임시로 여우만), 오소리 `ApplyPenalty` 빈 구현, 잔여 역할 누출(오너 전용 동기화), 제안 항목 전체

---

## [2026-10-09] 세션 5 — 미션 시스템 (공통 구조) 리뷰

**범위:** `BaseMission.cs`, `BagBase.cs`, `IBagItem.cs`, `DragItem.cs`, `DragThrowItem.cs`, `MissionRegistry.cs`, `MissionSelector.cs`, `TreeObject.cs`, `MissionData.cs` (+ 시작/완료 흐름 대조: `MissionUIManager.cs`, `GamePlayer.TryStartMission`, `GamePlay.unity`의 미션 프리팹 목록)

### 치명적 → 조치 상태: 논의 필요
- **[BaseMission.cs:50 → GamePlayer.CmdCompleteMission] 미션 성공 여부는 여전히 클라이언트 선언.** (세션 3에서 이관) 미니게임 로직이 전부 클라이언트 UI(`BaseMission` 하위 클래스)에 있고 서버는 "근처에서 시작한 미션인지"만 확인함. 미션별 서버 판정은 미니게임을 서버에서 재현해야 해 비용이 큼 → 현실적 대안은 **서버가 시작 시각을 기록하고 미션별 최소 소요 시간 이전의 완료를 거부**하는 방식. 방식 결정 필요.

### 경고
- [GamePlayer.cs `TryStartMission`, MissionUIManager.cs:50-56] **완료할 때 Modal을 두 번 Pop.** 완료 콜백이 `onComplete`(Pop) → `onClosed`(Pop)를 연달아 호출. Push는 한 번이라, 다른 모달이 떠 있으면 그 잠금까지 풀림. 같은 이유로 완료 직후 `onClosed`가 매번 `CmdCancelMission`을 불필요하게 전송(로컬 상태가 아직 Completed로 동기화되기 전이라). 서버가 무시하긴 함.
- [BaseMission.cs:50-60] `Complete()`/`Fail()`에 재진입 방지가 없음. 한 프레임에 두 번 호출되면 콜백이 두 번 실행되어 완료 Command 2회(두 번째는 거부 → 경고 로그), Modal Pop 4회.
- [MissionUIManager.cs:35-39] 프리팹이 없는 미션이면 에러 로그만 찍고 반환하는데, 호출 쪽은 이미 Modal을 Push하고 `CmdStartMission`을 보낸 상태 → **이동/상호작용이 영구히 잠김.** 현재 프리팹이 없는 종류는 `Grass_C`, `BadgerB`~`BadgerE`(지금은 배정되지 않아 잠복 상태).
- [GamePlayer.cs `CoApplyMissionsToLocalUI`, MissionRegistry.cs `EnableOnly`] 미션 목록이 바뀔 때마다(시작·취소·완료 포함) 전체 미션 오브젝트를 껐다가 **완료한 미션까지 포함해** 다시 켬. 완료 시 호출하는 `DisableType`은 직후 동기화로 바로 되돌려짐. 완료된 미션은 시작이 막혀 있어 동작 문제는 없으나 트리거가 계속 살아 있음.
- [BaseMission.cs:7-19] `MissionType`에 명시적 값이 없는데 프리팹·씬에는 정수로 저장됨. enum 중간에 항목을 추가/삭제/정렬하면 **기존 미션 프리팹 18개와 씬 오브젝트 18개의 종류가 전부 밀림.**
- [DragThrowItem.cs:128-136, 148-153] 느리게 놓았거나 바닥에서 놓으면 `OnReleased`를 호출하지 않고 반환 → 이 이벤트에 의존하는 미션(`FruitBStone`)은 놓았다는 사실을 못 받음. (세션 6에서 영향 확인)
- [DragThrowItem.cs:116, 140] 속도는 마지막 `OnDrag` 이벤트 기준이라, 끌다가 멈춘 뒤 놓아도 이전 속도로 날아감. `speed * throwPower(150)`는 거의 항상 `maxThrowSpeed(500)`에 걸려 던지는 세기가 사실상 고정.
- [DragItem.cs:36-43, DragThrowItem.cs:102-113] `boundsRect` 기준 로컬 좌표를 `anchoredPosition`에 그대로 대입 → 아이템의 부모가 `boundsRect`가 아니거나 앵커가 중앙이 아니면 위치가 어긋남. `DragThrowItem.Update`는 `boundsRect` null 체크 없음.
- [BagBase.cs:20-42] 수락 판정(`OnTriggerEnter2D`)이 주석 처리돼 `acceptedTypes`/`AcceptsType`이 죽은 코드이고, 추상 메서드 `OnItemAccepted`를 베이스가 호출하지 않음. `IBagItem` 구현체는 `SeedItem` 하나뿐. 하위 가방(Fruit/Grass/Seed/Wood)이 각자 어떻게 처리하는지 세션 6에서 확인.
- [MissionSelector.cs:107-111] 포식자(미션 없음)는 정상 경우인데 매 게임 `LogWarning` 출력.

### 제안
- **새 미션 추가 시 손대야 하는 곳이 6군데**(`MissionType` enum, `MissionMeta.CategoryByType`, `MissionData` 이름표, `MissionSelector` 동물별 풀, `MissionUIManager` 프리팹 목록, 씬의 `MissionObject`)이고 하나라도 빠지면 런타임에야 드러남 → 미션 정의를 ScriptableObject 하나로 모으고 시작 시 누락 검사.
- `MissionSelector`: 무작위 배정 자체는 공정함(카테고리별 1개씩 먼저 뽑고 중복 없이 채움, 모든 동물이 6종 이상 후보 보유). 다만 채우기 단계가 "무작위 재시도 + guard 200" 방식 → 남은 후보를 모아 한 번에 뽑으면 결정적. `MissionCategory.Shellfish` 미사용, static 메서드뿐인데 `MonoBehaviour`, 자기 자신을 `using static`.
- `MissionMeta.GetTypesInCategory`가 호출마다 딕셔너리 전체 순회 + 리스트 할당(게임 시작 시 1회라 영향은 작음).
- `BaseMission`/`TreeObject`/`DragThrowItem`의 콜백이 public `Action` 필드라 외부에서 덮어쓰기·호출 가능 → `event`로. `MissionStatus.NotAssigned` 미사용(기본값이라 초기화 안 된 슬롯이 이 상태가 됨).
- `MissionRegistry.Instance`가 파괴 시 비워지지 않고 등록 해제가 없음(씬 단위 수명이라 현재는 문제 없음).
- `DragItem._startPos`/`_startParent`는 대입만 되고 미사용. 미사용 using(`Unity.VisualScripting`, `UnityEngine.PlayerLoop`), `DragThrowItem`의 public 필드 `_rect`/`_isDragging`.

### 다음 세션에서 볼 것
- 세션 6: 각 미션의 `Complete()`/`Fail()` 호출 조건, 닫기 처리, `End()` 오버라이드의 정리 누락, `OnReleased` 미호출 영향, 가방 4종의 중복 구현
- 세션 7: `CheckPreySurvival`의 "완료 수 < 일차" 규칙과 전갈 독 슬롯·오소리 복수 미션의 상호작용
- 세션 8: `MissionListUI.RefreshList`

### 조치 상태 (2026-10-09 수정)
- ✅ **미션 처리 흐름을 서버 승인 방식으로 변경** (사용자가 제시한 흐름대로)
  1. [클라] 상호작용 → `CmdStartMission`. 응답이 올 때까지 Modal로 이동/상호작용 잠금
  2. [서버] 검증: 배정된 미션인가 / 이미 완료됐나 / 생존(오소리는 사망 후 허용) / 거리 / **다른 플레이어가 사용 중인가** → 통과 시 사용 중 잠금(`MissionRegistry.Lock`, 미션 종류당 1명) + 시작 시각 기록 + `TargetMissionStartAllowed`. 거부 시 `TargetMissionStartDenied`(사유 포함, 채팅창에 안내 문구)
  3. [클라] 허용 응답을 받은 뒤에만 미니게임 UI 실행(로컬 진행)
  4. [클라] 클리어 → `CmdCompleteMission`, 닫기/실패 → `CmdCancelMission`
  5. [서버] 재검증: 시작을 승인한 미션인가 / **최소 소요 시간**(`MissionObject.minDurationSeconds`, 기본 2초, 미션별 인스펙터 조절) 경과 / 거리 / 중복 완료 아님 → `Missions`(SyncList) 갱신. 거부 시 진행 상태 되돌림 + `TargetMissionCompleteRejected`
  - 잠금 해제 시점: 완료, 취소, 완료 거부, 다른 미션 시작, **사망**(`Die`), **접속 종료**(`OnStopServer`)
- ✅ **Modal 이중 Pop / 불필요한 취소 Command 제거** — `MissionUIManager.StartMission`이 `onFinished(bool completed)` 콜백 하나만 받고 정확히 한 번 호출. Pop도 한 곳(`OnLocalMissionFinished`)에서 한 번만.
- ✅ **프리팹 없는 미션으로 영구 잠김 방지** — 요청 전에 `MissionUIManager.HasMission` 확인, 허용 응답 후 UI를 못 열면 Pop + 서버 취소.
- ✅ **`BaseMission.Complete()`/`Fail()` 재진입 방지**(`_isFinished`).
- ✅ **`MissionType`에 명시적 값 부여**(기존 순서와 동일한 0~22) — 이후 항목을 추가해도 저장된 프리팹/씬 값이 밀리지 않음.
- ✅ **완료한 미션 오브젝트를 다시 켜지 않도록** `CoApplyMissionsToLocalUI`에서 Completed 제외.
- ⏸ 미조치: `DragThrowItem`의 `OnReleased` 누락/속도 계산, 드래그 좌표 가정, `BagBase` 죽은 코드(세션 6에서 개별 미션과 함께 확인), `MissionSelector` 경고 로그, 제안 항목 전체
- **남은 한계:** 최소 소요 시간만큼 기다렸다가 완료를 보내는 변조 클라이언트는 여전히 통과함(미니게임 내용은 서버가 모름). 즉시 완료·원격 완료·중복 완료·동시 사용은 차단됨.

---

## 아직 확인 필요한 것

> 테스트 항목은 전부 `Docs/review-tests.md`로 옮김 (2026-10-09). 이후 세션의 테스트 항목도 그 문서에만 추가한다.

> **세션 6 이후의 리뷰 기록은 `Docs/review-progress-2.md`에 이어서 작성한다** (파일당 200줄 제한).
