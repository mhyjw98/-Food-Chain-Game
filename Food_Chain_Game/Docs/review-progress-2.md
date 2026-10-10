# 리뷰 진행 기록 (이어서)

> `review-progress.md`(세션 1~5)에서 이어지는 문서 (파일당 200줄 제한). 기록 형식은 `review-rules.md` 4번과 동일.

---

## [2026-10-09] 세션 6 — 미션 시스템 (개별 미션 구현체) 리뷰

**범위:** `Assets/Script/Mission/` 하위 미션 폴더 18개, 45개 파일 약 6,900줄. 미션 본체 18개는 전부 정독, 아이템·가방·도구 스크립트는 `FruitItemA`/`WoodItemB`, `GrassItem`, `SeedItem`, `CropsItemA`/`CropsItemB`, `WoodItemA`, `FruitBStone`, 가방 4종을 정독하고 나머지(`FishRod`, `FishNetB`, `FishSchoolB`, `FlySwatter`, `InsectBugA/B`, `MeatAScanner` 등)는 위험 패턴 검색 + 부분 열람. 승리 조건에 영향을 주는 프리팹 값(`Mission_*.prefab`)도 대조.

**총평:** 개별 미션에는 네트워크 코드가 없고 완료/실패는 전부 `BaseMission.Complete()`/`Fail()`을 거치므로 동기화 방식은 일관됨. 새로 나온 치명적(크래시/치팅/desync) 항목은 없음. 문제는 ①몇몇 미션의 집계·종료 버그 ②복붙 수준의 중복.

### 치명적
- 없음

### 경고 (실제 버그)
- **[FruitA/FruitItemA.cs:31-41, 93-105 / WoodB/WoodItemB.cs 동일] 가방에 직접 끌어다 놓으면 한 개가 두 번 집계됨.** 아이템에 가방 판정 경로가 둘 있음 — `OnEndDrag`(포인터가 가방 안이면 `OnFruitCollected`)와 `Update`(매 프레임 `bag.TryCatchItem` → `OnFruitPutInBag` → `OnFruitCollected`). `Destroy`는 프레임 끝에 처리되고 EventSystem이 일반 스크립트보다 먼저 돌기 때문에, 놓은 그 프레임에 두 경로가 모두 실행됨. 열매 4개(나뭇가지 6개)가 목표인데 절반만 담아도 완료될 수 있음. 던져서 날아가 들어간 경우는 `Update` 경로만 타서 한 번.
- **[GrassA/GrassItem.cs:31-41, 53-66] 같은 구조로 `OnGrassPutInBag`이 두 번 호출됨** → 완료/실패 코루틴이 두 개 시작. 기존에는 완료 콜백 2회(완료 Command 2회, Modal Pop 중복)로 이어졌고, 지금은 세션 5에서 넣은 `BaseMission` 재진입 방지로 한 번만 처리됨.
- **[GrassA.cs:181, SeedA.cs:219, SeedB.cs:137] `FailCoroutine`이 종료 플래그(`isComplete`)를 켜지 않음.** 시간이 0이 되면 `Update`가 2초 동안 매 프레임 실패 코루틴을 새로 시작(약 120개)하고, "실 패"가 뜬 뒤에도 조작이 가능함. SeedA는 그사이 씨앗을 다 담으면 "성 공"이 표시되지만 결국 실패 처리됨. 이 세 미션은 타이머 텍스트를 0으로 보정하기 전에 출력해 `-0.0`이 보이고, `timeLimit`이 인스펙터에 노출되지 않은 `private 20f`.
- **[SeedA.cs:174-175] `Random.Range(0, Count - 1)`** — 정수 버전은 상한 제외라 마지막 씨앗/덮개가 절대 선택되지 않음. 같은 씨앗이 두 번 뽑힐 수도 있어 숨겨지는 씨앗 수가 `hiddenSeedCount`보다 적어짐.
- [SeedA.cs:197] `OnSeedCollected`에 종료 검사가 없어 목표 초과 수집 시 완료 코루틴이 중복 시작. [SeedA.cs:105] `spawnPoints`가 비면 0으로 나누기.
- [InsectB.cs:65] `bushes` null 검사가 `SpawnBug`에만 있고 그보다 먼저 도는 `ClearObject`의 `foreach`에는 없음. [WoodA.cs:83] `canvas.worldCamera` null 체크 없음. `statusText`는 같은 파일 안에서도 어떤 곳은 null 체크하고 `Begin()`에서는 안 함(FruitA, WoodA/B, InsectA/B, GrassA/B, SeedA/B).
- [FishB/FishNetB.cs:278] `public void Reset()` — Unity가 에디터에서 컴포넌트 추가/Reset 시 자동 호출하는 메시지와 이름이 같아, 에디터에서 `netRect` 미할당 상태로 실행되어 NRE.
- [Badger/BadgerA.cs, Poision/PoisonA.cs] 두 파일이 주석 한 줄 빼고 동일한 임시 구현(버튼을 전부 누르면 완료). 전갈 독·오소리 복수 미션은 사실상 미니게임이 없는 상태.

### 경고 (현재 데이터에서는 발생하지 않는 잠복 조건 — 프리팹 값 대조 완료)
- [GrassA.cs:66, 88] 정답은 후보 전체에서 뽑는데 스폰은 앞에서부터 `min(totalObjectCount, spawnGroup, 후보 수)`개만 함 → 후보가 스폰 수보다 많아지면 정답이 화면에 없는 판이 생김. 또 후보 i가 항상 스폰 지점 i에 놓여 위치가 고정됨(외울 수 있음). 현재 후보 8 / 스폰 8.
- [CropsA.cs:252] 정답 집합은 후보 **인덱스**인데 비교는 아이템의 `id`로 함. 현재 id가 0~7로 인덱스와 같아 동작. 후보 수가 `targetCount`보다 적으면 완료 불가.
- [MeatB.cs:98, 121] 스폰 지점이 null이면 건너뛰는데 목표 수는 그대로라 완료 불가. 현재 지점 8개 전부 할당.

### 제안 (중복·구조)
- **18개 미션 전부에 같은 뼈대가 복붙됨**: 타이머 `Update`, `_isComplete`류 플래그, "성 공/실 패" 표시 후 대기하는 `CompleteCoroutine`/`FailCoroutine`(미션당 약 40줄). `BaseMission`(또는 `TimedMission`)으로 올리면 위 `FailCoroutine` 버그류가 한 곳에서 해결됨. 플래그 이름도 `isComplete`/`_isComplete`/`_isFinished`/`_done`으로 제각각이고 결과 표시 시간도 2초/1.5초 하드코딩.
- **통째로 같은 파일 쌍**: `FruitA` ≡ `WoodB`(각 306줄, 이름·수치만 다름), `FruitItemA` ≡ `WoodItemB`, `BadgerA` ≡ `PoisonA`, 가방 4종(`FruitBag`/`GrassBag`/`SeedBag`/`WoodBag`)의 `TryCatchItem`. `GrassA`와 `CropsA`의 스폰/가방 판정 코드도 동일. → "나무 흔들어 담기" 미션 하나 + 설정값, 제네릭 가방 하나로 통합 가능.
- 가방 4종의 `OnItemAccepted`는 `BagBase`가 호출하지 않아 죽은 코드(세션 5와 연결).
- 프레임당 비용: 떨어진 아이템마다 매 프레임 `GetComponent<RectTransform>` + 좌표 변환 2회로 가방 판정, `IsInBagArea`/`TryGetRandomSpawnArea`가 호출마다 `GetComponentInParent<Canvas>()`, `FishNetB.CheckHitSchool`의 `GetComponentInChildren` + `FindObjectOfType`. 아이템 수가 적어 영향은 작음.
- 미션 인스턴스는 시작할 때마다 새로 생성/파괴되므로 `Begin()`의 초기화·`ClearObject()`와 `End()` 오버라이드의 정리는 대부분 불필요.
- [CropsB/CropsItemB.cs] 시든 작물은 누르면 즉시 실패이고 되돌릴 수 없어, 하나라도 놓치면 시간 초과까지 기다려야 함(`_wiltDuration`은 계산만 되고 쓰이지 않음) — 의도 확인.
- 폴더명 오타 `Poision`, 메서드명 `initBush`.

### 다음 세션에서 볼 것
- 세션 7: 미션 실패/완료 수와 `CheckPreySurvival` 규칙의 연결, 라운드 흐름
- 세션 8: `MissionListUI`, 미션 UI와 `UIManager` 우선순위

### 조치 상태 (2026-10-09 수정)
- ✅ **가방 이중 집계 수정** — `FruitItemA`/`WoodItemB`/`GrassItem`의 `OnEndDrag`가 처리 전에 `_cleared`를 켜서 같은 프레임의 `Update` 가방 판정이 다시 처리하지 않도록 함.
- ✅ **시간 초과 후 조작 가능 문제 수정** — `GrassA`/`SeedA`/`SeedB`의 `FailCoroutine`이 종료 플래그를 켜고 중복 시작을 막음. 종료 후에는 담기/수집/단계 진행 콜백을 무시. 타이머는 0으로 보정한 뒤 표시(`-0.0` 제거). 세 미션의 `timeLimit`을 `[SerializeField]`로 노출(기본 20초 유지).
- ✅ **`SeedA` 숨기기 로직 수정** — 상한 제외 문제(`Count - 1`) 제거, 같은 씨앗/덮개가 두 번 뽑히지 않도록 후보에서 하나씩 꺼냄. `spawnPoints`가 비었을 때 0으로 나누기 방어.
- ✅ **작물 B 규칙 변경(사용자 결정)** — 수확 시기를 놓쳐 작물이 시드는 순간 미션 실패 처리(`CropsItemB` → `CropsB.OnHarvestFailure`). "실 패" 표시(2초) 후 UI가 닫히고 미션이 미시작으로 돌아가 바로 다시 시작 가능. 기존에는 시간 초과까지 기다리거나 시든 작물을 눌러야 했음.
- ✅ null 방어: `InsectB.ClearObject`의 `bushes`, `WoodA.SweepCutWood`의 `canvas`.
- ✅ `BaseMission`의 재진입 방지 플래그 이름을 `_hasEnded`로 변경(`MeatA`/`MeatB`의 `_isFinished`와 혼동 방지).
- 📝 공통 뼈대/중복 파일 통합 리팩토링은 지금 하지 않고 `review-plan-2.md`의 "리뷰 이후 리팩토링 계획"에 정리(사용자 결정).
- ⏸ 미조치: `FishNetB.Reset()` 이름 충돌, 전갈 독·오소리 복수 미션의 임시 구현, 잠복 조건 3건(데이터 변경 시 주의), 제안 항목 전체

---

## [2026-10-09] 세션 7 — 게임 흐름 & 매니저 리뷰

**범위:** `GameMamager.cs`, `SpawnManager.cs`, `IWinCondition.cs`, `AnimalWinCondition.cs`, `WinCondutionFactory.cs`, `Map/MapData.cs`, `Map/MiniMapController.cs`, `Map/MinimapProfile.cs`, `Data/ConfigManager.cs` (+ `RoomManager.OnServerDisconnect`/캐릭터 풀, `GamePlayer.Die`/`OnAliveChanged`, 프리팹·씬 대조)

### 테스트 전 반드시 알아야 할 환경 문제
- **이 PC에는 `Assets/StreamingAssets/config.json`이 없음.** `.gitignore`의 `*.json` 규칙(42행)에 걸려 저장소에 올라가지 않았고 `.meta`만 커밋돼 있었음(그래서 git status에 `config.json.meta` 삭제로 표시됨). 파일이 없으면 `ConfigManager`가 에러를 남기고 방 생성/입장이 동작하지 않음. 다른 PC의 파일을 복사하거나 `{"IP": "서버주소"}`로 새로 만들어야 함. 저장소에 예시 파일을 올릴지는 결정 필요.

### 치명적 → 조치 상태: 논의 필요
- **[GameMamager.cs 전반, RoomManager.cs:103] 게임 도중 누가 나가면 라운드 흐름이 멈춤.** `players`(SyncList)는 시작 시 한 번 채우고 접속 종료 시 빼는 코드가 없음. 나간 플레이어는 파괴된 오브젝트로 목록에 남아 "살아있음" 상태가 유지되고, 밤 체크에서 굶주림/미션 실패로 `Die()`가 호출되면 `OnAliveChanged` → `SpawnCorpse`의 `transform` 접근에서 예외 → `RoundFlow` 코루틴이 그대로 종료됨(다음 낮이 오지 않고 게임이 끝나지도 않음). 나간 포식자는 `deadPredatorCount`에도 반영되지 않아 "포식자 전멸" 종료 조건이 영영 성립하지 않을 수 있음.
- **[GameMamager.cs:253-258] 시작을 고정 2.5초 대기로 처리.** 2.5초 시점에 `FindObjectsOfType`로 찾은 플레이어만 게임에 포함됨. 씬 로딩이 그보다 늦은 클라이언트는 ①`players`에 없어 미션·능력 설정·굶주림/미션 체크·승패 판정에서 전부 빠지고 ②그 시점에 보낸 `SetGameUI` ClientRpc(버퍼링 없음)를 못 받아 `GamePlayUI.localPlayer`·시야 마스크·슬롯이 초기화되지 않음. 같은 기기 테스트에서는 잘 안 드러나고 실제 네트워크/저사양에서 발생.
- **[GameMamager.cs:436-440, 478-485] 승패 결과가 호스트 외 클라이언트에 잘못 표시될 가능성이 높음.** 서버가 `isWin`(SyncVar)을 쓰고 같은 프레임에 `RpcShowGameResult`를 보내는데, Mirror는 RPC를 그 프레임의 SyncVar 갱신보다 먼저 전달함 → 클라이언트가 RPC를 받는 시점의 `localPlayer.isWin`은 아직 false라 승자도 "패배"로 보일 수 있음. 호스트는 서버 값을 직접 읽어 정상. (코드 분석 결과이며 실제 확인 필요)
- **[GameMamager.cs:294-297, 453-475] 밤 체크 도중 게임 종료 조건이 성립하면 처리 순서가 꼬임.** `CheckPredatorSurvival`에서 마지막 포식자가 굶어 죽으면 그 자리에서 승패를 계산하고 자기 자신(`RoundFlow`)을 `StopCoroutine`함. 그런데 코루틴은 다음 `yield`까지 계속 실행되므로 ①그 뒤의 `CheckPreySurvival`이 **승패 계산 후에** 피식자를 죽임(그날 죽어야 할 피식자가 승리로 기록됨) ②`currentTime`이 End에서 다시 Day로 덮어써지고 `currentRound`가 증가 ③마지막 날이면 루프를 빠져나와 `EvaluateGameResult`가 **한 번 더** 실행되어 로비 복귀(`ServerChangeScene`)가 두 번 예약됨.

