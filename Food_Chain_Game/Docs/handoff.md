# 작업 인수인계 (2026-10-12 기준)

다음 세션에서 이 문서를 먼저 읽고 이어서 작업한다. 프로젝트 규칙은 `CLAUDE.md`, 미니맵 요구사항은 `Docs/minimap.md`가 기준이다.

## 1. 지금 상태 한눈에

- 브랜치: `mainc`. **push는 하지 않았다** (사용자 확인 후에만 push).
- 작업 내용은 전부 커밋돼 있다. 커밋하지 않은 것은 원래부터 있던 `.claude/`, `Food_Chain_Game.slnx`, `Assets/TextMesh Pro/Examples & Extras/`뿐이다(이번 작업과 무관해서 건드리지 않았다).
- 에디터 3개(메인 + 클론 2개) 모두 컴파일 에러 없음, 플레이 모드 꺼짐, 메인 에디터는 Start 씬.
- **다음에 할 일: 미니맵 4단계** (아래 3번).

| 커밋 | 내용 |
|---|---|
| `d66387b` | 음성 채팅 개선, 시야를 빛과 일치하도록 수정, 입장 거부 사유 표시 수정, 음성 설정 취소 코드 제거 |
| `ecde44c` | 미니맵 1~2단계: 코너 미니맵 기본 틀, 스크롤/줌 |
| `b577316` | 미니맵 3단계: 전체 맵 오버레이, Map 키(기본 Tab) |
| 그다음 커밋 | 이 인수인계 문서 추가 |

## 2. 끝난 작업

자세한 변경 내역은 `Docs/review-progress-2.md`, 사용자가 직접 확인해야 할 항목은 `Docs/review-tests-2.md`에 있다.

### 입장 거부 사유
- 거부된 클라이언트가 사유를 받으면 스스로 나가고, 서버 타이머는 안전장치로만 둔다(`RoomManager.cs`).

### 음성 채팅
- 마이크 테스트, 음성 키(눌러서 말하기 V, 음소거 M)를 키세팅 맨 위에 추가, 눌러서 말하기 토글.
- 끊기면 자동 재연결(5초마다 확인, 2→30초 간격으로 재시도), 종료 시 로그아웃, 음소거 상태 저장.
- 사망이 확정되면 연출 전에 바로 음성 차단.
- 개별 음소거가 로비·게임 채널을 옮겨도 유지.
- 설정 > 플레이어 탭: 나를 뺀 방 전체 플레이어의 음량·음소거. 죽은 사람은 어둡게(유령에게는 전부, 생존자에게는 자신이 죽였거나 시체를 조사한 사람만). 말하는 사람은 초록 테두리 + 머리 위 스피커 아이콘(로비는 조건 없음, 게임 중은 내 시야 안만).
- 손대지 않기로 한 것: 결과 화면 전환 시 잠깐의 무음, 저장된 음량·장치의 시작 시 적용, 채널 입장 권한(내용만 `Docs/important.md`에 정리).

### 시야
- 1단계 적용: `FOVMaskController2D`가 물리 레이캐스트 대신 **빛과 같은 ShadowCaster2D 도형**을 벽으로 삼고, 벽 꼭짓점 기준으로 매 프레임(`LateUpdate`) 계산한다. 반경도 빛과 맞췄다.
  - 동작 변화: 미션 오브젝트 뒤에 선 플레이어가 이제 가려진다(빛이 원래 그곳을 그림자로 그리고 있었다).
- 2단계(빛도 같은 도형으로 그리기)는 구현했다가 **사용자 요청으로 롤백**했다("큰 차이가 없다"). 다시 시도한다면 Freeform 빛은 삼각형 분할이 겹쳐 밝은 띠가 생기므로 Sprite 방식이어야 한다.

### 미니맵 (1~3단계)
- 방식: A안(미리 찍은 맵 그림 + UI 마커). 시야 시스템과 얽히지 않고 네트워크로 주고받는 것이 없다.
- 파일
  - `Assets/Script/Minimap/MinimapConfig.cs`: 맵 경계, 맵 그림, 찍기 설정
  - `Assets/Script/Minimap/MinimapView.cs`: 미니맵 한 장(코너/전체 맵 공용). `FollowPlayer` / `FitWholeMap`, 마커 풀
  - `Assets/Script/Minimap/MinimapOverlay.cs`: 전체 맵 여닫기(Map 키, 코너 클릭, 배경 클릭)
  - `Assets/Script/Minimap/Editor/MinimapCaptureTool.cs`: `Tools > Minimap > Capture Map Image`
  - `Assets/Data/Minimap/MinimapConfig_GamePlay.asset`, `MinimapConfig_GamePlay_Map.png`
