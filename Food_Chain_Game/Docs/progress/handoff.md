# 작업 참고 메모

작업을 이어갈 때 읽는 문서. **진행 상태와 남은 일은 `progress.md`**, 프로젝트 규칙은 `CLAUDE.md`, 변경 상세는 `Docs/review/review-progress-2.md`에 있다.

## 1. 시스템별 메모 (2026-10-17 ~ 18 구현분)

### 탐색 오브젝트
- 플레이어 탐색은 탐색 오브젝트에서만, 모든 생존자 가능. 횟수: 일반 `investigationCount`(1), 조력자 `helperInvestigationCount`(2). 서버가 판정하고 결과를 보낼 때 차감
- 시체 조사(부검)는 조력자 전용. 결과창·노트 제목은 "부검 결과" / "탐색 결과"

### 맵
- 맵 경계: x -45.75 ~ 45.75, y -9 ~ 26.25(10-17에 공간 1.5배, 미니맵 그림 다시 찍음)

### 위장·예측, 승리 조건
- `DisguiseObject`(카멜레온, 1회), `PredictObject`(올빼미, 1회). 들판 탐색 오브젝트 오른쪽(임시 그림)
- 올빼미: 예측한 플레이어가 이기면 함께 승리(`IDependentWinCondition`, `GameResultEvaluator`에서 나중에 계산)
- 까마귀: 늑대 조력자(늑대 생존). 전갈: 독 반격 한 번 성공하면 승리(`ScorpionAbility.HasCountered`). 카멜레온: 생존
- 위장 가능 여부는 `CanDisguise`(카멜레온인지)만 본다
- 테스트용 직업 선택: `RoomManager.allowCharacterSelectForTest`, 에디터·개발 빌드에서만

### 미니맵
- 상호작용 마커: 할 수 있는 미션 노랑, 탐색·위장·예측 보라. 다 쓰면 사라짐
- 코너 미니맵은 시야 반경만큼만(`fitToVision`), 벽 가림은 없음
- 플레이어 마커: 내 시야 안 플레이어를 그 색으로. 얼룩말·타조는 파트너와 파트너 시야 안 플레이어도(`FOVMaskController2D.IsVisibleFrom`)

### 커스터마이징
- 스크립트 `Assets/Script/Costume/`, 목록 `Assets/Resources/CostumeCatalog.asset`(0번 기본, 강아지·고양이·토끼)
- 선택값 `RoomSessionData.CostumeIndex`(PlayerPrefs) → `RoomPlayer` → `GamePlayer`의 `costumeIndex` SyncVar
- **그림 넣는 법:** `Assets/Data/Costume/Costume_○○.asset`의 `bodySprite`(플레이어 색이 입혀짐), 필요하면 `overlaySprite`(색 없이 덮음), `icon`
- 새 의상은 목록 **끝**에 추가(순서를 바꾸면 저장된 번호가 달라짐)

### ESC 닫기 스택 (`UIManager`)
- `RegisterEscape(key, onEscape)`: 창을 열 때 등록(`onEscape`가 null이면 그 창이 떠 있는 동안 ESC 차단). `UnregisterEscape(key)`: 닫을 때. `HandleEscape()`: 가장 나중 창을 닫음
- 처리 순서(`SettingManager`): 키 설정 중 무시 → 나가기 확인창 → 설정 창 → 스택 맨 위 → 없으면 설정 창 열기
- ESC 차단: 미션 중, 탐색·부검 진행 중. ESC로 안 닫는 것: 채팅, 프로필, 캐릭터 소개, 결과 화면
- `UIManager.CloseAllEscapable()`: 스택의 창을 위에서부터 전부 닫는다(사망 시 사용). onEscape가 null인 항목은 목록에서만 빠지므로 그 작업은 먼저 취소해 둔다

### 사망 처리 (`GamePlayUI.BeginLocalDeath`)
- 본인 사망 시: 미션 실패·대기 중 미션 요청 취소(GamePlayer) → 탐색·부검 취소 → 창 닫기 → 사망 연출
- **사망 연출 넣는 법:** 연출 오브젝트(꺼 둔 상태)를 만들어 `GamePlayUI`의 `deathEffectUI`에 연결, 길이는 `deathEffectSeconds`. 켜질 때 재생되도록 만들면 된다. 그동안 이동·ESC가 막힌다

## 2. 작업 방법과 주의할 점

### 사용자 지침
- 답변은 한국어.
- 씬·프리팹은 **Unity MCP로만** 수정한다. `.scene` / `.prefab` YAML 직접 편집 금지.
- C# 수정 뒤에는 MCP로 컴파일 성공을 확인하고, 컴파일 에러 없는 상태로만 마무리한다.
- 수치는 `[SerializeField]`로 빼고 하드코딩하지 않는다.
- 기능 단위로 나눠 하나씩 끝내고 확인받는다.
- 코드를 수정하면 `Docs/review/review-tests-2.md`에 확인 항목을, `Docs/review/review-progress-2.md`에 변경 내역을 적고, `Docs/progress/progress.md`의 남은 작업을 갱신한다(규칙: `Docs/review/review-rules.md` 6번).
- ParrelSync 클론에서는 에셋을 수정하지 않는다(메인 에디터에서만 수정, 클론은 새로 고침만).
- 플레이 모드는 필요하면 직접 꺼도 된다(사용자 허락).
- push는 사용자 확인 후에만.