### 경고
- **[AnimalWinCondition.cs:73-81] 전갈 승리 조건이 설명과 다름.** 캐릭터 설명은 "독으로 반격해 포식자를 죽이면 승리"인데 코드는 "사망자 8명 이상". 7인 게임에서는 달성 불가.
- [AnimalWinCondition.cs:118-124] 여우 설명은 "포식자/피식자 진영 선택, 해당 진영 승리 시 승리"인데 코드는 단순 생존. 진영 선택 기능 자체가 없음. 까마귀의 승자 예측(`predictedWinner`)도 승패에 쓰이지 않음.
- [GameMamager.cs:433-435] `WinCondutionFactory.GetCondition`이 null을 반환할 수 있는데(`AnimalType.None`) 그대로 `Evaluate` 호출 → 예외 시 결과 표시·로비 복귀가 모두 누락되어 게임이 끝나지 않음.
- [GameMamager.cs:460] 포식자가 0명이면 `deadPredatorCount == predatorCount`(0 == 0)가 처음부터 참 → **첫 사망 즉시 게임 종료.** 캐릭터 풀이 고정 순서(타조, 얼룩말, 늑대, …)라 2인 이하 테스트에서는 포식자가 없어 항상 해당됨. 늑대는 3인부터, 전갈 5인, 까마귀 6인, 스컹크 7인부터 등장.
- [GameMamager.cs:23, 78] `timer`(SyncVar)를 서버가 매 프레임 감소 → 동기화 주기마다 전원에게 전송되고 훅도 매번 실행. 페이즈 종료 시각을 한 번만 동기화하고 클라이언트가 계산하면 됨. `timerText`도 매 프레임 문자열 생성.
- [SpawnManager.cs:53-58] `GetRandomPositionInZone`은 이름과 달리 항상 들판(Field)에 스폰(거주지 무관). `Random.Range(-4, 4)`가 정수 버전이라 x는 정수 8칸 중 하나 → 겹쳐 스폰될 수 있음. 스폰 지점 4개 중 하나라도 미할당이면 `zonePositions`가 null이라 NRE.
- [GameMamager.cs:112-124] `players.Clear()` 후 한 명씩 `Add` → 클라이언트는 콜백마다 슬롯 UI 전체를 다시 만듦(N명이면 N+1회).
- [GameMamager.cs:478-485] `RpcShowGameResult`에서 `NetworkClient.localPlayer` null 체크 없음. `OnRoundTimeChanged` 훅의 `GamePlayUI.Instance`도 동일.
- 하드코딩: 밤 길이(`nightRoundTime` private 20, 낮은 public), 시작 대기 2.5초×2, 결과 표시 3초, 미션 5개, 씬 이름 `"GameRoom"`.

### 제안
- `GameMamager`가 라운드 진행, UI RPC, 시야 계산, 미션 배정, 승패 판정, 플레이어 목록을 모두 담당(God 클래스). 최소한 승패 판정과 UI RPC는 분리 여지.
- `AnimalWinCondition`: `MonoBehaviour` 안에 중첩 클래스 13개, 그중 9개가 `return player.isAlive;` 동일 → 공용 `AliveWinCondition` 하나로.
- 죽은 코드: `MiniMapController`/`MinimapProfile`(씬·프리팹 어디에도 없음. 2D 게임인데 `worldPos.z`를 사용하고 null 체크도 없어 그대로는 동작 안 함), `GameMamager.zoneColliders`, `RoundTimeChanged`/`TimerChanged` static 이벤트(구독자 없음), `SpawnManager.ResetUsedSpawns`/`GetZonePosition`, 주석 처리된 위장/탐색 페이즈.
- `GameMamager.Instance`·`SpawnManager.Instance`가 파괴 시 비워지지 않음. `MaxHungryRounds`는 public static 가변 딕셔너리.
- `ConfigManager`: 정적 생성자에서 파일 IO, `NetworkConfig`가 struct라 로드 실패와 빈 값이 구분되지 않음. PC 전용 경로 방식(안드로이드 등에서는 `File`로 StreamingAssets를 읽을 수 없음).
- 이름 오타: `GameMamager`, `WinCondutionFactory`, `ActivateAbilitie`.

### 다음 세션에서 볼 것
- 세션 8: `GamePlayUI`(결과 표시, `localPlayer` 초기화 의존), `PlayerSlotUI.CreateSlots`, 채팅/귓속말, `UIManager` 우선순위
- 세션 10: `.gitignore`의 `*.json` 규칙 범위, 미커밋 변경 정리

### 조치 상태 (2026-10-09 수정)
- ✅ **치명적 1번: 게임 도중 접속 종료/튕김 = "나감" 처리**
  - `RoomManager.OnServerDisconnect`가 플레이어 오브젝트를 파괴하기 전에 `GameMamager.ServerHandlePlayerLeft` 호출(정상 종료와 타임아웃 튕김 모두 이 경로).
  - 나간 플레이어는 `players` 목록에서 제거 → 이후 굶주림/미션 체크·승패 판정에서 빠지고, 클라이언트의 플레이어 슬롯과 구역 아이콘도 사라짐. 시체는 남기지 않음. 공생 파트너 해제, 진행 중 미션 잠금 해제(세션 5의 `OnStopServer`), 전원에게 "○○님이 게임에서 나갔습니다." 안내.
  - 나간 직후 종료 조건을 다시 판정 → 유일한 포식자가 나가면 게임 종료. 나간 플레이어는 승패 판정 대상이 아니며, 하이에나의 승리 조건은 "늑대가 목록에 없으면(나감) 사망으로 취급".
  - 목록 순회 전부에 파괴된 항목 방어 추가.
- ✅ **치명적 4번: 종료 판정 순서** — 종료 판정을 `ServerTryEndGame`/`ServerEndGame` 한 곳으로 모으고 `_gameEnded`로 한 번만 실행. 밤 체크는 굶주림·미션 실패 사망을 전부 처리한 뒤 한 번만 판정(승패 계산 후 추가 사망, 페이즈가 Day로 되돌아가는 문제, 마지막 날 이중 실행 해소). 종료 조건은 카운터 대신 현재 목록에서 직접 계산.
  - 동작 변경: **포식자가 처음부터 0명인 게임은 첫 사망으로 끝나지 않음**(생존자 0명 또는 마지막 날까지 진행).
- ✅ **사망 처리의 서버 로직을 SyncVar 훅에서 `Die()`로 이동**(세션 4 경고) — 시체 생성·유령 전환·종료 판정이 호스트 모드에 의존하지 않음.
- ✅ **유령 규칙 정리(사용자 요구)**
  - 이동: 유령 레이어는 벽·밤 맵 차단·하늘 차단(Occluder/Wall)과 충돌하지 않음을 충돌 매트릭스로 확인(맵 경계·물·미션 오브젝트와는 충돌). 코드 변경 없음.
  - 소통: 일반 채팅은 기존대로 유령→유령만. 귓속말은 **같은 상태끼리만**(산 사람↔유령 양방향 차단). 서버가 직접 띄우던 말풍선 제거(호스트가 살아있어도 유령의 말풍선을 보던 문제).
  - 기믹: 죽으면 `Scanner`가 공격 대상·시체·조사 오브젝트를 잡지 않음(킬 UI도 안 뜸). 미션 목록과 미션 오브젝트는 사망 후 전용 미션이 있는 캐릭터(오소리의 복수 미션)만 표시되고 나머지 유령은 전부 꺼짐.
- ✅ `EvaluateGameResult`: 승리 조건이 없는 캐릭터는 패배 처리(예외로 게임이 안 끝나던 문제).
- ✅ **치명적 2번: 전원 준비 후 시작** — 각 클라이언트가 스폰 → 역할 수신 → 게임 UI 초기화를 마치면 `CmdReportGameReady`로 알리고, 서버(`RoundFlow`)는 접속 중인 모든 연결이 준비될 때까지 대기(최대 `readyTimeoutSeconds` 30초, 넘으면 경고 후 현재 인원으로 시작). 전원 준비되면 `RpcPlayStartIntro`로 **모두 동시에** 시작 연출(캐릭터 소개)을 재생 → `startAnimationSeconds`(2.5초) → 플레이어 목록/능력/미션 설정 → `preRoundSeconds`(2.5초) → 첫 낮 시작. 두 값의 합이 캐릭터 소개 연출 길이(5초)와 같음.
  - 게임 UI 초기화(`SetGameUI` RPC)는 서버 RPC를 없애고 각 클라이언트가 자기 준비 시점에 직접 수행(`GameMamager.ClientInitLocalUI`) → 로딩이 늦어 RPC를 놓치는 문제 제거. 시작 연출도 `introStarted`(SyncVar)로 늦은 클라이언트가 보충 재생.
  - 한계: 제한 시간(30초)을 넘겨 스폰된 플레이어는 여전히 게임에서 제외됨.
- ✅ **치명적 3번: 본인 승패 결과** — `EvaluateGameResult`가 각 플레이어에게 `TargetShowGameResult(isWinner)`로 결과를 인자로 직접 전달. SyncVar `isWin`을 RPC 시점에 읽던 `RpcShowGameResult` 제거.
- ✅ **마지막 밤 이후 판정/복귀** — 마지막 밤의 굶주림·미션 실패 사망 처리를 전부 끝낸 뒤 `ServerEndGame` → 승패 계산 → 결과 표시(`resultDisplaySeconds` 3초) → 로비 복귀가 한 번만 실행. 복귀 씬은 하드코딩 `"GameRoom"` 대신 `NetworkRoomManager.RoomScene` 사용.
- ✅ 하드코딩이던 시작 대기 2.5초×2, 결과 표시 3초를 `[SerializeField]`로 노출.
- ⏸ 미조치: 전갈/여우 승리 조건, `config.json`, 경고·제안 항목 전체
- 참고: 유령의 음성 채팅 분리는 `VoiceManager.EnterGameplay(isAlive: false)`로 처리되는 구조이나 내부는 세션 9에서 확인 예정. 호스트가 나가면 서버가 사라져 게임이 종료되는 것은 그대로(호스트 이전 기능 없음).

---

## [2026-10-09] 멀티플레이 구조 점검 — 방 시스템 / 로비 ↔ 게임 반복 / 접속 종료

세션 7 수정 후 사용자 요청으로 진행한 추가 점검. **코드 수정 없음(점검만).**

**범위:** `RoomManager.cs`, `RoomPlayer.cs`, `NetworkCoordinator.cs`, `RoomHost.cs`, `RoomJoin.cs`, `CodeManager.cs`, `RoomSessionData.cs`, `NetworkErrorManager.cs`, `GameRoomUI.cs`, `TitleUI.cs`(방 생성/입장), `SettingManager.cs`(나가기), `RoomManager.prefab` 설정, `GameRoom.unity` 기본 상태, Mirror `NetworkRoomManager`/`NetworkRoomPlayer`/`NetworkServer` 소스 대조

### A. 반복 플레이를 막는 문제 (치명적)
- **A-1. 로비로 돌아온 뒤 두 번째 게임을 시작할 수 없음.** 시작 버튼과 방 코드 표시는 씬 기본값이 꺼짐이고, 켜는 코드(`GameRoomUI.CheckHostStatus`)는 `RoomPlayer.CmdSetUserId`(최초 입장 시 1회)에서만 호출됨. 게임 후 `GameRoom` 씬이 다시 로드되면 새 `GameRoomUI`에는 아무도 호출하지 않아 호스트에게 시작 버튼이 나타나지 않음. [GameRoomUI.cs:32, RoomPlayer.cs:106-114]
- **A-2. 정원이 정확히 찼을 때만 시작 가능.** `StartGame`이 `joinedConnections.Count < maxPlayerCount`면 거부. 게임 중 한 명이 나가면 누가 다시 들어올 때까지 다음 게임을 시작할 수 없음. 캐릭터 풀도 실제 인원이 아니라 정원(`maxPlayerCount`) 기준으로 뽑음. [RoomManager.cs:331-338, 299]
- **A-3. 호스트 주소로 Node 서버 주소를 등록함.** `TitleUI.StartHost`의 `hostIp = ConfigManager.Config.IP` — 설정 파일의 IP는 Node(방 코드) 서버 주소인데 이를 호스트 IP로 등록. 입장자는 코드 조회 결과로 받은 그 주소에 접속하므로, 호스트가 Node 서버와 같은 기기일 때만 접속됨. (Node 서버가 요청자의 실제 IP를 쓰도록 돼 있는지는 서버 코드가 저장소에 없어 확인 불가.) 호스트 직접 접속 방식이라 포트 개방/릴레이 없이는 외부 접속도 불가. [TitleUI.cs:179-185]

### B. 접속 종료·튕김 관련 허점
- **B-1. 게임 진행 중 입장 시도가 방 인원수를 깎음.** 진행 중에는 Mirror가 새 연결을 바로 끊는데(`Not in Room scene`), `OnServerDisconnect`는 입장(+1) 여부와 무관하게 항상 Node에 −1을 보냄 → Node의 인원수가 실제보다 작아져 정원 초과 입장이 가능해짐. 서버(Mirror) 자체의 접속 제한은 `maxConnections: 100`이라 정원을 막지 못함. 시도한 쪽은 "서버와의 연결이 끊어졌습니다."만 보게 됨(게임 중이라는 안내 없음). 튕긴 플레이어는 그 판에 재접속 불가(로비 복귀 후에는 코드로 재입장 가능). [RoomManager.cs:165-168]
- **B-2. 호스트가 비정상 종료하면 방이 Node 서버에 남음.** 방 삭제는 정상 종료 경로의 코루틴 웹 요청뿐이라, 창 닫기/크래시 시에는 전송되지 않음. Node 쪽에 만료(하트비트/TTL)가 없으면 죽은 방 코드가 계속 조회됨.
- **B-3. 호스트가 나가면 전원이 타이틀로 나감(호스트 이전 없음).** 호스트 방식의 한계. `isHost` SyncVar, `ReassignHostAfterDisconnect`, `wasHost` 분기는 호스트 이전처럼 보이지만 실효가 없음(가장 낮은 연결 ID = 항상 실제 호스트이고, 실제 호스트가 끊기면 서버가 사라짐).
- **B-4. 씬 전환 대기 중 이탈.** 로비→게임 전환 때 서버보다 먼저 로딩을 끝낸 클라이언트는 Mirror의 `pendingPlayers`에 들어가는데, 그 상태에서 끊기면 목록에서 빠지지 않음. 서버 로딩 완료 시 `OnRoomServerCreateGamePlayer`가 파괴된 RoomPlayer에 접근해 예외 → 뒤에 대기 중인 다른 플레이어의 게임 캐릭터가 생성되지 않을 수 있음(타이밍이 좁아 드묾). [RoomManager.cs:234-262]
- **B-5. 로비 복귀 시 다른 플레이어가 안 보일 수 있음.** RoomPlayer 표시 복구를 서버의 `RpcSetRoomVisualsActive(true)`에 맡기는데, ClientRpc는 그 시점에 준비(ready) 상태인 클라이언트에게만 전달됨. 서버보다 씬 로딩이 늦은 클라이언트는 RPC를 놓쳐 숨김 상태가 유지됨. 같은 기기 테스트(로딩 시간 동일)에서는 통과했던 항목. 숨김은 클라이언트 훅(`OnClientExitRoom`)이 처리해 문제없음. [RoomManager.cs:414-445]
- **B-6. 서버 예외 = 그 플레이어 튕김.** `exceptionsDisconnect: 1`이라 Command 처리 중 예외가 나면 해당 연결이 끊김. 게임 쪽 Command는 세션 3~5에서 방어했으나 로비 쪽(`RoomPlayer.CmdSetUserId`의 `GameRoomUI.Instance` 등)은 미점검 항목이 남음. 타임아웃(KCP 10초, 비활성 15초)보다 씬 로딩이 길면 로딩 중 튕길 수 있음.
- **B-7. Mirror 디버그 GUI가 켜져 있음**(`showRoomGUI: 1`). 게임 중 호스트 화면 우상단에 "Return to Room" 버튼이 떠서 누르면 종료 처리 없이 로비로 전환되고, 로비에는 "PLAYERS" 박스와 Mirror 기본 준비/제거 버튼이 표시됨.
- **B-8. 의도적으로 나가는 수단이 설정 메뉴의 "타이틀로"뿐.** `GameRoomUI.exitRoomButton`은 미연결. 호스트가 누르면 확인 없이 전원의 게임이 끝남. `_userRequestedQuit`은 켜는 곳이 없는 죽은 변수.

