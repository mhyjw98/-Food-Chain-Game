# 먹이사슬(Food Chain) 프로젝트 리뷰 계획

## 목적
Mirror 기반 멀티플레이 게임 "먹이사슬"의 전체 구조를 점검하고 문제점을 찾아내기 위한 세션 분할 계획.
한 세션에서 전체를 다 보면 비효율적이므로, 아래 세션 단위로 나누어 **한 세션당 하나의 영역**만 구체적으로 점검한다.

> 세션 7~10 및 참고 자료는 `review-plan-2.md`에 이어서 정리되어 있음 (파일당 200줄 제한).

## 진행 방식
- 세션을 시작할 때 "N번 세션 진행해줘" 또는 세션 제목으로 요청하면, 해당 섹션의 대상 파일들만 집중적으로 읽고 리뷰한다.
- 리뷰 결과(발견한 문제, 개선 제안)는 각 세션 항목 하단에 정리해서 이 문서에 누적 기록한다.
- 상태는 `[ ]` 미시작 / `[~]` 진행중 / `[x]` 완료 로 표시한다.
- 전체 스크립트 규모 참고: `Assets/Script/` 하위 136개 .cs 파일, 약 1.1만 라인.

## 프로젝트 구조 요약 (가볍게 탐색한 결과)
- `Assets/Script/Network/` — Mirror 기반 네트워킹, 방 생성/입장, 코드 관리
- `Assets/Script/Player/` — 캐릭터 이동, 시야(POV 마스크), 사망/시체 처리
- `Assets/Script/Character/` — 포식자/피식자별 특수 능력(Wolf, Hawk, Crocodile, Badger, Scorpion 등)
- `Assets/Script/Mission/` — 미션 오브젝트 및 종류별(A/B) 구현체 다수 (Crops, Fish, Fruit, Grass, Insect, Meat, Seed, Wood 등)
- `Assets/Script/Game/` — 게임 매니저, 승리 조건, 스폰, 미니맵, 시야 시스템 총괄
- `Assets/Script/UI/` — 인게임/로비 UI, 채팅, 미션 리스트, 조사(수사) 리스트 등
- `Assets/Script/Setting/` — 설정 화면, 오디오, 키 설정, FOV 마스크 컨트롤러
- `Assets/Script/Voice/` — Vivox 기반 음성 채팅
- `Assets/Script/Data/` — 캐릭터/미션 데이터, 방 세션 데이터, 설정 매니저
- `Assets/Script/Object/` — 조사(수사) 오브젝트 및 트리거

---

## 세션 1 — 네트워크 & 방(Room) 구조 [x] (2026-09-17 완료, 상세 결과는 review-progress.md 참고)
Mirror 기반 멀티플레이의 뼈대. 연결/재연결, 방 생성·입장, 호스트 권한, 동기화 문제 위주로 점검.

**대상 파일**
- `Assets/Script/Network/NetworkCoordinator.cs`
- `Assets/Script/Network/RoomManager.cs`
- `Assets/Script/Network/RoomHost.cs`
- `Assets/Script/Network/RoomJoin.cs`
- `Assets/Script/Network/CodeManager.cs`
- `Assets/Script/Network/NetworkErrorManager.cs`
- `Assets/Script/Player/RoomPlayer.cs`
- `Assets/Script/Data/RoomSessionData.cs`

**점검 포인트**
- 서버 권위(server authority) 원칙이 지켜지는지 (클라이언트가 직접 상태를 바꾸는 곳은 없는지)
- SyncVar/Command/ClientRpc 사용이 올바른지, 치트에 취약한 구조는 없는지
- 재접속/퇴장 시 상태 정리(cleanup) 누락 여부
- 방 코드 생성/중복 처리 로직

---

## 세션 2 — 플레이어 이동 & 시야(POV) 시스템 [x] (2026-09-18 완료, 상세 결과는 review-progress.md 참고)
어몽어스류 게임의 핵심인 이동감과 시야 마스킹 구현을 점검.

**대상 파일**
- `Assets/Script/Player/PlayerMove.cs`
- `Assets/Script/Player/FollowCamera.cs`
- `Assets/Script/Player/VisionAnchor.cs`
- `Assets/Script/Player/LocalVisionLight.cs`
- `Assets/Script/Setting/FOVMaskController2D.cs`
- `Assets/Script/Setting/VisionUIController.cs`
- `Assets/Script/Game/VisionSystem.cs`