- 씬(`GamePlay.unity`의 `Canvas` 아래)
  - `Minimap UI`: 코너 미니맵(우측 하단 300x300, FollowPlayer, 클릭하면 전체 맵 열림)
  - `Map Overlay UI` > `Panel`(평소 꺼짐) > `Map`(FitWholeMap)
  - 두 미니맵 모두 구조는 `Viewport(RectMask2D) > Content > Map Image / Markers > Marker Template`
- 키: `KeyAction.MAP`(기본 Tab), 설정 > 키세팅 맨 아래 "지도 열기".
- 맵 경계: x -30.5 ~ 30.5, y -6 ~ 17.5 (MapBoundary 콜라이더 범위).
- 사용자가 정한 것: 벽은 임시 그림이라 빨간 점과 색이 겹쳐도 괜찮다.

## 3. 해야 할 작업 (미니맵, `Docs/minimap.md` 순서)

각 단계가 끝나면 사용자에게 보고하고 확인을 받은 뒤 다음으로 넘어간다. 단계마다 `feat(minimap): 설명` 형식으로 커밋한다(push는 하지 않는다).

### 4단계: 미션 마커 + 시야 안의 다른 플레이어 마커
- **미션 마커(노란 점)**: 로컬 플레이어에게 할당된 미션만 표시하고, 완료되면 지운다.
  - 할당·완료 데이터: `GamePlayer.Missions`(`SyncList<MissionSlot>`, 본인에게만 동기화). `Status != MissionStatus.Completed`인 것만 표시.
  - 표시 조건은 `GamePlayer.CanDoMissions`도 함께 본다(죽으면 미션 불가. 오소리는 예외).
  - 미션 오브젝트 위치: `MissionRegistry`가 종류별 목록(`_byType`)을 갖고 있지만 밖에서 읽는 통로가 없다. **종류로 오브젝트 위치를 읽는 메서드를 하나 추가**해야 한다(기존 스크립트 수정은 이 정도의 최소 범위로).
  - 같은 종류의 오브젝트가 여러 개일 수 있다(씬에 미션 오브젝트 18개).
- **다른 플레이어 마커**: 기본은 표시하지 않고, **내 시야 안의 플레이어만** 표시한다(사용자 결정: 파트너 시야 공유는 일단 하지 않는다).
  - 시야 판정: `GamePlayer.IsInLocalVision`(머리 위 닉네임이 보이는 상태와 같다).
  - 플레이어 목록: `GamePlayer.AllPlayers`.
  - 유령·사망자 처리에 주의: 생존자에게 유령이 보이면 안 된다. `GamePlayer.ApplyVisibility`의 규칙을 따라갈 것.
  - 색은 정해지지 않았다. 인스펙터 값으로 두고 사용자에게 확인.
- 구현 위치: `MinimapView`에 마커 종류를 추가한다. `RentMarker` / `ReturnMarker` 풀이 이미 있다. 코너와 전체 맵 두 곳에 똑같이 적용된다.
- 규칙: 매 프레임 메모리 할당 금지, 수치는 `[SerializeField]`, 다른 플레이어 위치를 새로 동기화하지 않는다(위치는 `NetworkTransformReliable`로 이미 전원에게 와 있다).

### 5단계: 멀티플레이 검증
- Host + Client 2개 이상으로: 스폰·해제, 늦은 접속, 재접속 시 마커가 맞게 생기고 사라지는지.
- 씬 전환·라운드 재시작 뒤 마커가 중복되거나 남지 않는지.
- 지금까지의 미니맵 확인은 전부 **네트워크 없이 테스트용 플레이어를 띄운 것**이다. 실제 게임 진행 중 화면은 한 번도 확인하지 않았다.

### 마무리
- `Docs/progress.md`를 현재 상태 기준으로 갱신(minimap.md의 요구).
- 2차 기능(안개, 마커 깜빡임)은 이번에 구현하지 않는다. 구조만 막지 않게 한다.

## 4. 알아야 할 정보

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

## 5. 아직 사람이 확인하지 않은 것

제가 직접 할 수 없어 `Docs/review-tests-2.md`에 남긴 것들이다.
- 음성: 실제 소리, 실제 키 입력, 마이크 입력 막대, 실제 말할 때의 표시, 네트워크 끊김, 빌드에서의 종료 로그아웃.
- 시야: 실제 게임 진행 중 화면, 낮밤 전환 중 반경 변화, 여러 명일 때 성능(S-1).
- 미니맵: 실제 게임 진행 중 화면, 실제 Tab 입력, 열린 상태에서의 이동, 해상도 변경, 타이틀·로비 설정 창의 키세팅 배치(MM-1, MM-2).
