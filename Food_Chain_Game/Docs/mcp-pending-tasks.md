# Unity MCP 연결 후 진행할 작업 목록

2026-10-09 리뷰(세션 3~7 + 멀티플레이 구조 점검)는 Unity MCP가 연결되지 않은 세션에서 진행했다.
코드는 `dotnet build Assembly-CSharp.csproj`로 C# 컴파일만 확인했고(오류 0), **에디터 확인과 씬/프리팹 작업은 전부 미뤄졌다.**
이 문서는 MCP 서버를 켜고 Claude Code를 다시 실행했을 때 바로 이어서 작업하기 위한 목록이다.

## 진행 상태 — 2026-10-09 마무리

MCP 연결 세션에서 0~4번을 진행했고, 사용자 결정으로 **이 문서의 작업은 여기서 마무리**하고 세션 8(UI) 리뷰로 넘어간다.
아래에 `[ ]`로 남은 항목은 버린 것이 아니라 **다른 시점으로 넘긴 것**이다.

| 남은 항목 | 넘긴 곳 |
|---|---|
| `Setting UI.prefab`의 음소거 토글 원본 프리팹(다른 PC에 있음), 탭 버튼 5개의 없는 메서드 연결(실행에는 영향 없음 — 세션 9 확인) | 토글 프리팹을 가져온 뒤 프리팹 정리 |
| 방 나갈 때 Vivox 에러 등 음성 전반 | 음성 구현이 정리된 뒤(세션 9에서 제외) |
| `InvestigationObject` 자식 트리거 처리 | 스캔 전용 오브젝트 추가 작업 때 |
| `GamePlayer`의 `NetworkTransform` 서버 권위 전환, 유령의 물 통과 여부, 미션 enum 정리(`Grass_C`, `BadgerB`~`E`) | 설계 결정 필요(미정) |
| `currentZone` 전원 공개 | 미니맵 구현 때 |
| 5번 리팩토링 R-1~R-4, R-6 | 테스트 통과 후 |
| 6번 플레이 테스트 보조 | 리뷰 전체 종료 후 |
| 구식 API 경고 26건(CS0618) | 세션 10 |

## 시작 절차 (새 세션에서 그대로 수행)

1. Unity 에디터에서 프로젝트를 열고 MCP 서버를 켠다 (패키지: `com.coplaydev.unity-mcp`, 메뉴 Window → MCP for Unity).
2. Claude Code에서 Unity MCP 도구가 보이는지 확인한다. 보이지 않으면 연결되지 않은 것이므로 작업을 시작하지 말고 사용자에게 알린다.
3. `CLAUDE.md`의 "개발 방식 관련 요청"을 지킨다: 씬/프리팹은 **MCP로만** 수정(.unity/.prefab YAML 직접 편집 금지), 대량 변경이 필요할 때만 `Editor/TemporaryGeneratedScripts`에 임시 스크립트를 만들고 끝나면 삭제.
4. 아래 **0번(컴파일/Weaver 확인)을 반드시 먼저** 한다. 여기서 에러가 나면 이후 작업을 멈추고 에러부터 고친다.
5. 작업한 항목은 이 문서에서 `[x]`로 표시하고, 수정 내용은 `review-progress-2.md`, 테스트 항목은 `review-tests-2.md`에 기록한다 (`review-rules.md` 6번).
6. 사용자 결정이 필요한 항목(★ 표시)은 진행 전에 묻는다.

---

## 0. 컴파일 / Mirror Weaver 확인 — 최우선

- [x] 에디터 콘솔에 C# 컴파일 에러·경고가 없는지 확인 — 캐시를 비운 전체 재컴파일 기준 **에러 0**. 경고 26건은 전부 CS0618(구식 API: `FindObjectOfType`/`FindObjectsOfType` 24건, `VoiceSetting`의 `enableWordWrapping` 2건). 정리 여부는 세션 10에서 결정
- [x] **Mirror Weaver 에러가 없는지 확인 — 에러 0 (2026-10-09 MCP 세션).** Weaver는 에디터에서만 돌기 때문에 지금까지 한 번도 검증되지 않았다. 문제가 날 수 있는 변경:
  - 새 네트워크 메시지 `RoomNoticeMessage`(struct + `RoomNoticeReason : byte` enum) — `RoomManager.cs` 상단
  - RPC 인자로 쓰는 enum `MissionDenyReason : byte` — `BaseMission.cs`
  - 연결 인자 없는 `[TargetRpc]` 다수(`GamePlayer`의 `TargetReceiveRole`, `TargetInvestigation*`, `TargetCorpseScan*`, `TargetMission*`, `TargetShowGameResult`)
  - `[Server]`가 붙은 값 반환 메서드(`ServerCanInvestigate`, `ServerValidateMissionStart`, `ServerTryEndGame` 등)
  - SyncVar에서 일반 필드로 바꾼 것: `GamePlayer`의 역할 필드들, `Corpse` 전체, `BadgerAbility`의 2개, `RoomPlayer.assignedCharacter`
  - 새 SyncVar `GameMamager.introStarted`, `OnStopServer`/`OnStartClient`/`OnStopClient`/`OnGUI` 오버라이드