**점검 포인트**
- 이동 입력 처리와 네트워크 동기화(보간/예측) 방식
- 2D Light + POV 마스크 구현이 퍼포먼스에 미치는 영향
- 로컬 플레이어 전용 시야가 다른 클라이언트에 누출되지 않는지(치팅 방지)

---

## 세션 3 — 상호작용 & 조사(수사) 오브젝트 [ ]
피식자가 미션을 시작/클리어하는 상호작용과 조사 시스템.

**대상 파일**
- `Assets/Script/Player/Scanner.cs`
- `Assets/Script/Object/InvestigationObject.cs`
- `Assets/Script/Object/InvestigationTrigger.cs`
- `Assets/Script/Mission/MissionTrigger.cs`
- `Assets/Script/Mission/MissionObject.cs`
- `Assets/Script/Data/InvestigationLog.cs`
- `Assets/Script/Data/InvestigationRecord.cs`

**점검 포인트**
- 키 통일(공격/탐색) 이후 상호작용 입력 충돌 여부 (최근 커밋과 연관)
- 상호작용 판정 범위/레이어 설정의 정확성
- 조사 기록 데이터가 서버/클라 간 일관되게 동기화되는지

---

## 세션 4 — 포식자/피식자 능력 시스템 [ ]
캐릭터별 특수 능력과 공격 로직.

**대상 파일**
- `Assets/Script/Character/WolfAbility.cs`
- `Assets/Script/Character/HawkAbility.cs`
- `Assets/Script/Character/CrocodileAbility.cs`
- `Assets/Script/Character/BadgerAbility.cs`
- `Assets/Script/Character/ScorpionAbility.cs`
- `Assets/Script/Character/HelperAbility.cs`
- `Assets/Script/Character/PreySymbiosisAbility.cs`
- `Assets/Script/Data/PlayerAbility.cs`
- `Assets/Script/Data/CharacterData.cs`
- `Assets/Script/Data/CharacterZoneData.cs`
- `Assets/Script/Player/PlayerDeathHandler.cs`
- `Assets/Script/Player/Corpse.cs`

**점검 포인트**
- 공격 판정과 쿨다운/범위 처리의 서버 권위 여부
- 능력별 밸런스 관련 하드코딩 값이 데이터로 분리되어 있는지
- 사망 처리와 시체 오브젝트 생성/정리 흐름

---

## 세션 5 — 미션 시스템 (공통 구조) [ ]
`BaseMission`, `MissionRegistry`, `MissionSelector` 등 미션 프레임워크의 설계.

**대상 파일**
- `Assets/Script/Mission/BaseMission.cs`
- `Assets/Script/Mission/BagBase.cs`
- `Assets/Script/Mission/IBagItem.cs`
- `Assets/Script/Mission/DragItem.cs`
- `Assets/Script/Mission/DragThrowItem.cs`
- `Assets/Script/Mission/MissionRegistry.cs`
- `Assets/Script/Mission/MissionSelector.cs`
- `Assets/Script/Mission/TreeObject.cs`
- `Assets/Script/Data/MissionData.cs`

**점검 포인트**
- 미션 무작위 설정 로직(최근 커밋 "미션 무작위 설정 로직 구현")이 공정하게/중복 없이 동작하는지
- 미션 프레임워크의 확장성 (새 미션 추가 시 규칙이 명확한지)
- 드래그/투척 등 공통 상호작용 컴포넌트의 재사용성과 중복 코드

---

## 세션 6 — 미션 시스템 (개별 미션 구현체) [ ]
A/B 두 종류로 구현된 개별 미션들. 세션 5에서 프레임워크를 본 뒤 진행.

**대상 파일 (예시로 그룹핑, 전부 순회)**
- Crops: `CropsA/*`, `CropsB/*`
- Fish: `FishA/*`, `FishB/*`
- Fruit: `FruitA/*`, `FruitB/*`
- Grass: `GrassA/*`, `GrassB/*`
- Insect: `InsectA/*`, `InsectB/*`
- Meat: `MeatA/*`, `MeatB/*`
- Seed: `SeedA/*`, `SeedB/*`
- Wood: `WoodA/*`, `WoodB/*`
- 기타: `Mission/Badger/BadgerA.cs`, `Mission/Poision/PoisonA.cs`

**점검 포인트**
- A/B 버전 간 구조 중복이 과도하지 않은지 (공통화 가능 여부)
- 각 미션의 완료/실패 조건과 네트워크 동기화
- 유사 미션 간 일관성 없는 구현 패턴