### C. 경고 / 제안
- 타이틀 씬 이중 로드 가능성: Mirror가 `offlineScene`(Title)을 자동 로드하는데 `CleanupAndLoadTitle`도 직접 로드함 → 에러 팝업이 첫 로드에서 소비되고 사라질 수 있음(실제 확인 필요).
- 방 인원 목록 이중 관리: Mirror의 `roomSlots`와 자체 `roomPlayers`/`joinedConnections`.
- `CmdSetColor`에 서버 측 중복 색상 검사 없음, 로비 채팅 길이 제한 없음.
- 접속 시도 중(최대 10초)에는 오프라인 상태로 로비 씬이 그대로 보임(연결 중 표시 없음).

### 문제없이 되어 있는 것
- 끊김 감지(비활성 15초) → 로비에서는 색상 해제·목록 정리, 게임 중에는 "나감" 처리(세션 7)
- 서버가 사라졌을 때 클라이언트의 타이틀 복귀 + 안내 문구, 정리 중복 방지 가드와 재무장
- 호스트/클라이언트 시작 재시도, 이전 세션 강제 정리
- RoomPlayer를 게임 중에도 유지해 로비 복귀 시 재사용, 매 게임 캐릭터 재배정
- 게임 종료 → 결과 → 로비 복귀 1회 실행(세션 7)

### 조치 상태 (2026-10-09 수정)
- ✅ **A-1 두 번째 게임 시작** — `GameRoomUI`가 로비 씬이 열릴 때마다(최초 입장·게임 후 복귀) 방장 전용 UI(시작 버튼, 방 코드)를 스스로 갱신. 방장 판정은 `NetworkServer.active`(이 구조에서 방장은 항상 호스트).
- ✅ **A-2 시작 조건** — "정원이 꽉 찼을 때만" → **최소 인원 이상**(`RoomManager.minPlayersToStart`, 임시 3명)이면 시작. 캐릭터 풀은 정원이 아닌 **실제 인원** 기준. 인원이 모자라면 로비 안내 문구로 사유 표시. 시작 버튼 연타 방지(`_startingGame`), 서버가 아니면 시작 불가.
  - **테스트용 우회:** `allowBelowMinPlayersForTest`(기본 켜짐)가 켜져 있으면 **에디터와 개발 빌드에서만** 1~2명으로도 시작 가능. 정식 빌드에서는 이 값과 무관하게 최소 인원 적용.
- ✅ **B-1 인원수 어긋남** — 실제로 입장 처리된 연결만 Node 서버에 퇴장(−1)으로 보고. 정원 초과 연결은 서버에서도 거부(`OnServerConnect`).
- ✅ **B-4 전환 대기 중 이탈** — 접속 종료 시 Mirror의 `pendingPlayers`에서 해당 연결 제거, 게임 캐릭터 생성부 null 방어.
- ✅ **B-5 로비 복귀 표시** — `RoomPlayer`가 활성 씬 변경을 직접 감지해 표시/숨김을 스스로 결정(서버 RPC는 보조로 유지).
- ✅ **B-7 디버그 GUI** — `RoomManager`/`RoomPlayer`의 `OnGUI`를 비워 Mirror 기본 방 GUI 제거(프리팹의 `showRoomGUI` 값과 무관).
- ✅ **끊김 사유 안내** — 서버가 연결을 끊기 전에 사유 메시지(`RoomNoticeMessage`)를 보내고, 클라이언트는 타이틀에서 일반 문구 대신 그 사유를 표시: "게임이 진행 중인 방에는 입장할 수 없습니다." / "방 인원이 가득 찼습니다." / "방장이 방을 닫았습니다."
- ✅ **B-8 나가기** — `RoomManager.LeaveRoom()` 추가. 방장이 나가면 남은 사람들에게 방이 닫혔음을 먼저 알린 뒤 종료. 설정 메뉴의 "타이틀로"가 이 경로를 사용. 로비용 `GameRoomUI.OnExitRoomClicked()` 추가(**버튼 연결은 씬 수정이 필요해 미완** — Unity MCP 필요). `_userRequestedQuit`이 실제로 쓰이게 됨.
- ✅ **B-6 로비 Command 방어** — 닉네임(빈 값/길이), 채팅(빈 값/길이), UserId 길이, 색상 인덱스 범위 검증. **색상 중복 방지**: 이미 쓰이는 색을 요청하면 최초 입장은 빈 색으로 대신 배정, 변경 요청은 거부.
- ✅ **게임 중 퇴장 시 호스트 예외(신규 발견)** — `PlayerColorPalette`가 로비 씬의 버튼을 static으로 들고 있어, 게임 씬에서 누가 나가면 색상 해제(`colorIndex = -1`) 훅이 파괴된 버튼에 접근해 호스트에서 예외 → `OnServerDisconnect`가 중단되어 **나간 플레이어의 캐릭터가 파괴되지 않고 남는** 문제. 버튼 접근을 전부 null 안전하게 바꾸고, `OnServerDisconnect`는 정리 중 예외가 나도 Mirror의 연결 정리(`base`)를 반드시 실행하도록 try/finally로 감쌈.
- 📝 **A-3 호스트 주소 등록 / B-2 죽은 방 정리 등 Node 서버 관련** — 서버 코드를 볼 수 없어 `Docs/node-server-checklist.md`에 점검 목록으로 정리(사용자 결정).
- ⏸ 미조치: B-3 호스트 이전, 게임 중 재접속, 방장 종료 확인창·로비 나가기 버튼 배치·연결 중 표시(UI 작업, MCP 필요), 타이틀 이중 로드 여부 확인

---

## [2026-10-09] Unity MCP 검증 — 컴파일/Weaver, 인스펙터 값, 씬·프리팹 확인

MCP가 연결된 세션에서 `mcp-pending-tasks.md`의 0번·1번과 4번의 확인 항목을 진행했다. 항목별 결과는 그 문서에 기록.

### 결과 요약
- **컴파일/Weaver:** 캐시를 비운 전체 재컴파일에서 에러 0, Weaver 에러 0. 경고 26건은 전부 구식 API(CS0618).
- **호스트 1명 실행:** 타이틀 → 방 생성 → 로비 → 게임 시작 → 게임 씬 진입까지 예외 0. (Node 서버 등록 실패 로그는 127.0.0.1에 서버가 없어서 나는 것)
- **인스펙터 값:** 세션 3~7에서 추가한 `[SerializeField]` 값이 프리팹/씬에 전부 코드 기본값대로 들어가 있음. `GamePlayUI` 참조 43개 전부 할당.

### 수정한 것
- `RoomManager.prefab`의 `showRoomGUI`를 끔 (MCP로 프리팹 수정).
- `Assets/StreamingAssets/config.json`을 `127.0.0.1`로 생성 (이 PC에 파일이 없었음. gitignore 대상이라 커밋되지 않음).

### 새로 발견한 것 (미조치, 사용자 결정 대기)
- **시작 연출이 나오지 않음.** `GamePlay.unity`의 `Start UI`가 꺼져 있고 켜는 코드가 없음 → 캐릭터 소개가 안 보이는 채 5.1초간 입력만 막힘. 커밋 `7ac5a86`에서 꺼짐. [GamePlayUI.cs:104-125]
- **`Setting UI.prefab`의 음소거 토글 4개가 원본 없는 프리팹을 가리킴** (guid `4498e22a...`, 프로젝트·git 기록에 없음).
- **`RoomManager.prefab`에 `NetworkManagerHUD`가 켜져 있음** → Mirror 디버그 HUD로 종료 절차를 건너뛸 수 있음.
- **로비 복귀 시 RoomPlayer 재배치가 방장 외에는 적용되지 않음.** `RoomPlayer`의 `NetworkTransform`이 Client→Server인데 서버가 `transform.position`을 직접 바꿈. [RoomManager.cs:610-618]
- **로비 안내 문구 영역이 좁음** (300 폭에 약 459 필요, 두 줄로 넘침).

### 추가 조치 (같은 날, 사용자 결정 반영)
- ✅ **시작 연출 복구** — `GamePlay.unity`의 `ICanvas/Start UI`를 다시 켬. 플레이 모드에서 캐릭터 이름·거주지·승리 조건 문구가 표시되고 Animator 경고가 사라진 것을 확인.
- ✅ **한글 깨짐(ㅁ) 수정** — `TMP Settings` 전역 대체 폰트에 `KCC-Hanbit SDF` 추가, 로비 `Notice Text` 폰트를 `KCC-Hanbit SDF`로 변경. 원인은 한글이 없는 기본 폰트(`LiberationSans SDF`)를 쓰는 텍스트에 한글이 들어간 것.
- 씬 두 개(`GamePlay`, `GameRoom`)를 Unity 6000.3에서 처음 저장하면서 직렬화 형식이 새 버전으로 바뀌어 diff가 크게 나옴(내용 변경은 위 두 가지뿐).
- ⏸ `NetworkManagerHUD`는 제거하지 않음(사용자 결정). 음소거 토글 원본 프리팹은 다른 PC에 있어 그쪽에서 커밋 필요. 
- ✅ **로비 복귀 시 RoomPlayer 재배치** — 서버가 `transform.position`만 바꾸던 것을 `NetworkTransform.RpcTeleport`로 지시하도록 변경(`RoomManager.ServerRelocateRoomPlayer`). ClientRpc는 준비된 클라이언트에게만 가므로, 본인이 아직 씬 로딩 중이면 목적지를 보관했다가 `OnServerReady`에서 전달. 접속 종료·씬 전환 시 보관분 정리. [RoomManager.cs]
- ✅ **로비 "방 나가기" 버튼** — `GameRoom.unity`에 `Exit Room Button` 생성, `GameRoomUI.OnExitRoomClicked` 연결, `exitRoomButton` 할당. 호스트 1명으로 클릭 시 타이틀 복귀·서버 종료·에러 팝업 없음 확인.
- ✅ **로비 안내 문구 폭** 300 → 700.
- 새로 발견: 방을 나갈 때 Vivox `SetVolumeAsync` 에러 2건(세션 9에서 확인).
- ✅ **연결 중 표시** — `GameRoom.unity`에 `Connecting Overlay`(화면 전체를 가리는 반투명 패널 + "연결 중..." 문구) 추가. `GameRoomUI`가 로비 씬이 열릴 때 아직 접속 전이면 띄우고, 내 플레이어가 생성되면 끈다. 게임 후 로비 복귀(이미 접속됨) 때는 띄우지 않음. 설정 창보다 아래에 있어 ESC 메뉴로 나가기는 가능. 문구·점 간격은 인스펙터에서 조절. [GameRoomUI.cs]
- ✅ **미사용 스크립트 삭제** — `VisionSystem.cs`(시야는 `LocalVisionLight`가 담당), `PlayerDeathHandler.cs`(빈 파일). 코드·씬·프리팹 참조 0건 확인.
- 참고: 접속 실패 시 타이틀 씬이 두 번 로드되는 것으로 보임(씬 로드 경고가 두 벌 출력) — 멀티플레이 구조 점검 C의 "타이틀 이중 로드 가능성"과 같은 건. 에러 팝업은 정상적으로 처리됨.

---

## [2026-10-09] 리팩토링 R-5 — 역할 관련 잔여 동기화를 본인 전용으로

전원에게 동기화되어 다른 플레이어의 역할을 추정할 수 있던 값을 서버와 본인만 갖도록 바꿨다.

### 변경 내용
- **새 컴포넌트 `PlayerPrivateState`** (Sync Mode = Owner) — `hungryStreak`, `hasEate`, `hasAttacked`, `scanCount`, `predictedWinner`, `disguisedAs`, `predictedAs`, `predictedCorrectly`, `Missions`(SyncList)를 `GamePlayer`에서 옮김. `GamePlayer`에는 같은 이름의 프로퍼티를 남겨 중계하므로 호출부는 바뀌지 않음. `GamePlayer`에 `[RequireComponent]` 추가, `GamePlayer.prefab`에 컴포넌트 추가.
- **`ScorpionAbility`(`hasPoison`), `PreySymbiosisAbility`(`partnerNetId`)** — 컴포넌트의 Sync Mode를 Owner로 변경(가진 SyncVar가 전부 본인 전용 값).
- 세 컴포넌트 모두 프리팹 설정과 별개로 `Awake`에서 `syncMode = SyncMode.Owner`를 고정(프리팹 값이 바뀌어도 전원 동기화로 돌아가지 않게).
- **`homeZone`** — SyncVar → 서버 전용 필드. 클라이언트에서는 구역 아이콘의 처음 위치를 정하는 데만 쓰였고, 그 때문에 전원의 거주지가 보였음. 아이콘은 이제 현재 구역(`currentZone`) 기준으로 놓임.
- **`memo`** — SyncVar → 일반 필드. 원래 각 클라이언트가 로컬에서만 쓰는 메모라 동기화되는 일이 없었음.

### 확인
- 컴파일/Weaver 에러 0. 호스트 1명으로 게임 시작 → 미션 5개 설정·미션 목록 UI 표시, 예외 없음.
- 서버에서 직접 직렬화해 비교: 본인 전용 값(스캔 횟수, 공격 여부, 굶주림, 위장, 미션, 파트너)을 바꾸면 **본인용 데이터만 달라지고 다른 플레이어용 데이터는 한 바이트도 달라지지 않음**. 대조로 공개 값을 바꾸면 다른 플레이어용 데이터가 달라짐.

### 남은 것
- `currentZone`은 아직 전원 공개(구역 아이콘). 미니맵 구현 때 "시야 내/능력으로 볼 수 있는 플레이어만" 보이도록 교체 예정(사용자 계획).
- 이제 클라이언트에서 **다른 플레이어의** 위 값들은 항상 기본값이다. 다른 플레이어의 값을 읽는 UI/로직을 추가할 때는 서버에서 판정해 결과만 전달해야 함.
- 실제 다른 클라이언트 화면에서의 동작(미션 진행, 스캔, 공생 시야 등)은 2인 이상 테스트 필요.