- [x] 에디터에서 호스트 1명으로 플레이 모드 진입 → 방 생성 → 게임 시작까지 콘솔에 예외가 없는지 확인 — **예외 0**. 타이틀 → 방 생성 → 로비(RoomPlayer 생성) → 게임 시작 → GamePlay(GamePlayer 생성, 미션 5개 설정, `introStarted` 켜짐)까지 확인. 이 PC에 `config.json`이 없어 `{"IP": "127.0.0.1"}`로 새로 만듦(gitignore 대상, 실제 서버 IP로 바꿔야 함). 이때 발견한 것은 아래 "0번에서 새로 발견한 것" 참고 (`RoomManager.allowBelowMinPlayersForTest`가 켜져 있으면 1명으로 시작 가능. `Assets/StreamingAssets/config.json`이 있어야 방 생성 가능)

### 0번에서 새로 발견한 것
- [x] (2026-10-09 `Start UI`를 다시 켬 — 플레이 모드에서 연출 재생, Animator 경고 사라짐 확인) **시작 연출(캐릭터 소개)이 화면에 나오지 않음.** `GamePlay.unity`의 `ICanvas/Start UI`가 꺼져 있고(커밋 `7ac5a86`에서 꺼짐, 그 전 커밋에서는 켜져 있었음) 켜 주는 코드가 없다. `GamePlayUI.ShowCharacter`가 꺼진 오브젝트의 Animator에 트리거를 보내 "Animator is not playing an AnimatorController" 경고 4건이 나고, 아무것도 안 보이는 채로 5.1초 동안 입력만 막힌다(`UIManager.Push(Modal)`). 씬에서 `Start UI`를 다시 켤지 결정
- [ ] ★ **`Setting UI.prefab`에 원본이 없는 중첩 프리팹 4개.** 소리 설정의 `Toggle - TotalSound` 4개(Total/BGM/Effect/UI Sound)가 가리키는 프리팹(guid `4498e22a880a14c4fae5091f69805a0d`)이 프로젝트에도 git 기록에도 없다. 씬을 열 때마다 "Script 'Toggle' ... possibly missing Required Components" 경고 4건. 음소거 토글이 동작하지 않을 가능성 — 세션 9(설정·음성)에서 함께 확인. **원본 프리팹은 사용자의 다른 PC에 있음(사용자 확인)** — 그 PC에서 프리팹과 .meta를 커밋해 가져와야 함
- [x] (사용자 결정: **제거하지 않음**) **`RoomManager.prefab`에 `NetworkManagerHUD`가 켜진 채 붙어 있음.** Mirror 기본 디버그 HUD(좌상단 "Stop Host" 등)가 그려지며, 누르면 `LeaveRoom()`의 안내·정리 절차를 건너뛴다. B-7에서 방 GUI는 없앴지만 이 컴포넌트는 별개. 제거할지 결정
- 타이틀에서 닉네임이 없는 상태로도 방 생성이 됨(닉네임 팝업만 뜨고 막지는 않음) → 로비에서 "Player"로 표시. 세션 8(UI)에서 확인
- [x] **한글이 ㅁ로 깨지는 문제 (2026-10-09 수정).** 기본 TMP 폰트(`LiberationSans SDF`)에 한글이 없는데 일부 텍스트가 이 폰트를 쓰고 있었음. 대표적으로 로비 안내 문구(`Notice Text`)에 리뷰 때 추가한 한글 사유 문구가 들어가면서 깨짐. 조치: ① `TMP Settings`의 전역 대체 폰트에 `KCC-Hanbit SDF` 추가(어떤 TMP 텍스트든 없는 글자는 이 폰트로 표시) ② `GameRoom.unity`의 `Notice Text` 폰트를 `KCC-Hanbit SDF`로 변경
- 방 나가기 직후 Vivox 에러 2건: `SetVolumeAsync ... Target Object Does Not Exist (1001)` — 음성 채널을 떠난 뒤 볼륨을 설정하려는 것으로 보임. 세션 9(설정·음성)에서 확인
- 플레이 모드 자동 테스트 시 주의: 플레이 중 MCP 스크린샷은 쓰지 말 것("PlayerLoop called recursively" 에러 발생, Overlay UI도 찍히지 않음). 에디터 창이 포커스를 잃으면 프레임이 멈춘다(`runInBackground` 꺼짐). 테스트 중에만 `Application.runInBackground = true`로 켜면 진행됨(설정 파일에는 저장되지 않음)

