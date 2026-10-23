# 리팩토링 계획 (미착수)

코드 리뷰 세션 1~10은 2026-10-09에 모두 끝났다(기록: `review-progress.md`, `review-progress-2.md`).
아래는 리뷰 중 발견했지만 범위가 커서 미뤄 둔 구조 개선이다. **플레이 테스트(`review-tests*.md`)를 먼저 통과시킨 뒤** 진행한다.
프리팹 필드명이 바뀌는 작업이 많으므로 Unity MCP로 값 유실을 바로 확인하면서 진행한다.

### R-1. 미션 공통 뼈대를 베이스 클래스로 (세션 6) — 우선순위 높음
- 대상: 미션 본체 18개 (`Assets/Script/Mission/*/`)
- 내용: 타이머 `Update`, 종료 플래그, "성 공/실 패" 표시 후 대기하는 완료/실패 코루틴(미션당 약 40줄)을 `BaseMission`(또는 `TimedMission`)으로 이동. `timeLimit`, 결과 표시 시간, 타이머/상태 텍스트를 베이스의 `[SerializeField]`로 통일
- 효과: 종료 플래그 누락류 버그가 한 곳에서 해결됨. 플래그 이름(`isComplete`/`_isComplete`/`_isFinished`/`_done`) 통일
- 주의: 프리팹의 직렬화 필드명이 바뀌면 값이 날아가므로 `[FormerlySerializedAs]` 사용

### R-2. 통째로 같은 미션 파일 통합 (세션 6)
- `FruitA` ≡ `WoodB`, `FruitItemA` ≡ `WoodItemB` → "나무 흔들어 담기" 미션 하나 + 설정값
- 가방 4종(`FruitBag`/`GrassBag`/`SeedBag`/`WoodBag`) → 제네릭 가방 하나. `BagBase`의 죽은 수락 로직 정리
- `GrassA`/`CropsA`의 스폰·가방 판정 공통화
- `BadgerA` ≡ `PoisonA`는 임시 구현이므로 통합보다 실제 미니게임 구현이 먼저

### R-3. 미션 정의 단일화 (세션 5)
- 새 미션 추가 시 6군데(`MissionType`, `MissionMeta`, `MissionData`, `MissionSelector` 풀, `MissionUIManager` 프리팹 목록, 씬 `MissionObject`)를 고쳐야 함 → ScriptableObject 하나로 모으고 시작 시 누락 검사

### R-4. 캐릭터 데이터 단일화 (세션 4)
- `GamePlayer.SetAnimalType`의 switch, `CharacterConfig`, `CharacterZoneData`, `MaxHungryRounds`, `PredatorPriority` 등 여러 곳 → 캐릭터 테이블 하나
- 거주지가 `TerritoryType`(CharacterData) / `ZoneType`(CharacterZoneData) 두 곳에 따로 적혀 있음 → 하나로 합치기
- 능력 컴포넌트 정리(늑대/매/악어/조력자는 빈 구현)

### R-6. 상호작용 오브젝트 공통화 (세션 3)
- `MissionObject`/`InvestigationObject`/`DisguiseObject`/`PredictObject`, `MissionTrigger`/`InvestigationTrigger` 복붙 → 공통 베이스
- 함께 결정: `InvestigationObject`의 자식 트리거가 `Awake`에서 꺼진 뒤 다시 켜지지 않아 하이라이트/클릭 경로가 죽어 있음(상호작용 키 경로는 동작)

### 이름 오타 정리 (세션 10, 동작 영향 없음)
- `GameMamager`, `NickNamemanager`, `WinCondutionFactory`, `hasEate`, 폴더 `Utill` — 다른 변경과 섞지 말고 이름 변경만 따로 한 커밋으로