### 나가기 확인창 (2026-10-09)
- ✅ 사용자 결정: 방장 여부와 무관하게 "타이틀 화면으로 돌아가시겠습니까?" 확인창만 띄운다.
- `ReturnToTitleConfirmUI`(새 스크립트) + `Assets/Prefab/UI/Return Title Confirm UI.prefab`. 로비(`GameRoom`)와 게임(`GamePlay`) 씬의 설정 UI가 있는 캔버스 맨 위에 배치. 타이틀 씬에는 두지 않음(없으면 바로 이동).
- 로비 "방 나가기"(`GameRoomUI.OnExitRoomClicked`)와 설정의 "타이틀 화면"(`SettingManager.OnClickReturnToTitle`)이 모두 이 확인창을 거침. 확인창이 떠 있는 동안 이동/상호작용 차단, ESC는 확인창만 닫음.
- `Setting UI.prefab`은 원본 없는 중첩 프리팹 4개가 들어 있어, 저장 시 연결 정보가 사라질 위험을 피하려고 **건드리지 않고** 별도 프리팹으로 만들었음.
- 확인: 호스트 1명으로 나가기 클릭 → 확인창, 취소 → 닫힘·이동 가능, 설정 메뉴 경로도 동일, 확인 → 타이틀 복귀.
- 새로 발견(세션 9로 넘김): 설정 창 "나가기" 탭 버튼의 onClick이 존재하지 않는 메서드 `SettingManager.OnClickCancleAfterCheck`를 가리킴.

`mcp-pending-tasks.md` 작업은 여기서 마무리(남은 항목과 넘긴 곳은 그 문서 상단 표 참고).

---

## [2026-10-09] 세션 8-A — UI 시스템 (인게임 UI) 리뷰

세션 8은 대상 파일이 24개라 규칙(세션당 5~10개)에 따라 둘로 나눴다. 8-A는 인게임 UI 9개.
범위: `GamePlayUI`, `MissionUIManager`, `MissionListUI`, `MissionListItemUI`, `InvestigationListUI`, `InvestigationListItem`, `InvestigationResultPresenter`, `ContextMenuUI`, `PartnerAttackAlertUI`
Unity MCP 연결 상태에서 진행(컴파일·씬 확인 포함).

### 치명적
- 없음

### 경고 (실제 버그)
- **결과 창을 연속으로 열면 상호작용이 영구히 잠김.** 조사 기록 목록에서 항목을 두 번 연달아 누르면 결과 창이 열릴 때마다 잠금(Scan)을 걸지만 닫을 때는 한 번만 풀어서, 이후 미션·조사 등 상호작용이 씬이 바뀔 때까지 막힘. 결과 창이 화면을 덮지 않아 목록을 계속 누를 수 있음. [GamePlayUI.cs `ActiveScanResult`/`ActivePlayerScanResult`] → **수정함**
- **플레이어 탐색 기록을 다시 열면 엉뚱한 내용이 표시됨.** 시체 조사용 창("사망 시각/사망자/공격자")을 그대로 써서 "사망자: 늑대"(기록에 없는 값의 기본값), "공격자: (탐색한 정체)"로 나오고 경과 시간도 소수점 그대로 표시. [GamePlayUI.cs `ActivePlayerScanResult`] → **수정함**(항목 이름을 "탐색 시각/대상/정체"로 바꿔 표시)
- **탐색 대상 목록을 만들 때 나간 플레이어가 있으면 예외.** `GameMamager.players`에 null로 남은 항목의 닉네임을 읽음 → 탐색 창이 버튼 없이 열림. [GamePlayUI.cs `InitBtnGroup`] → **수정함**
- **미션 종료가 두 번 처리될 수 있음.** 미션 UI는 프레임 끝에 파괴되므로 그 사이 같은 미션이 완료/실패를 한 번 더 알리면 서버에 완료와 취소가 함께 전달될 수 있음. [MissionUIManager.cs `Finish`] → **수정함**(미션당 한 번만 처리)
- **우클릭 메뉴가 열린 사이 대상이 나가면 예외.** 귓속말/스캔/예측 버튼이 파괴된 대상의 netId를 읽음. 로컬 플레이어·채팅 매니저 null도 미확인. [ContextMenuUI.cs:34-56] → **수정함**
- **위장/예측 창의 잠금 짝이 맞지 않음 (현재 꺼져 있는 기능).** 창을 열 때 잠금 1회, 확인창을 닫을 때마다 1회씩 해제 → 확인을 누르면 2번 풀려서 동시에 열린 다른 창(설정, 플레이어 목록)의 이동 잠금까지 풀림. 확인창을 취소하면 선택 창이 열린 채로 이동 가능. 시간 초과 시 확인창이 남고, 그 상태에서 확인을 누르면 무작위로 정해진 값을 덮어씀. 위장과 예측이 `isSelected`를 같이 써서, 위장 선택 후 10초 안에 예측 창이 뜨면 위장이 무작위 값으로 바뀜. [GamePlayUI.cs:151-201, 345-377, 652-676] → **보류**(아래 "논의 필요")
- **우클릭 메뉴의 스캔·예측 버튼이 항상 비활성.** 두 버튼은 "탐색 시간" 단계에서만 켜지는데, `GameMamager.RoundFlow`에서 위장/탐색 단계가 주석 처리돼 있어 그 단계가 오지 않음. [ContextMenuUI.cs:38-40, GameMamager.cs:316-327] → **논의 필요**

### 경고 (잠복)
- 캐릭터 소개 연출이 `CharacterConfig`에 없는 동물이면 예외로 중단되고 이동 잠금이 풀리지 않음 [GamePlayUI.cs `ShowCharacter`] → **수정함**(정보가 없으면 잠금 전에 건너뜀)
- `AnimalNameMap`을 인덱서로 읽는 곳이 여러 군데(없는 값이면 예외) → 이 파일 안의 결과 표시부는 안전한 조회로 **수정함**
- `InvestigationLog.Instance`가 없을 때 조사 목록/결과에서 예외 → **수정함**
- `PartnerAttackAlertUI.PlayAlert`: 오브젝트가 꺼져 있으면 코루틴 시작 실패. 현재 씬에서는 켜져 있어 문제없음(MCP 확인).
- 조사 목록은 열려 있는 동안 새 기록이 추가돼도 갱신되지 않음(다시 열어야 보임).
- 구역 아이콘의 닉네임은 생성 시 한 번만 설정(닉네임이 늦게 오면 빈 칸). 미니맵 교체 예정이라 미조치.

### 제안
- `GamePlayUI`가 소개 연출·라운드 표시·위장·예측·시체 조사·플레이어 탐색·구역 아이콘·결과까지 맡는 744줄 God 클래스. 기능별 패널 스크립트로 분리 권장(위장/예측을 되살릴 때 함께).
- 하드코딩 수치: 소개 연출 5초·0.1초, 위장 10초, 예측 5초 → `[SerializeField]`로 **수정함**. 단, 소개 연출 5초는 서버의 `startAnimationSeconds + preRoundSeconds`(2.5+2.5)와 따로 관리되므로 한쪽만 바꾸면 어긋남.
- 위장/예측 버튼 목록(`allAnimals`)이 8종만 포함(다람쥐·얼룩말·오소리·타조·스컹크 없음). 의도인지 확인 필요.
- `ContextMenuUI`가 열릴 때마다 `FindObjectOfType<ChatManager>`와 `Debug.Log` 호출.
- `MissionListUI`는 목록이 바뀔 때마다 전부 파괴 후 재생성(항목 5개 내외라 영향 작음).
- 사용하지 않는 `using`: `ContextMenuUI`의 `Mirror.Examples.Chat`(Mirror 예제 폴더를 지우면 컴파일이 깨짐) → **제거함**. `GamePlayUI`의 `Unity.VisualScripting` → **제거함**. 같은 종류가 범위 밖에 더 있음(세션 10에서 일괄 정리): `GameMamager`, `GamePlayer`, `BaseMission`, `DragThrowItem`, `FruitItemA`, `GrassItem`, `WoodA`, `PlayerMove`, `RoomPlayer`, `Scanner`의 `Unity.VisualScripting*`, `SettingManager`의 `Mirror.BouncyCastle.Tsp`.

### MCP로 확인한 것
- 세 씬과 `Assets/Prefab` 전체에서 버튼에 연결된 메서드가 실제로 존재하는지 검사: 인게임 UI 쪽은 이상 없음. **`Setting UI.prefab`의 탭 버튼 5개가 없는 메서드를 가리킴**(`OnClickApply`, `OnClickResetDefault`, `OnClickCompleteSet`, `OnClickCancleAfterCheck` x2) → 세션 9에서 확인. `Button.prefab`에 대상 없는 빈 onClick 1개.
- `GamePlay.unity`에 `PartnerAttackAlertUI`, `InvestigationLog`, `InvestigationListUI`, `UIManager`, `MissionUIManager`가 모두 있고 활성 상태.
- 수정 후 플레이 모드 확인: 결과 창을 시체 기록 → 탐색 기록 → 시체 기록 순으로 연속으로 열고 한 번 닫으면 잠금이 정상 해제됨. 탐색 기록은 "탐색 시각 / 대상 / 정체"로 표시됨.

### 논의 필요
1. **위장 시간·탐색 시간 단계를 되살릴 계획인지.** 되살린다면 위장/예측 창의 잠금 문제를 먼저 고쳐야 하고, 버린다면 관련 코드(위장·예측 창, 우클릭 메뉴의 스캔·예측 버튼, 서버의 `CmdSetDisguise`/`CmdSetPredict`/`CmdScan`)를 정리할 수 있음.
2. 위장/예측 후보 8종이 의도한 목록인지.

### 다음 세션에서 볼 것
- 세션 8-B: `GameRoomUI`, `TitleUI`, `ChatManager`, `ChatInputFocus`, `PlayerSlot`, `PlayerSlotUI`, `PlayerColorPalette`, `CharacterSetting`, `UIManager`, `UIDraggable`, `UIResizable`, `SortingSprite`, `SpriteSorter`, `SpeechBubble` (+ 이번 세션에 새로 만든 `ReturnToTitleConfirmUI`)
- 타이틀에서 닉네임 없이 방 생성이 되는 것(MCP 검증 중 발견) — 8-B에서 확인

### 8-A 후속 조치 (2026-10-09, 사용자 결정 반영)
- **결정:** "위장 시간/탐색 시간" 단계는 없앤다. 대신 **위장 오브젝트와 탐색 오브젝트**로 대체할 예정. 위장 후보는 **모든 동물**.
- ✅ **위장 창 정리** [GamePlayUI.cs] — 후보를 8종 → 전체 13종으로 변경. 시간 제한(10초 뒤 무작위 선택) 제거. 이동 잠금을 "열 때 한 번, 닫을 때 한 번"으로 고쳐 논의 항목이던 잠금 어긋남 해결(확인창 "아니요"는 선택 창으로 돌아가고 잠금 유지). 선택 없이 닫는 `CloseDisguiseUI()` 추가(위장 오브젝트에서 사용). 확인창이 선택 창 뒤에 가려질 수 있던 순서 문제도 함께 수정.
  - 여는 진입점은 기존 `GamePlayUI.ShowDisguiseUI()`(여우만 열림) 그대로. 지금은 호출하는 곳이 없어 화면에 뜨지 않음 → 위장 오브젝트 작업 때 연결.
- ✅ **게임 중 퇴장 = 사망 (임시 구현)** — 세션 7에서는 "시체 없이 목록에서 제거"였으나 변경. 라운드 진행 중에 살아 있는 플레이어가 나가면 `Die`로 사망 처리(시체 생성, 진행 중 미션 해제, 공생 해제, 종료 조건 확인)한 뒤 목록에서 제거. 사망 원인 `DeathReason.Left`(퇴장) 추가, 시체에 사망 원인을 보관해 조사 결과의 공격자 칸에 "퇴장"으로 표시. [GameMamager.cs `ServerHandlePlayerLeft`, GamePlayer.cs, Corpse.cs, GamePlayUI.cs]
  - 라운드 시작 전(시작 연출 중)이나 게임 종료 후에 나간 경우는 기존처럼 목록에서만 제거.
  - 확인: 호스트 1명 플레이 모드에서 퇴장 처리를 직접 호출 → 사망, 시체 생성(사망 원인 Left), 결과 창 "퇴장" 표시, 예외 없음. 실제 접속 종료 경로는 2인 이상 테스트 필요.
- ⏸ **남은 정리(위장/탐색 오브젝트 작업 때 함께):** `RoundTime.Disguise`/`Exploration` 값과 `GameMamager.RoundFlow`의 주석 처리된 단계, `RPCShowDisguiseUI`/`RPCShowPredictUI`/`RPCCheckPredict`, 우클릭 메뉴의 스캔·예측 버튼(탐색 시간에만 켜지므로 현재 항상 비활성), 예측 창(까마귀)의 잠금 문제. **예측 기능을 어떻게 할지는 미정** — 확인 필요.
- ✅ **예측 창 정리 (2026-10-09, 사용자 결정: 예측도 오브젝트로 대체 예정)** [GamePlayUI.cs] — 위장 창과 같은 방식으로 정리. 시간 제한(5초 뒤 무작위 예측) 제거, 이동 잠금을 "열 때 한 번, 닫을 때 한 번"으로 수정, 선택 없이 닫는 `ClosePredictUI()` 추가. 진입점은 `ShowPredictUI()`(까마귀만 열림) 그대로이며 지금은 호출하는 곳이 없음. 후보 목록은 기존 8종 그대로(변경 지시 없음).

---

## [2026-10-09] 세션 8-B — UI 시스템 (로비·공용 UI) 리뷰

범위: `ChatManager`, `ChatInputFocus`, `PlayerSlot`, `PlayerSlotUI`, `PlayerColorPalette`, `CharacterSetting`, `GameRoomUI`, `TitleUI`, `UIManager`, `UIDraggable`, `UIResizable`, `SortingSprite`, `SpriteSorter`, `SpeechBubble`, `ReturnToTitleConfirmUI`(이번에 새로 만든 것)
Unity MCP 연결 상태에서 진행(컴파일·씬 확인·플레이 모드 확인 포함).

### 치명적
- 없음