## 1. 인스펙터 값 확인 (새로 추가한 `[SerializeField]`)

코드 기본값이 프리팹/씬에 들어갔는지 확인하고, ★ 표시는 사용자와 값 조정.

- [x] `GamePlayer.prefab` → `GamePlayer`: `investigationDuration` 5 / `corpseScanDuration` 3 / `corpseScanRange` 2 / `attackRange` 3 / `maxChatLength` 200
- [x] `GamePlay.unity` → `MissionObject` 18개: `interactRange` 4 / `minDurationSeconds` 2 — 18개 모두 일치. ★ 값 조정(미션별 최단 클리어 시간보다 작아야 함)은 플레이 테스트 때
- [x] `GamePlay.unity` → `InvestigationObject`: `interactRange` 2
- [x] `GamePlay.unity` → `GameMamager`: `readyTimeoutSeconds` 30 / `startAnimationSeconds` 2.5 / `preRoundSeconds` 2.5 / `resultDisplaySeconds` 3
- [x] `RoomManager.prefab` → `RoomManager`: `minPlayersToStart` 3 / `allowBelowMinPlayersForTest` 켜짐 / `rejectDisconnectDelay` 0.5 / `hostCloseNoticeDelay` 0.2
- [x] `RoomPlayer.prefab` → `RoomPlayer`: `maxNicknameLength` 16 / `maxChatLength` 200 / `maxUserIdLength` 64
- [x] 미션 프리팹 `Mission_GrassA` / `Mission_SeedA` / `Mission_SeedB`: `timeLimit` 20
- [x] `RoomManager.prefab`의 `showRoomGUI`를 끈다 (코드에서 `OnGUI`를 비워 이미 표시되지 않지만 설정도 맞춰 둠)
- [x] (경고 없음 확인) `GamePlayer.prefab`에 남아 있는 옛 직렬화 값(`animalType`, `isPredator` 등 `[NonSerialized]`로 바뀐 필드)이 경고를 내지 않는지 확인

## 2. 로비 UI 작업 (코드는 준비됨, 씬 작업만 남음)

- [x] (2026-10-09 완료 — `UI Canvas/Exit Room Button`, 좌상단 200x70, 호스트 1명으로 클릭 → 타이틀 복귀·예외 없음 확인) **로비 "방 나가기" 버튼.** `GameRoom.unity`에 버튼을 만들고 onClick에 `GameRoomUI.OnExitRoomClicked`를 연결, `GameRoomUI.exitRoomButton` 필드에 할당(현재 비어 있음). 동작: 호출 시 `RoomManager.LeaveRoom()` → 방장이면 남은 사람에게 "방장이 방을 닫았습니다" 안내 후 종료
- [x] (2026-10-09 완료 — 사용자 결정: **방장 여부와 무관하게** "타이틀 화면으로 돌아가시겠습니까?" 확인창만 띄움. `Return Title Confirm UI.prefab` + `ReturnToTitleConfirmUI`, 로비 "방 나가기"와 설정의 "타이틀 화면" 버튼 양쪽에 적용) **방장 종료 확인창.** 방장이 "방 나가기"/설정 메뉴의 "타이틀로"를 누르면 "방을 닫으면 모든 플레이어가 나가게 됩니다" 확인 팝업을 띄운 뒤 `LeaveRoom()` 호출. 팝업 UI 생성 + `GameRoomUI`·`SettingManager.OnClickReturnToTitle`에 분기 코드 추가 필요(코드 미작성). 방장 판정은 `NetworkServer.active`
- [x] (2026-10-09 완료 — `UI Canvas/Connecting Overlay` + `GameRoomUI.CoShowConnectingOverlay`. 접속 지연 시 표시 → 시간 초과 후 타이틀 복귀, 호스트는 플레이어 생성 후 꺼짐 확인) **연결 중 표시.** 코드로 입장하면 접속이 끝나기 전(최대 10초)에도 로비 씬이 그대로 보인다. `NetworkClient.isConnected`가 될 때까지 "연결 중..." 오버레이 표시(UI 생성 + `GameRoomUI`에 코루틴 추가, 코드 미작성)
- [x] (2026-10-09 폭을 300 → 700으로 넓힘, 한 줄 표시 확인. 위치는 그대로 복사 버튼 아래) 로비 안내 문구(`GameRoomUI.noticeText`) 폭 — **확인 결과 좁음.** `UI Canvas/Code Group/Copy Button/Notice Text`는 300x50, 글자 28pt인데 문구 한 줄에 약 459가 필요. 잘리지는 않지만(Overflow) 두 줄로 접혀 영역 밖으로 넘침. 원래 "복사됨" 안내용 텍스트를 같이 쓰는 구조라 위치도 복사 버튼 아래. 폭을 넓힐지, 전용 안내 텍스트를 따로 둘지 결정 (방 나가기 버튼 작업 때 함께)