### Unity MCP
- 인스턴스
  - 메인(호스트): `Food_Chain_Game@4ff81786c685e2db`
  - 클론 0: `Food_Chain_Game_clone_0@e2d3bb4307baeb03`
  - 클론 1: `Food_Chain_Game_clone_1@07f28478eb06ef18` (반응이 느릴 때가 있다)
- `set_active_instance`로 대상을 바꾼 뒤 `execute_code`를 쓴다. `execute_code`는 `compiler: codedom`(C# 6 문법만)으로 쓴다.
- 프리팹 수정: `execute_code` 안에서 `PrefabUtility.LoadPrefabContents` → 수정 → `SaveAsPrefabAsset` → `UnloadPrefabContents`.
- 컴파일 확인: `refresh_unity`(compile: request) 후 20초쯤 기다렸다가 `EditorUtility.scriptCompilationFailed`와 콘솔을 본다. 바로 읽으면 컴파일 중이라 비어 있을 수 있다.
- 기다릴 때는 `sleep`을 백그라운드로 실행한다(포그라운드 sleep은 막혀 있다).

### 주의할 점
- **스크립트를 쓰기 전에 세 에디터가 플레이 중인지 먼저 확인**한다. 플레이 중에 컴파일되면 그 세션이 깨진다(실제로 사용자의 플레이 세션을 한 번 날린 적이 있다).
- 메인 에디터가 플레이 중에 이유 없이 도메인을 다시 불러오는 일이 몇 번 있었다(원인 파일 미확인). 테스트가 중간에 끊기면 다시 시도한다.
- **`GamePlay.unity`는 저장할 때마다 그림자 메시가 다시 만들어져 무관한 변경이 1만 줄 넘게 생긴다.** 꼭 필요할 때만 저장하고, 씬을 열어 본 뒤 저장하지 않고 닫아도 "변경됨"으로 표시되는 것은 이 때문이다.
- `MinimapView`는 컴포넌트를 붙이는 순간 `OnValidate`가 기본값(우측 하단 300x300)으로 위치를 덮어쓴다. 새로 붙일 때는 Corner를 먼저 정한 뒤 위치를 잡는다.
- `execute_code` 안에서 `OpenScene`을 부르면 그 전에 불러 둔 에셋 참조가 무효가 된다. 씬을 연 뒤에 에셋을 다시 불러온다.
- `Sprite.OverrideGeometry`는 플레이 루프 밖에서 부르면 "Not allowed to override geometry" 에러가 난다(리플렉션으로 `BuildMask`를 직접 부를 때 생김. 평소 실행에서는 나지 않는다).
- PlayerPrefs는 세 에디터가 공유한다. 테스트로 바꿨으면 되돌린다(`PlayerNickname`은 `1234`, `Voice_MicMuted`는 0).
- 한글이나 따옴표가 든 여러 줄 수정은 스크래치패드에 파이썬 스크립트를 써서 실행하는 편이 안전하다(BOM과 줄바꿈 유지).

### 네트워크 없이 화면 확인하는 방법 (미니맵·시야에 사용)
1. 메인 에디터에서 `GamePlay` 씬을 열고 플레이.
2. 꺼 둔 빈 오브젝트 아래에 `GamePlayer.prefab`을 생성해 `Awake`가 돌지 않게 한 뒤, `NetworkIdentity`를 뺀 모든 스크립트·콜라이더·리지드바디를 끄고 밖으로 꺼낸다.
3. 리플렉션으로 `GamePlayer.LocalPlayer`에 넣는다. 카메라는 `CinemachineBrain`을 끄고 직접 옮긴다.
4. `ScreenCapture.CaptureScreenshot`으로 찍어 확인한다.
5. 끝나면 플레이를 끄고, 씬을 저장하지 않은 채 Start 씬으로 돌아간다.

이 방법으로는 실제 스폰, 동기화, 키 입력을 확인할 수 없다. 5단계는 호스트 + 클라이언트로 실제 방에 들어가서 해야 한다(호스트 시작은 플레이 후 3초쯤 지나 `NetworkCoordinator.Instance`와 `NetworkManager.singleton`이 생긴 뒤에).

### 맵·씬 구조 요약
- 게임 씬 `GamePlay`: 타일맵 없이 색칠된 사각형 스프라이트. 구역 4개(숲 왼쪽, 들판 가운데, 강 오른쪽, 하늘 가운데 위)의 T자 모양.
- 캔버스 3개 모두 1920x1080 기준 Scale With Screen Size: `Canvas`(order 0), `ICanvas`(order 10, 설정 창 등), `Mission Canvas`(order 100).
- 키 입력은 Input Manager 축이 아니라 `KeySetting`(자체 키 설정). 새 동작은 `KeyAction`의 `KEYCOUNT` 바로 앞에 추가하고 `defaultKeys`, 프리팹의 줄·배열·`ChangeKey` 번호를 함께 맞춘다.
- 역할(`animalType`, `isPredator`)은 서버와 본인만 가진다. 미니맵에는 쓰지 않는다.