### 경고 (실제 버그) — 모두 수정함
- **귓속말을 받은 쪽 화면에 보낸 사람이 아니라 받는 사람 자신의 닉네임이 표시됨.** 받는 쪽에 보내는 RPC의 "말한 사람" 인자에 받는 사람의 netId를 넣고 있었음. [GamePlayer.cs `CmdSendWhisper`, ChatManager.cs `AddWhisperMessage`]
- **채팅 입력 중에 씬이 바뀌거나 채팅창이 꺼지면 이후 이동 불가.** "입력 중" 표시가 static인데 해제 이벤트가 오지 않아 다음 씬까지 남음. 예: 로비에서 채팅을 치는 중에 방장이 게임을 시작하면 그 플레이어는 게임에서 움직이지 못함(엔터를 두 번 눌러야 풀림). [ChatInputFocus.cs]
- **귓속말 상대가 나간 뒤 그 탭에서 보낸 글이 전체 채팅으로 전송됨.** [ChatManager.cs `SendChat`]
- **채팅에 `<size=300>`, `<color>` 같은 태그를 넣으면 그대로 적용됨.** 글자를 화면 가득 키우거나 시스템 안내처럼 꾸밀 수 있음. 닉네임도 같은 경로. [ChatManager.cs] → 채팅창 표시분은 태그가 글자 그대로 보이도록 처리. **말풍선과 머리 위 이름표는 아직 그대로**(아래 "남은 것")
- **메시지가 올 때마다 채팅창 전체를 지우고 다시 만듦 + 보관 개수 제한 없음.** 대화가 길어질수록 메시지 한 줄마다 수백 개를 재생성. [ChatManager.cs `RefreshChatView`] → 새 메시지 한 줄만 추가, 채널당 100개(인스펙터 조절)까지만 유지
- **플레이어 메모를 쓰고 엔터를 누르면 채팅 입력으로 포커스가 넘어감.** [ChatManager.cs `Update`]
- **방 정원이 어긋남.** 드롭다운을 건드리지 않고 방을 만들면 방 코드 서버에는 정원 0으로 등록되고, 실제 정원은 static에 남은 이전 값(첫 실행은 1). 타이틀로 돌아오면 드롭다운은 다시 1로 보이는데 실제 값은 이전 방의 값. [TitleUI.cs:33-35, 177-185] → 한 값으로 통일하고 드롭다운 표시도 맞춤
- **닉네임 없이 방 생성/입장 가능.** 닉네임 팝업을 ESC로 닫으면 그대로 진행됨(로비에서 "Player"). [TitleUI.cs] → 방에 들어가기 직전에 다시 확인
- **게임 후 로비로 돌아오면 다른 사람이 쓰는 색 버튼이 눌림.** 버튼은 씬과 함께 새로 만들어지는데 "사용 중" 표시는 색이 바뀔 때만 갱신. 누르면 서버는 거부하지만 내 미리보기는 그 색으로 바뀜. [PlayerColorPalette.cs] → 로비 씬이 열릴 때 현재 플레이어들의 색으로 맞춤
- **마우스 휠을 화면 어디서 굴려도 채팅이 스크롤됨**(채팅 위에서는 두 배로 움직임). [UIDraggable.cs `Update`] → 채팅창 위에서만
- 방 생성/입장 버튼을 연달아 누르면 씬 로드가 중복 요청됨 [TitleUI.cs] / 읽지 않은 탭 이름에 강조 태그가 메시지마다 겹겹이 쌓임 [ChatManager.cs `UpdateTabVisual`] / 방 코드 복사 안내와 시작 실패 안내가 같은 문구 칸을 따로 지워 서로의 문구를 일찍 지움 [GameRoomUI.cs]

### 경고 (잠복) — 수정함
- `PlayerSlot`: 로컬 플레이어가 없거나 슬롯의 대상이 나간 상태에서 설정/메모 편집/우클릭 시 예외
- `CharacterSetting`: 색이 정해지지 않은 상태(-1)로 호출되면 예외
- `SpriteSorter`: Back/Front 오브젝트가 없거나 같은 높이면 예외 또는 0으로 나눔. `SortingSprite`: 씬에 SpriteSorter가 없으면 매 프레임 예외, 씬 전환 후에는 파괴된 SpriteSorter를 참조(로비↔게임을 오가는 RoomPlayer)
- `ChatManager.Start`: 이름으로 오브젝트를 찾다가 하나라도 없으면 예외로 중단되어 채팅 전체가 동작하지 않음

### 제안
- `UIManager.CurrentPriorityValue`가 매 프레임(이동 가능 여부 확인) LINQ로 최댓값 계산 → 반복문으로 **수정함**
- `SortingSprite`가 오브젝트마다 `FindObjectOfType` 호출(게임 씬 19개 + 플레이어) → 공유 캐시로 **수정함**
- 하드코딩 수치: 닉네임 8글자, 정원 13, 휠 0.15, 안내 2초, 정렬 범위 1~10000 → `[SerializeField]`로 **수정함**
- `ChatManager`가 네트워크 기능을 쓰지 않는데 `NetworkBehaviour` + 씬 `NetworkIdentity`로 돼 있음 → 접속 전에는 꺼져 있고, 서버가 스폰해야 켜짐. `MonoBehaviour`로 바꾸는 편이 단순(씬 오브젝트 수정 필요, 미조치).
- `GamePlayer`/`RoomPlayer`가 메시지마다 `FindObjectOfType<ChatManager>` 호출 → `ChatManager.Instance` 권장(미조치)
- 죽은 코드: `PlayerColorPalette.GetRandomExcluding`(중복), `PlayerSlot.Instance`, `GameRoomUI.CheckHostStatus`/`ShowStartGameButton`/`ShowCodeGroup`(`CoRefreshHostUI`와 중복), `ChatManager`의 주석 처리된 `AddMessage`(이번에 제거). `AnimalNameMap`이 `PlayerSlot.cs` 안에 있음(R-4 때 데이터 쪽으로).
- `UIDraggable`의 드래그는 Screen Space Overlay 캔버스를 전제(현재 씬은 Overlay라 문제없음, MCP 확인).

### 남은 것 / 논의 필요
1. **로비의 입장·퇴장 안내가 방장 화면에만 표시됨.** 서버 코드가 자기 화면의 채팅창에 직접 적고 있어 다른 플레이어에게는 가지 않음. [RoomPlayer.cs `CmdSetData`, RoomManager.cs:310] — 범위(네트워크) 밖이라 미조치. 전원에게 보내도록 고칠지 결정 필요.
2. **말풍선과 머리 위 이름표의 태그.** 채팅창은 막았지만 말풍선(`SpeechBubble`)과 이름표는 같은 글을 그대로 표시함. 서버에서 닉네임·채팅의 `<`를 걸러내는 방식이 가장 확실(`ServerTrySanitizeChat`, `CmdSetData`) — 미조치.
3. **방 정원 기본값이 1명.** 드롭다운 첫 항목이 1이라, 그대로 방을 만들면 아무도 들어올 수 없음. 최소 시작 인원(3명)·목표 인원(7명 이상)에 맞춰 기본값과 선택 범위를 정할 필요.
4. **닉네임 길이 제한이 두 곳.** 타이틀 입력은 8글자, 서버는 16글자.
5. `TitleUI.QuitGame`은 설정 저장·음성 로그아웃 없이 바로 종료(`SettingManager.QuitGame`은 둘 다 함) — 세션 9에서 확인.
6. 방 코드 복사 안내 문구가 영문("Copy completed.") — 인스펙터에서 바꿀 수 있게 해 둠.

### MCP로 확인한 것
- 세 씬 모두 `UIManager`·`EventSystem` 1개씩. `SpriteSorter`와 Back/Front는 로비·게임 씬에 있음. `ChatManager`의 `chatInputField`는 두 씬 모두 인스펙터 미연결(이름 검색에 의존), 로비는 탭 UI 미연결(전체 채널만).
- 플레이 모드: 태그가 든 채팅이 글자 그대로 표시됨, 메시지 130개 추가 시 100개만 유지, 채팅 입력 중 채팅창을 끄면 "입력 중"이 해제됨, 귓속말 탭 생성·읽지 않음 표시·선택 시 해제, 플레이어 슬롯·정렬 정상, 예외 없음.

### 다음 세션에서 볼 것
- 세션 9(설정·음성): `Setting UI.prefab`의 탭 버튼 5개가 없는 메서드를 가리킴, 음소거 토글 원본 프리팹 누락, 방 나갈 때 Vivox 에러, `TitleUI.QuitGame`

### 8-B 후속 조치 (2026-10-09, 사용자 결정 반영)
- ✅ **로비 입장·퇴장 안내를 방의 모든 플레이어에게 전송** — 서버가 자기 채팅창에 직접 적던 것을 `RoomPlayer.ServerBroadcastSystemMessage`(각 플레이어에게 TargetRpc)로 변경. 퇴장 안내는 나가는 본인을 빼고 남은 사람에게 보냄. [RoomPlayer.cs `CmdSetData`, RoomManager.cs `ServerCleanupDisconnectedPlayer`]
- ✅ **닉네임·채팅의 태그를 서버에서 차단** — `TextSanitizer.BlockRichText`(새 파일 `Assets/Script/Utill/TextSanitizer.cs`): `<` 바로 뒤에 폭 없는 공백을 넣어 태그로 읽히지 않게 함. 화면에는 입력한 글자가 그대로 보임. 닉네임(`CmdSetData`), 로비 채팅, 게임 채팅·귓속말(`ServerTrySanitizeChat`)에 적용 → 이름표, 말풍선, 플레이어 목록 등 닉네임·채팅이 들어가는 모든 곳에 한 번에 적용됨. 채팅창의 `<noparse>` 처리는 이중 방어로 유지.
  - 폰트에 전각 괄호(＜)가 없어서 글자를 바꾸는 방식 대신 이 방식을 택함(MCP로 폰트 글리프 확인: U+200B 있음, U+FF1C 없음).
- ✅ **닉네임 길이 8글자로 통일** — 서버 제한(`RoomPlayer.maxNicknameLength`)을 16 → 8로 변경(코드 기본값 + `RoomPlayer.prefab`). 타이틀 입력 제한(8)과 일치.
- ✅ **방 정원 기본값 1명 유지(임시)** — 테스트 편의를 위한 사용자 결정. 코드 변경 없음(`TitleUI.SelectedPlayerCount` 기본값 1, 드롭다운 1~13). 정식 값은 추후 결정.
- 확인: 호스트 1명 플레이 모드에서 입장 안내가 RPC 경로로 채팅창에 표시됨, 태그가 든 글이 이름표에서 글자 그대로 표시됨, 예외 없음. 다른 클라이언트 화면은 2인 이상 테스트 필요.

---

## [2026-10-09] 세션 9 — 설정 (음성 제외) 리뷰

범위: `SettingManager`, `SettingsTabController`, `KeySetting`, `SoundSetting`, `ResolutionSetting`, `AudioManager`
**음성(Vivox)은 사용자 결정으로 제외** — 아직 구현 중이고 오류가 많은 상태라 `Assets/Script/Voice/*`와 `VivoxSmokeTest`는 보지 않았다. 음성 관련으로 넘겨 둔 항목(방 나갈 때 Vivox 에러 등)도 그대로 둠.
Unity MCP 연결 상태에서 진행(컴파일·씬 확인·플레이 모드 확인 포함).

### 치명적
- 없음

### 경고 (실제 버그) — 모두 수정함
- **키 변경을 마우스 클릭으로 취소하면 이후 ESC가 계속 무시됨.** 취소 처리에서 코루틴을 시작하지 않고 호출만 해서 "키 입력 대기 중" 표시(static)가 풀리지 않음 → 설정 창을 ESC로 열고 닫을 수 없고, 타이틀의 ESC도 동작하지 않음(키를 한 번 성공적으로 바꿔야 풀림). 키 입력을 기다리는 중에 씬이 바뀌어도 같은 상태가 됨. [KeySetting.cs:44-49]
- **닫기(X) 버튼은 저장하지 않고 ESC는 저장함.** X로 닫으면 바꾼 값(키, 소리, 해상도)이 그 씬에서는 적용돼 보이지만 저장되지 않아, 다음 씬에서 조용히 이전 값으로 돌아감. [SettingManager.cs, 프리팹의 Cancle Btn → `DeActiveSettingUI`] → X와 ESC 모두 "저장하고 닫기"로 통일
- **타이틀에서 설정 창을 연 채 ESC를 누르면 설정 창이 닫히면서 게임 종료 팝업도 같이 뜸.** 같은 ESC를 `SettingManager`와 `TitleUI`가 각각 처리. [SettingManager.cs `Update`, TitleUI.cs `Update`]
- **키 지정에 ESC와 "없음"이 들어갈 수 있음.** 키 입력 대기 중 ESC를 누르면 ESC가 그 동작의 키로 지정됨(메뉴 키와 겹침). 키를 뗄 때의 이벤트나 글자 입력 이벤트(keyCode 없음)도 그대로 받아들여, 누르고 있던 키가 엉뚱하게 지정될 수 있음. [KeySetting.cs `OnGUI`] → 키를 누를 때만 받고, ESC는 취소로 처리
- **해상도를 목록의 순번으로 저장.** 모니터나 PC가 바뀌면 같은 순번이 다른 해상도를 가리킴. [ResolutionSetting.cs] → 가로·세로 크기로 저장(예전 저장값도 읽음)
- **씬이 바뀔 때마다 해상도·화면 모드를 다시 적용.** 설정 창이 씬마다 있어서 타이틀 → 로비 → 게임으로 넘어갈 때마다 `Screen.SetResolution` 호출. [ResolutionSetting.cs `Awake`] → 실행 후 한 번만 적용
- 설정 창이 열린 상태에서 열기가 한 번 더 호출되면 이동 잠금이 두 번 걸려 닫아도 풀리지 않음 [SettingManager.cs `ActiveSettingUI`]

### 경고 (잠복) — 수정함
- 저장된 키 값이 손상됐거나 읽을 수 없으면 `Enum.Parse` 예외로 키 초기화가 중단 → 이동 키가 비어 플레이어 이동에서 예외 [KeySetting.cs `InitKeySetting`]
- `AudioManager`가 없는 상태(Start 씬을 거치지 않고 다른 씬에서 바로 실행)에서 소리 설정을 건드리면 예외 [SoundSetting.cs]
- `AudioManager.PlayUISfx`: 클립이 등록되지 않았는데 호출하면 예외. 볼륨 설정도 초기화 전이면 예외. 배경음 효과(`EffectBgm`)는 첫 씬의 카메라만 기억해 씬이 바뀌면 동작하지 않음 [AudioManager.cs]
- 저장 대상(해상도/키/소리/음성) 중 하나라도 연결이 빠지면 나머지도 저장되지 않음 [SettingManager.cs `SaveAllToPrefs`]

