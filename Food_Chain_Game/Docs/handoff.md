# 작업 인수인계 (2026-10-18 기준)

다음 세션에서 이 문서를 먼저 읽고 이어서 작업한다. 프로젝트 규칙은 `CLAUDE.md`, 미니맵 요구사항은 `Docs/minimap.md`가 기준이다.

## 1. 지금 상태 한눈에

- 브랜치: `mainc`. 2026-10-18 작업까지 모두 커밋하고 `origin/mainc`에 push했다(사용자 요청).
- 커밋하지 않은 것: 원래부터 있던 `.claude/`, `Food_Chain_Game.slnx`, `Assets/TextMesh Pro/Examples & Extras/`(이번 작업과 무관해서 건드리지 않았다).
- 메인 에디터: 컴파일 에러 없음, 플레이 모드 꺼짐, Start 씬.
- 다음에 할 일은 정해지지 않았다. 남은 일은 **실제 멀티플레이 확인**(아래 5번)과 **의상 그림 넣기**다.

| 커밋 | 내용 |
|---|---|
| `d66387b` | 음성 채팅 개선, 시야를 빛과 일치하도록 수정, 입장 거부 사유 표시 수정 |
| `ecde44c` | 미니맵 1~2단계: 코너 미니맵 기본 틀, 스크롤/줌 |
| `b577316` | 미니맵 3단계: 전체 맵 오버레이, Map 키(기본 Tab) |
| `a4d12ce` | 인수인계 문서 추가 |
| 그다음 커밋 | 2026-10-17~18 작업 전체(아래 2번)와 이 문서 갱신 |

## 2. 끝난 작업

자세한 변경 내역은 `Docs/review-progress-2.md`(2026-10-17 이후 항목), 사람이 확인할 항목은 `Docs/review-tests-2.md`에 있다. 2026-10-12 이전 작업(음성 채팅, 시야, 미니맵 1~3단계)은 이 문서의 이전 판(`a4d12ce`)을 본다.

### 탐색 오브젝트 일원화 (10-17)
- 플레이어 탐색은 탐색 오브젝트에서만 하고, 모든 생존자가 쓸 수 있다. 횟수: 일반 `investigationCount`(1), 조력자 `helperInvestigationCount`(2). 서버가 판정하고 결과를 보낼 때 차감.
- 우클릭 메뉴의 "탐색"·"예측" 버튼 제거. 시체 조사(부검)는 조력자 전용 그대로.
- 탐색 결과창 확인 버튼이 안 눌리던 문제: 캐릭터 소개 글자가 클릭을 가로챘다 → `raycastTarget` 끔. 결과창·노트 항목 제목을 "부검 결과" / "탐색 결과"로 구분.

### 맵 공간 1.5배 (10-17)
- 바닥·벽·경계·블록은 위치·크기 ×1.5, 미션·탐색 오브젝트와 스폰 지점은 위치만 ×1.5.
- 맵 경계: x -45.75 ~ 45.75, y -9 ~ 26.25. 미니맵 그림을 다시 찍었다.

### 위장·예측 오브젝트, 직업 능력 정리 (10-17)
- `DisguiseObject`(여우, 1회): 다른 동물로 위장. `PredictObject`(스컹크, 1회): 승리할 플레이어를 예측.
- 두 오브젝트 모두 들판의 탐색 오브젝트 오른쪽에 있다(임시 그림).
- 승리 조건
  - 스컹크: 예측한 플레이어가 이기면 함께 승리(`IDependentWinCondition`, `GameResultEvaluator`에서 나중에 계산).
  - 까마귀: 늑대 조력자(늑대 생존). 예측 능력 없음.
  - 전갈: 독 반격에 한 번이라도 성공하면 승리(`ScorpionAbility.HasCountered`).
  - 여우: 생존.
- `isDisguise` 필드 제거. 위장 가능 여부는 `CanDisguise`(여우인지)만 본다.
- 테스트용 직업 선택(로비 "직업" 버튼): `RoomManager.allowCharacterSelectForTest`, 에디터·개발 빌드에서만 동작. 기존 무작위 배정의 무한 루프 위험도 함께 고쳤다.

### 미니맵 4단계 (10-17)
- 상호작용 오브젝트 마커: 지금 할 수 있는 미션은 노란색, 탐색·위장·예측 오브젝트는 보라색. 다 쓰면 사라진다.
- 코너 미니맵은 시야 반경만큼만 보인다(`fitToVision`). 벽에 가려진 부분까지 따로 가리지는 않는다.
- 플레이어 마커: 내 시야 안의 플레이어를 그 플레이어의 색으로 표시. 산 사람에게 유령은 안 보인다.
- 얼룩말·타조: 파트너는 항상 보이고, 파트너 시야 안의 플레이어도 보인다(`FOVMaskController2D.IsVisibleFrom`). 미니맵에만 적용했고 실제 게임 화면의 시야 공유는 아직 없다.
- 원형 테두리: 안티앨리어싱한 원·링 그림(`MinimapCircle.png`, `MinimapRing.png`)으로 깨진 가장자리를 가렸다.

