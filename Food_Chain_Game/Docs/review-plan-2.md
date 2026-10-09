# 먹이사슬 프로젝트 리뷰 계획 (이어서)

> `review-plan.md`(세션 1~6)에서 이어지는 문서. 진행 방식/체크 표기는 그 문서와 동일.

---

## 세션 7 — 게임 흐름 & 매니저 [x] (2026-10-09 완료, 치명적 1~4번 수정 — 상세 결과는 review-progress-2.md 참고)
게임 시작부터 종료까지의 전체 흐름을 담당하는 매니저 레이어.

**대상 파일**
- `Assets/Script/Game/GameMamager.cs`
- `Assets/Script/Game/SpawnManager.cs`
- `Assets/Script/Game/IWinCondition.cs`
- `Assets/Script/Game/AnimalWinCondition.cs`
- `Assets/Script/Game/WinCondutionFactory.cs`
- `Assets/Script/Game/Map/MapData.cs`
- `Assets/Script/Game/Map/MiniMapController.cs`
- `Assets/Script/Game/Map/MinimapProfile.cs`
- `Assets/Script/Data/ConfigManager.cs`

**점검 포인트**
- 게임 상태(대기/진행/종료) 관리가 한 곳에 응집되어 있는지, 스파게티 참조는 없는지
- 승리 조건 판정 타이밍과 동기화
- 스폰 로직의 공정성(위치 중복, 밸런스)

> 참고: `GameMamager.cs`/`ConfigManager.cs`는 세션 1~2 진행 중 일부 이미 수정됨(review-progress.md 참고). 이 세션에서는 나머지 부분(승리 조건, 맵/미니맵)을 중심으로 점검.

---

## 세션 8 — UI 시스템 [x] (2026-10-09 완료 — 8-A, 8-B 모두. review-progress-2.md 참고)
인게임/로비 UI 전반. 대상이 24개라 둘로 나눠 진행.
- **8-A 인게임 UI [x]** (2026-10-09 완료 — review-progress-2.md 참고): `GamePlayUI`, `MissionUIManager`, `MissionListUI`, `MissionListItemUI`, `InvestigationListUI`, `InvestigationListItem`, `InvestigationResultPresenter`, `ContextMenuUI`, `PartnerAttackAlertUI`
- **8-B 로비·공용 UI [x]**: `GameRoomUI`, `TitleUI`, `ChatManager`, `ChatInputFocus`, `PlayerSlot`, `PlayerSlotUI`, `PlayerColorPalette`, `CharacterSetting`, `UIManager`, `UIDraggable`, `UIResizable`, `SortingSprite`, `SpriteSorter`, `SpeechBubble`, `ReturnToTitleConfirmUI`(신규)

**대상 파일**
- `Assets/Script/UI/GamePlayUI.cs`
- `Assets/Script/UI/GameRoomUI.cs`
- `Assets/Script/UI/MissionUIManager.cs`
- `Assets/Script/UI/MissionListUI.cs`
- `Assets/Script/UI/MissionListItemUI.cs`
- `Assets/Script/UI/InvestigationListUI.cs`
- `Assets/Script/UI/InvestigationListItem.cs`
- `Assets/Script/UI/InvestigationResultPresenter.cs`
- `Assets/Script/UI/ChatManager.cs`
- `Assets/Script/UI/ChatInputFocus.cs`
- `Assets/Script/UI/ContextMenuUI.cs`
- `Assets/Script/UI/PartnerAttackAlertUI.cs`
- `Assets/Script/UI/PlayerSlot.cs`, `PlayerSlotUI.cs`, `PlayerColorPalette.cs`
- `Assets/Script/UI/TitleUI.cs`
- `Assets/Script/UI/UIManager.cs`, `UIDraggable.cs`, `UIResizable.cs`
- `Assets/Script/UI/SortingSprite.cs`, `SpriteSorter.cs`, `SpeechBubble.cs`

**점검 포인트**
- UI 업데이트가 이벤트 기반인지 폴링(Update) 기반인지, 최근 "색상 선택/퇴장 시 UI 업데이트" 버그와 유사한 패턴이 더 있는지
- UI-네트워크 계층 간 의존성이 과도하지 않은지

> 참고: `PlayerColorPalette.cs`는 세션 1 진행 중 일부 이미 수정됨(review-progress.md 참고).

---