## 3. 역할 관련 잔여 동기화를 본인 전용으로 (리팩토링 R-5) ★

지금도 전원에게 동기화되어 역할을 추정할 수 있는 값이 남아 있다. 값이 게임 중 바뀌어서 "한 번 전달" 방식으로는 안 되고, 본인에게만 동기화되는 컴포넌트가 필요하다.

- 대상: `GamePlayer`의 `hasEate`, `hasAttacked`, `hungryStreak`, `scanCount`, `predictedWinner`, `predictedAs`, `predictedCorrectly`, `disguisedAs`, `Missions`(SyncList) / `ScorpionAbility.hasPoison` / `PreySymbiosisAbility.partnerNetId`
- 방법: 새 `NetworkBehaviour`(예: `PlayerPrivateState`)를 만들어 위 값들을 옮기고, `GamePlayer.prefab`에 컴포넌트를 추가한 뒤 그 컴포넌트의 **Sync Mode를 Owner**로 설정. 기존 접근 코드는 `GamePlayer`의 프로퍼티로 중계하면 호출부 변경이 최소화됨
- 주의: `Missions`는 클라이언트 쪽 구독(`Missions.Callback`)과 `MissionListUI`가 물려 있음. `homeZone`은 전원 공개가 의도인지 사용자 확인 필요. 타조·얼룩말 피격 알림의 공격자 netId 전송은 사용자 결정으로 임시 유지 중
- [x] (2026-10-09 완료) `PlayerPrivateState` 컴포넌트를 만들어 `GamePlayer`의 8개 값과 `Missions`를 옮기고 Sync Mode를 Owner로 설정. `ScorpionAbility`·`PreySymbiosisAbility`는 컴포넌트 자체를 Owner로 설정. `homeZone`은 클라이언트에서 쓰는 곳이 아이콘 초기 배치뿐이라 **서버 전용 값**으로 변경(사용자 방침: 구역 아이콘은 미니맵 구현 때 교체). 내용은 review-progress-2.md, 테스트는 review-tests-2.md M-12
- [ ] 남은 것: `currentZone`(현재 구역)은 여전히 전원에게 동기화되어 구역 아이콘으로 표시됨 → **미니맵 구현 때 "시야 내/능력으로 볼 수 있는 플레이어만" 표시하도록 교체**(사용자 계획). 타조·얼룩말 피격 알림의 공격자 netId 전송은 임시 유지 중

## 4. 씬/프리팹에서 확인할 것 (리뷰 중 코드만으로 판단하지 못한 것)