### 제안
- **쓰이지 않는 "변경 감지" 코드.** `KeySetting`/`SoundSetting`/`ResolutionSetting`의 `HasUnsavedChanges`, `OnDirtyChanged`, `CheckDirty`, `RevertToSnapshot`은 구독·호출하는 곳이 없음. 예전의 "적용/취소 확인" 방식을 "닫으면 저장"으로 바꾸면서 남은 것으로 보임. 세 클래스에 같은 구조가 반복됨 → 지우거나 공통 베이스로.
- **소리 크기가 이중으로 줄어듦.** 전체 음량은 `AudioListener.volume`에, 개별 음량은 각 재생기에 따로 적용되는데, 개별 음량은 전체 음량 이하로만 올릴 수 있음. 전체 70 / 배경음 70이면 실제 배경음은 49%. 의도인지 확인 필요.
- `AudioManager`와 `SoundSetting`이 같은 저장 키 이름과 기본값(70)을 각자 들고 있음. `AudioManager`의 `EffectSfx`/`UISfx` 항목명은 다른 프로젝트의 것(`fishBite`, `dungeonList` 등)이고 호출부는 전부 주석 처리 — 현재 효과음은 실제로 쓰이지 않음(클립 0개).
- `KeySetting.keys`(static)는 설정 창의 `Awake`에서만 채워짐 → 설정 창이 없는 씬에서 플레이어를 움직이면 예외. 현재 플레이 가능한 씬에는 모두 설정 창이 있어 문제없음(MCP 확인).
- `SettingsTabController`: `tabs`가 비어 있으면 `Awake`에서 예외. 주석("설정 UI가 켜질 때")과 달리 컴포넌트가 항상 켜진 루트에 있어 씬 로드 때 한 번만 실행됨.
- 사용하지 않는 `using Mirror.BouncyCastle.Tsp` 제거함. 키 이름표의 글자 크기(46/25)를 인스펙터로 뺌. `\` 키가 "|"로 표시되던 것 수정.

### MCP로 확인한 것
- `Setting UI`는 타이틀·로비·게임 씬에 있고 루트가 항상 켜져 있음. 설정 스크립트들의 인스펙터 참조는 전부 연결됨(소리 설정의 음소거 토글 4개는 원본 없는 프리팹의 자리표시 컴포넌트에 연결된 상태).
- `AudioManager`는 Start 씬에만 있고 씬을 넘어 유지됨. 배경음 클립 없음, 효과음·UI음 클립 0개.
- **탭 버튼 5개의 "없는 메서드" 연결은 실행에 영향 없음.** 유니티가 없는 메서드 연결은 건너뛰고, 탭 전환은 `SettingsTabController`가 코드로 따로 연결함. 정리하려면 `Setting UI.prefab`을 저장해야 하는데, 원본 없는 토글 프리팹 4개가 들어 있어 **음소거 토글 프리팹을 다른 PC에서 가져온 뒤에** 정리하는 것이 안전 → 미조치.
- 플레이 모드: 설정 창 중복 열기 후 닫기 → 잠금 정상 해제, 키 입력 대기 취소 → 다음 프레임에 ESC 가능, 대기 중 컴포넌트가 꺼져도 정리됨, 키 맞바꾸기·되돌리기 정상, 닫을 때 해상도가 가로·세로로 저장됨, 예외 없음.

### 남은 것 / 논의 필요
1. 음성(Vivox) 전체 — 구현이 정리되면 별도 리뷰.
2. `Setting UI.prefab` 정리(없는 메서드 연결 5개, 음소거 토글 프리팹 4개) — 토글 프리팹을 가져온 뒤.
3. 소리 크기 이중 적용이 의도인지.
4. 쓰이지 않는 "변경 감지/되돌리기" 코드를 지울지.

### 다음 세션에서 볼 것
- 세션 10(데이터·유틸·미커밋 변경 정리): 구식 API 경고 26건, 프로젝트 전반의 사용하지 않는 `using`(`Unity.VisualScripting*`), 이름 오타(`GameMamager`, `NickNamemanager`), git에 걸린 변경분 정리

### 세션 9 후속 조치 (2026-10-09, 사용자 결정 반영)
- ✅ **소리 크기 이중 적용 수정** [SoundSetting.cs `ApplyAllVolumes`] — 전체 음량은 `AudioListener`가 모든 소리에 곱하므로, 개별 재생기에는 "개별 음량 ÷ 전체 음량"을 넘겨 개별 슬라이더 값이 그대로 실제 크기가 되게 함. 확인(플레이 모드): 전체 80 / 배경음 40 → 실제 40%(예전 방식이면 32%), 전체를 30으로 내리면 배경음도 30으로 따라 내려가고 실제 30%, 전체 0이면 0.
- ✅ **쓰이지 않는 "변경 감지/되돌리기" 코드 삭제** — `KeySetting`, `SoundSetting`, `ResolutionSetting`의 `HasUnsavedChanges`, `OnDirtyChanged`, `CaptureSnapshot`, `CheckDirty`, `MarkDirty`, `RevertToSnapshot`, 스냅샷 구조체. 음성 쪽(`VoiceSetting`)의 같은 코드는 음성 작업 때 함께 정리하도록 그대로 둠.

### 음성(Vivox) 현황 점검 (2026-10-09, 코드만 확인 — 수정 없음)
사용자 요청으로 "지금 상태에서 멀티플레이 마이크 기능을 구현할 수 있는지"를 확인. 대상: `VoiceManager`, `InGameVoiceProximity`, 호출부(`RoomManager`, `GamePlayer`), 패키지 설정.

**기반은 갖춰져 있음**
- Vivox 16.11.0 / Authentication 3.7.3 / Services Core 1.18.0 설치, 유니티 클라우드 프로젝트 연결됨. 이 PC에서 익명 로그인과 Vivox 로그인 성공, 입출력 장치 목록 조회 성공(콘솔 로그로 확인).
- 채널 구조(로비 `lobby_방코드` / 생존자 `alive_방코드` / 유령 `dead_방코드`)와 입장·퇴장 직렬화(잠금) 코드가 있음.

**지금 동작하지 않거나 의도와 다르게 동작하는 것**
1. **근접 음성이 실제로는 한 번도 실행되지 않음.** `InGameVoiceProximity`를 실행 중에 `AddComponent`로 붙이는데, 플레이어 참조(`localPlayer`)는 에디터의 `Reset`에서만 채워져 실행 중에는 항상 비어 있음 → 매번 바로 반환. 그래서 게임 중에는 **거리와 무관하게 전원의 목소리가 그대로 들림.**
2. **참가자 ↔ 캐릭터 연결 방식이 성립하지 않음.** 근접 판정은 Vivox 표시 이름을 "닉네임|netId"로 읽어 캐릭터를 찾는데, 표시 이름은 로그인할 때 한 번만 정해짐. 실제로는 게임 시작 시 "Player_무작위"로 로그인하고 이후 요청은 무시됨(로그: "DisplayName differs (ignored while logged in)"). 1번을 고치면 이번에는 **전원이 음소거**됨.
3. **게임 시작 시 채널 전환이 뒤섞임.** `GamePlayer.OnStartLocalPlayer`는 "죽은 상태"로 채널에 들어가고(유령 채널로 송신), `RoomManager`는 같은 시점에 "산 상태"로 들어감. 두 요청의 순서에 따라 살아 있는 플레이어가 유령 채널에 말하게 될 수 있음. 로비에서는 로비 채널 대신 생존자 채널에 들어감(로비 입장 코드가 주석 처리).
4. **같은 PC에서 여러 개를 띄우면 서로 같은 계정으로 로그인됨.** 익명 로그인 정보가 PC에 저장되기 때문. Multiplayer Play Mode/ParrelSync 테스트에서는 인스턴스마다 다른 프로필을 쓰게 해야 서로 들림.
5. 방을 나갈 때 이미 떠난 채널에 음량을 설정하려다 Vivox 에러 2건(이전에 확인). 마이크 끄기(음소거)·눌러서 말하기 기능 없음. 시야 거리가 실제 시야 시스템과 따로 하드코딩(낮 11 / 밤 포식자 6 / 다람쥐 11 / 그 외 8).

**결론: 구현 가능.** 서비스 연결·로그인·채널 입장은 이미 되므로 새로 만들 것은 "누가 어느 캐릭터인지 연결"과 "채널 전환 정리"임. 필요한 작업:
- 각 클라이언트가 자신의 Vivox 플레이어 ID를 서버에 알리고 플레이어 오브젝트에 동기화(SyncVar) → 근접 판정은 이 ID로 캐릭터를 찾음
- `InGameVoiceProximity` 초기화 수정, 시야 거리는 실제 시야 값 사용
- 채널 전환을 한 곳에서만 하도록 정리(로비 → 로비 채널 / 게임 → 생존자 채널 / 사망 → 유령 채널 / 로비 복귀·방 나가기)
- 같은 PC 다중 실행용 프로필 분리, 나갈 때 에러 정리, (선택) 마이크 끄기·눌러서 말하기
- 한계: 음성이 실제로 들리는지는 마이크가 있는 인스턴스 2개 이상으로 사용자가 직접 확인해야 함. 근접 판정은 듣는 쪽 클라이언트가 음량을 조절하는 방식이라 변조된 클라이언트는 전원의 목소리를 들을 수 있음(서버가 강제하려면 Vivox 위치 기반 채널로 바꿔야 하고, 그 경우 벽에 가려짐은 반영되지 않음).

### 음성(Vivox) 멀티플레이 마이크 구현 (2026-10-09)
사용자 결정: ① 생존자는 시야보다 조금 넓은 범위 + 벽에 가리면 안 들림 ② 로비는 거리와 상관없이 전원 ③ 유령끼리는 전부 들리고 가까운 생존자도 들림, 생존자는 유령을 못 들음 ④ 기본은 항상 켜짐, 눌러서 말하기와 음소거도 제공.

**정정**: 앞의 현황 점검에서 "근접 판정을 실행 중에 `AddComponent`로 붙인다"고 적었으나, 실제로는 `GamePlayer.prefab`의 자식 `Prox`에 이미 붙어 있었고 플레이어 참조만 비어 있었음(그래서 동작하지 않은 것은 같음). `GamePlayer`가 `GetComponent`로 자식을 못 찾아 하나를 더 붙이던 것.

**바꾼 것**
- `VoiceManager.cs` (다시 작성)
  - **채널 전환을 한 곳으로**: `JoinLobby(방코드)` / `EnterGameplay(방코드, 생존 여부)` / `LeaveAllChannels()`. 요청이 겹치면 진행 중인 전환이 끝난 뒤 **마지막 요청**으로 맞춤(순서가 뒤섞여도 결과가 같음). 유령은 유령 채널에만 말하도록 송신 채널을 하나로 고정(`SetChannelTransmissionModeAsync(Single)`).
  - **음성 ID**: 로그인 후 `LocalVoiceId`(Vivox 참가자 ID와 같은 값)를 제공. 표시 이름에 닉네임·netId를 넣던 방식(`BuildVivoxDisplayName` 등)은 삭제.
  - **같은 PC 다중 실행**: 실행 순서대로 번호표(시스템 전역 Mutex)를 잡아 두 번째 인스턴스부터 다른 인증 프로필(`instance1`, `instance2`…)로 로그인. 첫 번째는 기존 계정(`default`) 그대로.
  - **마이크**: 방식(항상 켜짐 / 눌러서 말하기, PlayerPrefs `Voice_MicMode`에 저장), 음소거(실행할 때마다 꺼진 상태로 시작), 키(기본 V = 눌러서 말하기, M = 음소거, `Start` 씬의 VoiceManager 인스펙터에서 변경). 글자 입력 중에는 키가 동작하지 않음. 키를 뗀 뒤 0.2초 뒤에 마이크가 꺼짐(말끝 잘림 방지, 인스펙터 값).
  - 참가자가 채널에 들어올 때 음량 초기화: 생존자 채널은 무음에서 시작, 로비·유령 채널은 내가 정해 둔 개별 음량.
- `InGameVoiceProximity.cs` (다시 작성): 생존자 채널 참가자를 음성 ID로 캐릭터와 연결해 0.1초마다 음량 조절.
  - 생존자: `현재 실제 시야 반경(FOV 값, 낮/밤·능력 반영) + 1.5` 안이고 벽(Occluder 레이어)에 가리지 않은 생존자만 들림. 가장자리 1.0 구간에서 서서히 작아짐.
  - 유령: 3.5 거리 안의 생존자가 들림(벽 무시). 유령 채널은 항상 들림.
  - 누구인지 알 수 없는 참가자와 죽은 참가자는 무음. 내가 정한 개별 음량에 거리 감쇠를 더함.
  - 수치는 모두 `GamePlayer.prefab`의 `Prox` 오브젝트 인스펙터에서 조절.
- `GamePlayer.cs` / `RoomPlayer.cs`: `[SyncVar] voiceId` 추가, 로그인되면 `CmdSetVoiceId`로 서버에 알림(형식 검사, 다른 플레이어와 같은 ID 거부). `GamePlayer.AllPlayers`(클라이언트에 있는 플레이어 목록) 추가. 게임 시작 시 `EnterGameplay(생존 여부)`, 사망 시 유령 채널로 전환. 근접 판정은 죽은 뒤에도 켜 둠(예전에는 사망 시 꺼서 유령의 "가까운 생존자 듣기"가 동작하지 않았음).
- `RoomManager.cs`: 로비 씬에서 `JoinLobby`(예전에는 생존자 채널에 들어가던 것), 게임 채널 입장 코루틴 삭제(GamePlayer가 담당), `GamePlayer` 생성 시 `voiceId` 넘김.
- `VoiceSetting.cs`: 출력 음량을 장치 음량에만 적용(채널 음량에도 같이 적용해 **두 번 줄어들던 것**과, 방을 나갈 때 이미 떠난 채널에 적용하려다 나던 **Vivox 에러 2건**의 원인). 마이크 방식/음소거 토글용 선택 필드 추가(Setting UI 프리팹 정리 때 연결 예정, 지금은 비어 있음).
- `LobbyVoiceParticipantRow.cs` / `VoiceUI.cs`: 닉네임을 음성 ID로 찾음. 게임 중 생존자 채널에서는 개별 음량을 직접 적용하지 않음(근접 조절을 0.25초마다 덮어쓰던 문제). "전체 기본값" 버튼이 참가자 음량을 +20dB로 올리던 계산 오류 수정.
- 새 파일 `MicStatusUI.cs`, 프리팹 `Assets/Prefab/UI/Mic Status UI.prefab`: 화면 위쪽 가운데의 마이크 상태 표시(누르면 음소거, 아래 버튼으로 방식 전환). `GameRoom`의 `UI Canvas`, `GamePlay`의 `Canvas`에 배치(MCP).
- `GamePlayer.prefab`(MCP): `Prox`의 벽 레이어 = Occluder, 플레이어 참조 연결, 음량 변화 속도 150 / 100.

**확인한 것 (에디터, 방장 1명)**
- 컴파일 에러 없음. 시작 → 로그인 성공, 음성 ID 발급, 장치 목록(입력 4 / 출력 5).
- 로비: `lobby_방코드` 입장·송신, RoomPlayer의 `voiceId` 동기화, ID로 닉네임 찾기 성공.
- 게임 시작: `alive_방코드`만 입장·송신, GamePlayer `voiceId` 전달됨, 근접 판정 켜짐, 시야 반경 11.1 읽힘.
- 유령 상태: `dead_`와 `alive_` 두 채널에 들어가고 송신은 `dead_`만.
- 로비 복귀 → `lobby_`로 복귀. 전환 요청을 연달아 보내도 마지막 요청 상태로 끝남.
- 음소거 / 눌러서 말하기 전환 시 Vivox 입력 장치 음소거 상태가 따라 바뀜. 상태 표시 문구 정상.
- 방 나가기(로비에서, 게임 중에): 채널 전부 나감, Vivox 에러 없음.

**확인하지 못한 것** (review-tests-2.md 음성 항목)
- 실제로 소리가 들리는지, 거리·벽에 따른 들림/안 들림, 유령 규칙 — 인스턴스 2개 이상 + 마이크 필요.
- 같은 PC에서 두 번째 인스턴스가 다른 계정으로 로그인되는지(번호표 방식은 코드만 작성).
- 빌드에서의 동작.

**남은 것 / 알아 둘 것**
- 근접 판정은 듣는 쪽 클라이언트가 음량을 줄이는 방식이라, 변조된 클라이언트는 생존자 전원의 목소리를 들을 수 있음.
- 음성 키(V, M)는 설정 창의 키 설정에 없음. 마이크 방식/음소거 토글도 설정 창에는 없고 화면 표시로만 바꿈 → Setting UI 프리팹 정리 때 함께 추가.
- `VoiceSetting`의 변경 감지 코드(`CaptureSnapshot` / `RevertToSnapshot`)는 그대로 둠.
- `Assets/Script/Setting/VivoxSmokeTest.cs`는 어디에도 붙어 있지 않은 시험용 스크립트(삭제 여부는 사용자 결정).
- 게임 씬에 들어간 뒤 내 캐릭터가 생성될 때까지(짧은 시간)는 로비 채널에 남아 있어 전원이 서로 들림.

## [2026-10-09] 세션 10 — 데이터 계층 & 기타 유틸, 미커밋 변경 점검

범위: `Assets/Script/Data/*`(10개), `Assets/Script/Utill/*`, git 미커밋 변경 중 스크립트 외(`ProjectSettings/*`, `Packages/manifest.json`, `.gitignore`, 추적되지 않은 파일).

### 치명적
- 없음.

### 경고
- **[저장소] 유니티 버전 업그레이드(2022.3.21f1 → 6000.3.22f1)가 커밋되지 않은 상태** — `ProjectVersion.txt`, 패키지 버전(URP 14 → 17, Vivox 16.9 → 16.11, NGO 1.8 → 2.13 등), TMP 셰이더, 프로젝트 설정 변경이 전부 여기서 나온 것. 리뷰 수정분과 한 덩어리로 섞여 있어, 지금 상태에서 문제가 생기면 "업그레이드 탓인지 수정 탓인지" 가려내기 어렵다 → **업그레이드분을 먼저 따로 커밋**하기를 권장. (보류: 커밋은 사용자 결정)
- **[.gitignore] `*.json`이 통째로 무시됨** — 이미 추적 중인 `manifest.json` 등은 괜찮지만, 앞으로 새로 만드는 JSON(데이터 파일, 새 설정 파일)은 조용히 커밋에서 빠진다. `config.json`만 무시하려던 것이라면 그 파일만 지정하는 편이 안전. (보류)
- **[.gitignore] `Assets/Mirror/`가 무시 목록에 있는데 2,270개 파일이 이미 추적 중** — 기존 파일의 수정은 잡히지만, Mirror를 올릴 때 새로 생기는 파일은 커밋되지 않아 다른 PC에서 받으면 깨진다. 추적할지(무시 규칙 삭제) 말지(추적 해제) 하나로 정해야 함. (보류)
- **[Packages/manifest.json] 쓰지 않는 Netcode for GameObjects(2.13.1)와 Multiplayer Center가 들어 있음** — 이 프로젝트는 Mirror를 쓰고, 코드에서 `Unity.Netcode`를 쓰는 곳이 없다. 컴파일 시간과 빌드 크기만 늘리고, 같은 이름(`NetworkManager`, `NetworkBehaviour`)의 클래스가 두 벌 있어 자동완성에서 잘못 고르기 쉽다 → 제거 권장. (보류)
- **[Data/UserIdManager.cs] `userId`가 사실상 쓰이지 않음** — 클라이언트가 만들어 보내는 값이고 서버에서는 로그 한 줄에만 쓰인다. 빌드에서는 같은 PC의 모든 인스턴스가 같은 값(`user_id_main`). 재접속 식별 등에 쓸 계획이 없다면 삭제 후보, 쓸 계획이라면 서버가 발급해야 함. (보류)

### 제안
- ✅ **지원 종료 예정 API 경고 26건 → 0건** — `FindObjectOfType` → `FindFirstObjectByType`(14곳), `FindObjectsOfType` → `FindObjectsByType(…, FindObjectsSortMode.None)`(7곳), TMP `enableWordWrapping` → `textWrappingMode`(2곳). 여러 개를 찾는 7곳은 결과 순서에 기대지 않는 것을 확인(전부 순회하거나 최솟값을 고름).
- ✅ **쓰이지 않는 `using` 삭제(15개 파일)** — `Unity.VisualScripting*`, `UnityEngine.SocialPlatforms`, `UnityEngine.TextCore.Text`, `static UnityEngine.EventSystems.EventTrigger`. 자동완성이 잘못 넣은 것들로, 엉뚱한 확장 메서드가 섞여 들어올 여지를 없앰.
- ✅ **[Data/InvestigationLog.cs] 파괴될 때 `Instance`를 비움** — 씬이 바뀐 뒤 파괴된 오브젝트를 가리키는 참조가 남던 것.
- ✅ **[ProjectSettings] `Run In Background` 켬** — Mirror가 실행 중에 켜 주기는 하지만(RoomManager 프리팹 값), 그 전 씬에서도 창이 가려졌을 때 멈추지 않도록 프로젝트 설정도 맞춤. (에디터에서 창이 가려지면 멈추는 현상은 이 설정과 무관한 에디터 동작이었음)
- [Data/MissionData.cs] `BadgerB`~`BadgerE` 이름이 없어 화면에 영문 그대로(`BadgerB`) 나온다. `Fruit_A`/`Fruit_B`, `Wood_A`/`Wood_B`는 이름이 같아 목록에서 구분되지 않는다. (보류: 문구는 기획 결정)
- [Data/CharacterData.cs, CharacterZoneData.cs] 거주지가 두 곳에 따로 적혀 있음(`TerritoryType` / `ZoneType`, 지금은 값이 일치). 한쪽만 고치면 어긋난다 → 하나로 합치기. (보류)
- [Data/ConfigManager.cs] 서버 주소가 `http://IP:3000`으로 고정(포트 하드코딩, 암호화 없음). 프로젝트 설정은 HTTP 허용 상태라 지금은 동작한다. 실제 서비스 전에는 포트를 설정 파일로 빼고 HTTPS 검토. (보류: node-server-checklist.md와 함께)
- [Data/NickNamemanager.cs, RoomSessionData.cs, PlayerAbility.cs, InvestigationRecord.cs, Utill/TextSanitizer.cs] 문제 없음. `PlayerAbility.cs`의 주석 처리된 `OnAfterDeath`는 죽은 코드.
- **이름 오타** — `GameMamager`(7개 파일에서 사용), `NickNamemanager`(3), `WinCondutionFactory`(2), `hasEate`, 폴더 `Utill`. 동작에는 영향 없음. 지금 바꾸면 이미 큰 미커밋 변경에 이름 변경이 섞이므로 **커밋 후 이름 변경만 따로 한 번에** 하기를 권장. (보류)
- [저장소] 추적되지 않은 파일: `Assets/TextMesh Pro/Examples & Extras/`(6.3MB, 예제 묶음 — 쓰지 않으면 삭제), `Food_Chain_Game.slnx`(자동 생성 — `.gitignore`에 `*.slnx` 추가), `.claude/`(명령 파일 — 공유할지 결정), `Assets/DefaultVolumeProfile.asset`·`ProjectSettings/MultiplayerManager.asset`(유니티 6이 만든 것 — 커밋). (보류)
- [ProjectSettings] 회사 이름이 `DefaultCompany`. 저장 데이터(PlayerPrefs) 위치가 이 이름을 따라가므로, 배포 후에 바꾸면 기존 설정이 초기화된다 → 배포 전에 정하기. (보류)
- `com.coplaydev.unity-mcp`가 `#main`(버전 고정 없음)으로 들어가 있어 받을 때마다 내용이 달라질 수 있음. 에디터 전용이라 게임에는 영향 없음.

### 확인
- 캐시를 지운 전체 다시 컴파일: 에러 0, C# 경고 0(Weaver 포함).
- 에디터 방장 1명: 시작 → 방 생성 → 로비 → 게임 시작까지 예외 없음(콘솔 에러는 Node 서버가 꺼져 있어 나는 "[Room] 출입 실패" 1건뿐).

### 다음에 볼 것
- 세션 6~10 리뷰 종료. 남은 것은 `review-tests.md` / `review-tests-2.md`의 플레이 테스트, 보류 항목에 대한 사용자 결정, 테스트 통과 후의 리팩토링(R-1~R-4, R-6).

### 세션 10 후속 조치 (2026-10-09, 사용자 결정 반영)
- ✅ **유니티 업그레이드분 커밋** (`3f9dc04`) — 프로젝트 설정, 패키지, URP 에셋, TMP 셰이더/리소스 46개 파일. 리뷰에서 바꾼 두 줄(`Run In Background`, TMP 한글 대체 폰트)은 이 커밋에서 빼고 작업 폴더에 남김. 씬·프리팹은 업그레이드 재저장분과 리뷰 수정이 한 파일에 섞여 있어 포함하지 않음.
- ✅ **Mirror 폴더 추적 해제 커밋** (`c974500`) — `Assets/Mirror/` 2,270개 파일을 저장소에서만 뺌(로컬 파일은 그대로, `Assets/Mirror.meta`는 유지). **주의: 다른 PC에서 이 커밋을 받으면 그 PC의 `Assets/Mirror` 폴더가 지워진다 → 받기 전에 폴더를 따로 복사해 두거나, 받은 뒤 Mirror 96.0.1을 다시 가져와야 함.** 새로 받는 PC도 마찬가지.
- ✅ **쓰지 않는 패키지 제거** — `com.unity.netcode.gameobjects`, `com.unity.multiplayer.center`를 `Packages/manifest.json`에서 삭제. Mirror의 Weaver는 자체 Cecil(`Mono.CecilX`)을 써서 영향 없음. 제거 직후 에셋 임포트 보조 프로세스가 2번 죽었다는 로그가 났으나(패키지가 빠지는 도중의 일시 현상으로 보임) 다시 새로 고침했을 때는 재발하지 않음.
- ✅ **`userId` 제거** — `UserIdManager.cs` 삭제, `RoomPlayer.userId` / `CmdSetUserId` / `maxUserIdLength` 삭제. `CmdSetUserId` 안에 있던 **방장 다시 정하기와 시작 버튼 갱신**은 `CmdSetData`(입장 시 닉네임 등록)로 옮김 — 지우기만 했다면 방장이 정해지지 않았을 것.
- 확인: 컴파일 에러 0. 에디터 방장 1명으로 방 생성 → 방장 지정됨, 시작 버튼 표시됨.
- 보류(사용자 결정): 이름 오타는 나중에 수정, 미션 이름은 미구현이라 그대로 둠. `config.json` 처리 방식은 방안 제시 후 답변 대기.

## [2026-10-09] 해상도 대응 (사용자 요청: 모든 해상도에서 UI가 보이고 게임이 진행되도록)

### 원인
- 캔버스 6개 중 3개(`Start` 씬 `Canvas`, `GamePlay`의 `ICanvas`·`Mission Canvas`)가 **고정 픽셀 크기** 방식이라 화면 크기가 달라져도 UI 크기가 그대로였음. 미션 패널은 "화면에서 좌우 600, 상하 300을 뺀 크기"로 잡혀 있어, 1920x1080에서는 1320x780이지만 1280x720에서는 680x420이 되고 그 안의 물건들은 원래 크기 그대로라 패널 밖으로 넘침.
- 나머지 3개(`Title`, `GameRoom`, `GamePlay`의 `Canvas`)는 화면 크기에 맞춰 커지긴 했지만 "가로·세로 중간값" 기준이라 16:9가 아닌 화면에서는 가로 또는 세로가 1920x1080보다 좁아져 잘림(에디터 게임 뷰처럼 납작한 창에서는 세로가 잘림).
- 미션 스크립트의 좌표 계산은 대부분 변환 함수를 거치고 있어 배율이 바뀌어도 문제없음(54개 파일 확인). 예외 1곳만 수정.

### 수정
- **모든 화면 UI 캔버스 6개**: 화면 크기에 맞춰 확대/축소, 기준 1920x1080, **"기준 화면이 항상 다 들어오게"(Expand)** 방식으로 통일. 어떤 해상도·비율에서도 1920x1080 기준 배치가 통째로 화면 안에 들어오고, 남는 공간은 가장자리에 붙은 UI가 바깥쪽으로 벌어짐. (MCP)
- **미션 프리팹 18개**: 패널을 1320x780 고정 크기(가운데 정렬)로 변경. 화면 비율과 상관없이 1920x1080에서 만든 배치 그대로 나옴. (MCP)
- `MeatB.cs`: 고기 조각을 접시 위로 올리는 간격에 캔버스 배율을 곱함(화면 좌표에 캔버스 단위 값을 그대로 더하던 것).
- 겹침 정리: 마이크 상태 표시를 위쪽 가운데 → **아래쪽 가운데**로 이동(게임 중 남은 시간 표시와 겹쳤음). `GamePlay`의 날짜 표시(`Round Text`)를 왼쪽으로 160 이동(Scan 버튼과 겹쳐 있었음 — 1920x1080에서도 겹치던 것).

### 확인
- 배치 검사(에디터): 4개 씬의 캔버스를 16:9, 16:10, 4:3, 5:4, 21:9, 32:9 크기로 바꿔 가며 모든 UI 요소가 화면 안에 있는지 계산 → 새로 화면 밖으로 나가는 요소 없음.
- 실행 검사(에디터 게임 뷰 해상도 변경): 미션 18개를 1024x768에서 하나씩 띄워 모든 요소가 화면 안(패널 704x416 = 1320x780의 0.533배)에 있는 것을 확인.
- 화면 캡처로 눈으로 확인: 타이틀(1280x720), 로비(1024x768), 설정 창(1280x1024), 게임 화면(1024x768), 미션(씨앗 A 1024x768 / 벌레 A 1920x1080 / 물고기 A 2560x1080 / 고기 B 1280x1024).
- 확인하지 못한 것: 미션을 실제로 손으로 조작해 끝까지 진행하는 것, 빌드에서 설정 창으로 해상도를 바꾸는 것, 16:9 미만으로 아주 작은 해상도(800x600 등)에서 글자가 읽을 만한 크기인지.

### 남은 것 / 알아 둘 것
- 게임 세계(카메라)는 세로 범위가 고정이라 가로로 넓은 화면일수록 좌우가 더 많이 보임. 다만 시야는 시야 반경으로 따로 가려지므로 유불리는 제한적. 필요하면 가로 기준 고정이나 좌우 가림막을 추가.
- 미션 중에는 미션 패널이 왼쪽 "해야 할 일" 목록 일부를 가림(1920x1080에서도 같음, 기존 배치).
- Mirror 디버그 HUD(왼쪽 위)는 화면 크기에 따라 커지지 않고, 캐릭터 이름·방 나가기 버튼과 겹침(유지하기로 한 임시 HUD).
- 로비·게임 맵 바깥(파란색/검은색 빈 공간)이 화면에 보임 — 맵 크기와 카메라 문제로 UI와는 별개.


---

## [2026-10-10] Node 서버 리뷰 — 방 코드 서버 (`room-code-server/server.js`)

사용자가 서버 코드를 전달해 `node-server-checklist.md` 순서대로 점검. N-1~N-5는 같은 날 사용자 결정으로 수정함(아래 "조치 상태").

**범위:** `C:\Users\USER\room-code-server\server.js`(183줄, 저장소 밖), `package.json`, `rooms.db`(읽기 전용 조회), 대조용 Unity 쪽 `RoomHost.cs`, `RoomJoin.cs`, `TitleUI.StartHost`, `RoomManager`의 입장/퇴장 보고부

**구성:** Express 5.1.0 + sqlite3 5.1.7(파일 DB `rooms.db`) + cors 2.8.5. 포트 3000, `0.0.0.0` 바인딩, 평문 HTTP. git 저장소 아님.

### API 대조 (클라이언트 ↔ 서버)
- `POST /register` 일치. `POST /comeandgoing` 일치. `GET /lookup/:code` 일치(본문은 따옴표·개행 없는 IP 문자열, 403 기준은 `current >= max`).
- **`/delete` 불일치:** 클라이언트는 `GET /delete/{code}`(`RoomHost.cs:81`), 서버는 `app.delete`(`server.js:157`) → 클라이언트의 삭제 요청은 항상 404.

### 치명적 → 조치 상태: 전부 수정함
- **N-1. [server.js:47,62] 호스트 주소로 요청 본문의 `ip`를 그대로 저장(A-3 확정).** 클라이언트는 `config.json`의 IP(= Node 서버 주소)를 보내므로(`TitleUI.cs:217`), 입장자는 호스트가 아니라 Node 서버 기기로 접속한다. `rooms.db`에 남은 127개 방의 `ip`가 전부 같은 값(`192.168.219.100`)인 것으로도 확인됨. 호스트와 Node 서버가 같은 기기일 때만 동작.
- **N-2. [server.js:113-149] 인원수 갱신이 "읽고 → 계산해서 → 쓰기"로 나뉘어 있어 동시 요청에서 집계가 어긋남.** 두 요청이 같은 값을 읽으면 한 번만 반영된다. 방장이 방을 닫을 때처럼 −1이 한꺼번에 몰리면 0에 도달하지 못해 방이 남고, 반대로 +1이 누락된 상태에서 누가 나가면 사람이 남아 있는데 방이 삭제된다.
- **N-3. [server.js:157 / RoomHost.cs:81] 방 삭제 요청이 서버에 닿지 않음(위 메서드 불일치).** 지금은 마지막 −1이 0 이하가 될 때의 삭제(`server.js:126`)에만 의존하고 있어 N-2로 집계가 어긋나면 지울 방법이 없다.
- **N-4. [server.js:13-32 전체] 죽은 방 만료 없음(B-2 확정).** 갱신 시각 컬럼·하트비트·정리 작업이 없고 파일 DB라 재시작해도 남는다. 실제로 `rooms.db`에 정리되지 않은 방이 **127개** 쌓여 있음.
- **N-5. [server.js:46-71, 108-151, 157-175] 인증·입력 검증 없음.** 방 코드만 알면 누구나 ① `INSERT OR REPLACE`로 같은 코드를 다시 등록해 입장자를 다른 주소로 보낼 수 있고 ② `delta: -999`로 방을 지우거나 큰 값으로 "가득 참"을 만들 수 있고 ③ `DELETE`로 방을 지울 수 있다. `delta`가 문자열이면 숫자 덧셈이 아니라 문자열 이어붙이기가 되고(`"1"` → `1` + `"1"` = `"11"`), 빠지면 `NaN`이 저장돼 정원 검사가 무력화된다.

### 경고
- [server.js:47,58] `maxPlayer` 누락 시 `max`가 NULL → `/lookup`이 항상 403. 범위 검증도 없음.
- [server.js:47,109] Express 5에서는 JSON 본문이 없으면 `req.body`가 `undefined` → 구조 분해에서 예외 → 500. `NODE_ENV`가 `production`이 아니면 응답에 스택(서버 파일 경로)이 노출됨.
- [server.js:13-19] DB 연결 실패를 로그만 남기고 서버를 계속 띄움. 처리되지 않은 예외/거부에 대한 처리(`process.on`) 없음.
- [server.js:94] 게임 진행 중 여부를 서버가 모름(체크리스트 항목 그대로) — 게임 중인 방도 200이 내려감.
- [server.js:7] `cors()` 전체 허용. Unity 독립 실행 클라이언트에는 필요 없고, 열어 두면 임의의 웹 페이지에서 이 API를 호출할 수 있음.
- 요청 횟수 제한·본문 크기 제한(기본 100kb) 미설정. 평문 HTTP.

### 제안
- [server.js:181] 포트·DB 경로 하드코딩 → 환경 변수. `rooms.db`가 실행 위치 기준 상대 경로라 다른 폴더에서 실행하면 새 DB가 만들어짐.
- [server.js:50,68,99] 방 코드(사실상 입장 비밀번호)와 IP를 매 요청 로그에 기록.
- [package.json] `main`이 `index.js`(실제는 `server.js`), `start` 스크립트 없음. 서버 폴더가 버전 관리되지 않음.
- 콜백 중첩 → `better-sqlite3`(동기) 또는 메모리 `Map` + TTL로 단순화 가능(방 정보는 재시작 후 남길 이유가 없음).

### 문제없는 것
- SQL은 전부 `?` 바인딩이라 인젝션 없음.
- `/lookup` 응답 형식·상태 코드(404/403/200)는 클라이언트 기대와 일치.
- 존재하지 않는 코드에 대한 `/comeandgoing`·`/delete`는 404로 처리.

### 조치 상태 (2026-10-10 수정, 사용자 결정: N-1~N-5 한 번에 수정 / 기존 방 삭제 가능 / 서버 코드를 저장소에 포함)
- ✅ **서버 코드를 저장소에 포함** — 저장소 루트 `Server/`(`server.js`, `package.json`, `package-lock.json`, `README.md`, `.gitignore`). 루트 `.gitignore`의 `*.json` 규칙에 `package.json`/`package-lock.json` 예외 추가. `node_modules`와 `rooms.db`는 제외. 원본 폴더(`C:\Users\USER\room-code-server`)는 건드리지 않음.
- ✅ **N-1 호스트 주소** — 서버가 본문의 `ip` 대신 `/register` 요청의 실제 접속 주소(`req.ip`)를 저장. 호스트가 서버와 같은 기기(루프백)면 `/lookup`은 입장자가 서버에 접속할 때 쓴 주소를 돌려줌. 프록시 뒤라면 `TRUST_PROXY` 환경 변수 필요. Unity는 `ip`를 보내지 않음.
- ✅ **N-2 인원수 집계** — `UPDATE ... SET current = MAX(0, MIN(max, current + ?))` 한 문장으로 갱신(0~정원 보정). 0명이 되면 삭제하는 동작은 유지.
- ✅ **N-3 방 삭제** — Unity가 `DELETE` 메서드로 요청. 서버가 내려갈 때(`RoomManager.OnStopServer`)도 직접 삭제 요청. 이미 지워진 방(404)은 성공으로 취급.
- ✅ **N-4 만료** — `updated_at` 컬럼 + `POST /heartbeat` 추가. 60초(`ROOM_TTL_SECONDS`) 동안 갱신이 없으면 30초 주기 정리 작업이 삭제하고, 만료된 방은 `/lookup`에서도 404. Unity는 호스트 시작 시 20초 간격(`RoomHost.heartbeatIntervalSeconds`) 하트비트 시작, 서버 종료/방 삭제 시 중단. 예전 구조의 `rooms` 테이블은 기동 시 지우고 새로 만듦(기존 127개 방 정리).
- ✅ **N-5 인증·검증** — 등록 시 토큰 발급(`RoomSessionData.RoomToken`), `/comeandgoing`·`/heartbeat`·`/delete`는 `X-Room-Token` 헤더가 맞아야 처리. 살아 있는 코드 재등록은 409(Unity가 새 코드로 최대 5회 재시도). `code` 형식, `maxPlayer` 1~20, `delta` ±1 검증, 본문 1kb 제한, 본문 없음/잘못된 JSON은 스택 없이 400.
- ✅ **함께 고친 Unity 쪽 항목(체크리스트)** — 등록 성공을 확인한 뒤 호스트 시작(실패 시 팝업), 웹 요청 타임아웃(`requestTimeoutSeconds`), 방 코드 URL 이스케이프와 형식 검증(소문자·공백 보정), `/lookup` 응답 `Trim`, 응답 코드 분기 정리, `UnityWebRequest`를 `using`으로 해제.
- 변경 파일: `Server/*`, `.gitignore`, `RoomHost.cs`, `RoomJoin.cs`, `RoomSessionData.cs`, `TitleUI.cs`(`StartHost`), `RoomManager.cs`(`OnStartServer`, `OnStopServer`)

### 확인
- Unity MCP로 스크립트 재컴파일 → 콘솔에 C# 오류/경고 없음.
- **서버는 실행해 보지 못함** — 이 PC에 Node.js가 없어 구문 검사도 못 했다. 플레이 테스트도 하지 않음. 항목은 `review-tests-2.md`의 "Node 서버 리뷰"(N-0~N-6).

### 남은 것 / 알아 둘 것
- **방 만들기에 Node 서버가 필수가 됨.** 전에는 등록이 실패해도 방이 열렸지만 이제는 팝업을 띄우고 열지 않는다. 에디터 테스트 때도 서버가 떠 있어야 함.
- 호스트가 Node 서버와 **같은 공유기 안의 다른 기기**면 사설 IP가 저장되어 외부 입장자는 접속할 수 없음(같은 공유기 안에서는 동작). 외부 접속은 호스트의 UDP 7777 개방도 필요 — 릴레이/전용 서버 도입 여부는 미정.
- 미조치 경고: 게임 진행 중 상태를 서버가 모름, `cors()` 전체 허용, 요청 횟수 제한 없음, 평문 HTTP, 포트 3000이 Unity 쪽에 하드코딩, 요청 로그에 방 코드·IP 기록.
- `CodeManager`의 `Instance`/`RegisterRoom`/`GetIpFromCode`/`codeToIp`는 이제 쓰는 곳이 없음(`GenerateCode`만 사용) — 정리 후보.

### 체크리스트 재점검 (2026-10-10, 수정 후 코드를 `node-server-checklist.md` 항목별로 다시 대조)

**새로 찾아 고친 것**
- [Server/server.js 기동부] **서버를 60초 넘게 내렸다 올리면 살아 있는 방까지 전부 지워짐**(체크리스트 "서버 재시작 시 동작"). 꺼져 있던 동안 하트비트를 못 받았는데 기동 직후 만료 정리를 돌렸기 때문. 호스트는 경고 로그만 보고, 로비에 표시된 코드는 영영 404 → 기동 시 남아 있는 방의 갱신 시각을 현재로 맞추고 그때부터 만료를 세도록 수정(죽은 방은 TTL 뒤에 정리됨).
- [RoomHost.cs `DeleteRoom`] **삭제 요청을 코루틴으로 보내고 있었는데, 삭제 시점은 서버가 내려가는 중**이고 Mirror가 그 직후 타이틀 씬을 로드하면서 `RoomManager` 오브젝트(와 그 위의 코루틴)를 파괴함 → 응답 처리와 요청 해제가 실행되지 않음. 완료 콜백 방식으로 바꿔 오브젝트가 사라져도 끝까지 처리되게 함.
- [Server/server.js 각 라우트] 검증 전의 요청 값(`code`, `delta` 등 임의 문자열)을 로그에 그대로 찍던 것 → 검증을 통과한 뒤에만 기록. 컬럼명 `max`가 SQL 함수 `MAX`/`MIN`과 섞여 보이던 곳은 따옴표로 구분.

**항목별 결과**
| 체크리스트 항목 | 결과 |
|---|---|
| A-3 호스트 주소 | 충족. 프록시가 없으면 `X-Forwarded-For`를 믿지 않음(`TRUST_PROXY` 미설정 시) |
| B-2 죽은 방 정리 | 충족(하트비트 20초 / 만료 60초) |
| 게임 진행 중 상태 | **미충족** — 서버는 여전히 로비/게임 중을 모름. 접속 후 게임 서버가 거부하고 안내 문구는 나옴 |
| `/lookup` 응답 형식 | 충족(`text/plain`, 따옴표·개행 없음, 클라이언트도 `Trim`) |
| 인원수 0 미만/정원 초과 보정, 동시 요청 | 충족(SQL 한 문장) |
| 403 기준, 없는 코드 처리 | 충족 |
| 인증 | 충족. 단 평문 HTTP라 같은 망에서 토큰을 엿볼 수 있음 |
| 삭제 메서드, 409, 입력 검증, 본문 크기 | 충족 |
| 요청 횟수 제한 | **미충족** — 방 대량 등록을 막지 못함(코드 추측은 36^10이라 비현실적) |
| HTTPS | **미충족** — 외부 공개 시 결정 필요 |
| 서버 재시작 | 충족(위 수정). DB 파일을 지우고 재시작하면 방이 사라지는 것은 그대로 |
| 처리되지 않은 예외 | **부분** — 요청 처리 중 예외는 500으로 끝나지만, DB 콜백 안에서 예외가 나면 프로세스가 죽음. 자동 재시작(pm2 등) 없음 |
| 로그의 민감 정보 | **미충족** — 방 코드와 호스트 주소를 기록(N-1 확인에 필요해 유지) |
| 짝이 되는 클라이언트 수정 | 방 상태 항목만 미적용, 나머지 적용 |
| Unity 쪽 남은 문제 | 등록 실패 처리·타임아웃·URL 이스케이프·분기 순서·`using` 적용. **남음:** `isConfigValid`가 `Awake`에서 한 번만 계산, 포트 3000 하드코딩, `CodeManager`의 딕셔너리/싱글톤(이제 쓰는 곳 없음) |
| 접속 구조 | 결정 대기(릴레이/전용 서버, 호스트 이전·재접속) |

**확인**
- `server.js`: 브라우저 엔진으로 구문 분석만 통과 확인(실행은 여전히 못 함 — Node.js 없음).
- Unity: MCP 재컴파일 후 콘솔에 C# 오류/경고 없음.

**범위 밖에서 본 것(미조치)**
- `NetworkCoordinator.EnsurePreviousSessionStopped`: 이전 세션이 남아 있을 때 `StopHost()`를 부르는데, 이것이 `OnStopHost → CleanupAndLoadTitle`로 이어져 방금 만든 방 코드/토큰을 지우고 타이틀을 다시 로드한 뒤에도 `StartHost()`를 계속 시도함. 정상 흐름에서는 이전 세션이 남지 않아 드물지만, 그 경로에서는 방 생성이 엉킴(기존 동작, 이번 수정과 무관).