## 세션 9 — 설정 & 음성 채팅 [x] (2026-10-09 설정 부분 완료. **음성(Vivox)은 구현 중이라 사용자 결정으로 제외** — `Voice/*`, `VivoxSmokeTest`는 미리뷰)
설정 화면과 Vivox 기반 음성 시스템.

**대상 파일**
- `Assets/Script/Setting/SettingManager.cs`
- `Assets/Script/Setting/SettingsTabController.cs`
- `Assets/Script/Setting/KeySetting.cs`
- `Assets/Script/Setting/SoundSetting.cs`
- `Assets/Script/Setting/ResolutionSetting.cs`
- `Assets/Script/Setting/AudioManager.cs`
- `Assets/Script/Setting/VivoxSmokeTest.cs`
- `Assets/Script/Voice/VoiceManager.cs`
- `Assets/Script/Voice/VoiceSetting.cs`
- `Assets/Script/Voice/VoiceUI.cs`
- `Assets/Script/Voice/InGameVoiceProximity.cs`
- `Assets/Script/Voice/LobbyVoiceParticipantRow.cs`

**점검 포인트**
- 키 바인딩 저장/로드 및 충돌 처리(공격/탐색 키 통일 이후 잔여 이슈 확인)
- 음성 근접(proximity) 로직과 시야 시스템의 연동 여부
- 설정 값 영속성(PlayerPrefs 등) 관리

---

## 세션 10 — 데이터 계층 & 기타 유틸 [x]
전역 데이터 클래스와 유틸리티, 마무리 점검.

**대상 파일**
- `Assets/Script/Data/NickNamemanager.cs`
- `Assets/Script/Data/UserIdManager.cs`
- `Assets/Script/Utill/*` (전체)
- 프로젝트 설정 관련 변경 파일 검토 (`ProjectSettings/*`, `Packages/manifest.json` 등 git status 상 변경분)

**점검 포인트**
- 전역 싱글톤/매니저의 생명주기 관리
- 네이밍 오타(`GameMamager`, `NickNamemanager` 등) 등 사소하지만 누적된 이슈 정리
- 현재 git status에 걸려있는 미커밋 변경사항 정리 필요 여부

---

## 리뷰 이후 리팩토링 계획 (미착수)

리뷰 중 발견했지만 범위가 커서 미뤄둔 구조 개선. **리뷰 수정분의 플레이 테스트(review-tests*.md)를 먼저 통과시킨 뒤** 별도 작업으로 진행한다. 상세 근거는 각 세션 기록 참고.

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
- `GamePlayer.SetAnimalType`의 130줄 switch, `CharacterConfig`, `CharacterZoneData`, `MaxHungryRounds`, `PredatorPriority` 5곳 → 캐릭터 테이블 하나
- 능력 컴포넌트 7개(늑대/매/악어/조력자는 현재 빈 구현) 정리, 여우/스컹크 위장 플래그 정리

### R-5. 역할 관련 잔여 동기화를 본인 전용으로 (세션 3~4) — ✅ 2026-10-09 완료 (review-progress-2.md 참고)
- `hasEate`/`hasAttacked`/`hungryStreak`/`scanCount`/`Missions`/능력 상태 등은 아직 전원에게 동기화됨 → 오너 전용 컴포넌트(`syncMode = Owner`)로 분리. 프리팹 수정이 필요해 MCP 연결 세션에서 진행

### R-6. 상호작용 오브젝트 공통화 (세션 3)
- `MissionObject`/`InvestigationObject`, `MissionTrigger`/`InvestigationTrigger` 복붙 → 공통 베이스. 스캔 전용 오브젝트 추가 예정이므로 그 작업과 함께 진행

---

## 참고: 최근 커밋 이력 (문맥)
- `7ac5a86` save: 저장소 변경
- `69250b9` Re: 공격, 탐색키 상호작용 키로 통일
- `041da96` Feat: 미션 무작위 설정 로직 구현
- `baa439f` Fix: 색상 선택 or 퇴장시 UI 업데이트 되도록 수정
- `d6d5b95` Feat: 공격, 탐색 키 통일

## 리뷰 기록
> 세션별 상세 발견 사항은 `review-rules.md` 기록 형식에 따라 `Docs/review-progress.md`에 누적 기록한다. 이 문서에는 세션 진행 상태(체크박스)만 표시.