### 로비 커스터마이징 (10-18)
- 색상 창을 "커스터마이징" 창(`Customize UI`)으로 바꿔 의상과 색을 함께 고른다.
- 스크립트: `Assets/Script/Costume/`(`CostumeData`, `CostumeCatalog`, `PlayerCostumeView`, `CostumeButton`).
- 의상 목록: `Assets/Resources/CostumeCatalog.asset`. 0번이 기본이고 강아지·고양이·토끼가 들어 있다. 그림은 아직 비어 있다.
- 선택한 의상은 `RoomSessionData.CostumeIndex`로 PlayerPrefs에 저장되고, `RoomPlayer` → `GamePlayer`의 `costumeIndex` SyncVar로 모두에게 보인다.
- **그림 넣는 법:** `Assets/Data/Costume/Costume_○○.asset`의 `bodySprite`에 넣으면 바로 적용된다. 필요하면 `overlaySprite`, `icon`도 넣는다.
  - `bodySprite`에는 플레이어 색이 입혀진다. `overlaySprite`는 색 없이 몸 위에 덮인다.
  - 새 의상은 CostumeData 에셋을 만들어 목록 **끝**에 추가한다. 순서를 바꾸면 저장된 번호가 달라진다.

### ESC로 창 닫기 (10-18)
- 방식: `UIManager`의 닫기 스택.
  - `RegisterEscape(key, onEscape)`: 창을 열 때 등록한다. `onEscape`가 null이면 그 창이 떠 있는 동안 ESC를 막는다.
  - `UnregisterEscape(key)`: 창을 닫을 때 뺀다.
  - `HandleEscape()`: 가장 나중에 연 창을 닫는다.
- ESC 처리 순서(`SettingManager`)
  1. 키 설정 중이면 무시한다.
  2. 나가기 확인창이 떠 있으면 닫는다.
  3. 설정 창이 열려 있으면 닫는다.
  4. ESC 스택 맨 위의 창을 닫는다.
  5. 열린 창이 없으면 설정 창을 연다.
- ESC로 닫히는 창
  - 로비: 커스터마이징, 직업 선택
  - 게임: 위장·예측(확인창이 먼저 닫힘), 탐색(확인 중이면 목록으로), 결과 창, 탐색노트, 미션 목록, 우클릭 메뉴, 미니맵 전체화면
- ESC를 막는 때: 미션 중, 탐색·부검 진행 중.
- ESC로 닫지 않는 것: 채팅, 프로필, 캐릭터 소개 연출, 결과 화면.

## 3. 알아야 할 정보

### 사용자 지침
- 답변은 한국어.
- 씬·프리팹은 **Unity MCP로만** 수정한다. `.scene` / `.prefab` YAML 직접 편집 금지.
- C# 수정 뒤에는 MCP로 컴파일 성공을 확인하고, 컴파일 에러 없는 상태로만 마무리한다.
- 수치는 `[SerializeField]`로 빼고 하드코딩하지 않는다.
- 기능 단위로 나눠 하나씩 끝내고 확인받는다.
- 코드를 수정하면 `Docs/review-tests-2.md`에 확인 항목을, `Docs/review-progress-2.md`에 변경 내역을 적는다(규칙: `Docs/review-rules.md` 6번).
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

## 4. 아직 사람이 확인하지 않은 것

제가 직접 할 수 없어 `Docs/review-tests-2.md`에 남긴 것들이다. 10-17 이후 기능은 모두 **네트워크 없이 테스트용 플레이어로만** 확인했다.
- 음성: 실제 소리, 실제 키 입력, 마이크 입력 막대, 실제 말할 때의 표시, 네트워크 끊김, 빌드에서의 종료 로그아웃.
- 시야: 실제 게임 진행 중 화면, 낮밤 전환 중 반경 변화, 여러 명일 때 성능(S-1).
- 미니맵: 실제 게임 진행 중 화면, 실제 Tab 입력, 해상도 변경(MM), 다른 클라이언트에서 마커 색과 파트너 시야 공유(MAPMK, MAPPL).
- 탐색·위장·예측: 서버 쪽 확정(위장 적용, 예측 기록, 탐색 횟수 차감)과 게임 끝까지의 승패 판정(INV, DIS, PRE, WIN, SCF).
- 커스터마이징: 다른 플레이어에게 의상이 보이는지, 게임 씬으로 넘어가도 유지되는지(CUS).
- ESC: 실제 키 입력, 채팅 입력 중 ESC 동작(ESC).