- [x] (사용자 결정: **그대로 둠**) `GamePlay.unity`에 `Corpse` 프리팹 인스턴스가 직접 배치돼 있음(`deathDay` 등 오버라이드 포함). 테스트용이면 제거 ★ — 확인: 루트에 `Corpse` 1개, 위치 (0,0,0), 활성 상태
- [ ] (씬 확인: `Room Map/Field/Investigation Object` 1개, `interactRange` 2, 자식 1개) `InvestigationObject`는 `Awake`에서 스스로를 끄고 다시 켜는 코드가 없어 자식 트리거(3x3)가 영구 비활성 → 하이라이트/클릭 경로가 죽어 있음(상호작용 키 경로는 동작). 조력자에게만 켜 줄지, 경로를 제거할지 결정 ★. 사용자가 예고한 **스캔 전용 오브젝트 추가** 작업과 함께 진행
- [x] (2026-10-09 `RpcTeleport`로 수정, 2인 이상 테스트 필요 — review-tests-2.md M-9) **확인 결과 Client→Server(문제 있음).** 로비 복귀 때 서버가 `rp.transform.position`을 직접 바꾸는데, 이 방향에서는 방장 본인 외의 플레이어에게 반영되지 않고 각 클라이언트가 보내는 기존 위치로 덮인다(게임 전 로비에서 서 있던 자리 그대로). 재배치가 필요하면 `NetworkTransform.RpcTeleport`로 바꿔야 함(코드 수정, 2인 이상 테스트 필요). 원문: `RoomPlayer.prefab`의 `NetworkTransform` 동기화 방향 확인. 로비 복귀 시 서버가 `RoomPlayer` 위치를 재배치하는데(`RoomManager.OnRoomServerSceneChanged`), 방향이 Client→Server면 서버가 정한 위치가 클라이언트에 반영되지 않을 수 있음
- [ ] `GamePlayer.prefab`의 `NetworkTransform`이 Client→Server(클라이언트 권위 이동) — 세션 2 경고(MCP로 재확인: `NetworkTransformReliable`, Client→Server). 서버 권위로 바꿀지는 설계 결정 ★
- [ ] 미션 프리팹이 없는 종류: `Grass_C`, `BadgerB`~`BadgerE`. 쓰지 않을 거면 enum에서 정리할지 결정 ★ (값은 명시돼 있어 지워도 다른 미션이 밀리지 않음)
- [x] (`MiniMapController`/`MinimapProfile`은 사용자 결정으로 **그대로 둠**) (2026-10-09 `VisionSystem`, `PlayerDeathHandler`는 삭제함 — 코드·씬·프리팹 참조 0건 확인 후. **남은 것: `MiniMapController`/`MinimapProfile`**) 씬·프리팹 어디에도 연결되지 않은 스크립트: `MiniMapController`/`MinimapProfile`, `VisionSystem`, `PlayerDeathHandler`(빈 파일) → 삭제 여부 ★ — MCP로 확인: `Assets/Scenes`·`Assets/Prefab`·`Assets/Resources` 전체에서 참조 0건
- [x] `GamePlayUI`의 `scanPanel`·`invProgressPanel`·`investigationUI` 등 조사/시체 조사 UI 참조가 전부 할당돼 있는지 — 오브젝트 참조 43개 전부 할당됨(null 0)
- [ ] 유령 레이어 충돌 설정 재확인: Ghost는 Water·MapBoundary·MissionObject·UI와만 충돌(벽·Occluder 통과). 유령이 물도 통과해야 하는지 ★ — MCP로 확인: 실제 충돌 대상은 Water, MapBoundary, MissionObject, UI, UI_NoDrag, TransparentFX, Ignore Raycast (뒤 셋은 게임 오브젝트 충돌과 무관)

## 5. 리팩토링 (테스트 통과 후, `review-plan-2.md`의 "리뷰 이후 리팩토링 계획")

프리팹 필드명이 바뀌는 작업이라 MCP 연결 상태에서 진행해야 값 유실을 바로 확인할 수 있다.

- [ ] R-1 미션 공통 뼈대를 베이스 클래스로(18개 미션 프리팹의 `timeLimit`/텍스트 참조 유지 확인, `[FormerlySerializedAs]`)
- [ ] R-2 통째로 같은 미션 파일 통합, R-3 미션 정의 단일화, R-4 캐릭터 데이터 단일화, R-6 상호작용 오브젝트 공통화

## 6. 플레이 테스트 보조

- [ ] `review-tests.md`, `review-tests-2.md`의 항목 중 호스트 1명으로 확인 가능한 것(미니게임 6-1~6-5, 시작 흐름 7-5 일부)을 에디터 플레이 모드 + 콘솔 로그로 먼저 확인
- [ ] 여러 인스턴스가 필요한 항목은 사용자가 Multiplayer Play Mode/ParrelSync로 진행. 실패 항목이 나오면 콘솔 로그를 받아 원인 분석

---

## 참고: 남은 리뷰 세션

세션 8(UI), 9(설정·음성), 10(데이터·유틸·미커밋 변경 정리)은 아직 리뷰하지 않았다. `review-plan-2.md` 참고.
