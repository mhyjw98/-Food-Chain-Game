# 변경 이력 (Changelog)

코드 리뷰(review-plan.md/review-progress.md) 진행 중 실제로 적용한 코드 수정 기록. 최신순.

## 2026-09-18

### 수정
- **로비 복귀 시 RoomPlayer가 안 보이던 문제.** 클라이언트 훅(`OnClientEnterRoom`) 대신 서버가 확실히 감지하는 시점(`RoomManager.OnRoomServerSceneChanged`)에서 `RoomPlayer.RpcSetRoomVisualsActive` RPC로 표시/숨김·위치 재배치를 직접 지시하도록 변경. (`RoomPlayer.cs`, `RoomManager.cs`)
- **호스트가 끊긴 클라이언트를 감지 못하던 문제.** `disconnectInactiveConnections` 활성화, 타임아웃 60→15초로 단축. (`RoomManager.prefab`)
- **네트워크 지연 후 방 생성 시 `StartHost()`가 소켓 예외로 실패.** 실패 시 0.5초 간격 최대 5회 재시도 로직 추가. (`NetworkCoordinator.cs`)
- **`config.json` 위치 문제로 Node 서버 통신 전체가 실패.** `Resource/config.json`(Assets 밖, 클론/빌드에 자동 포함 안 됨)을 `Assets/StreamingAssets/config.json`으로 이전. 설정 누락 시 조용히 깨진 기본값을 쓰던 것을 명확한 에러 로그로 변경. (`ConfigManager.cs`, `RoomHost.cs`, `RoomJoin.cs`, `Assets/StreamingAssets/config.json` 신규)
- **TMP 텍스트에 `�` 대체 문자 경고 다량 발생.** `Assets/Script` 하위 62개 파일이 CP949로 저장되어 있던 것을 UTF-8(BOM)로 일괄 변환.
- **로비 → 게임 전환 시 로비 캐릭터가 게임 화면에 남아있던 문제.** `RoomPlayer.SetRoomVisualsActive()`로 시각 요소(스프라이트/닉네임/말풍선)만 껐다 켜도록 변경(오브젝트 자체는 유지). (`RoomPlayer.cs`)
- **`OnRoomServerCreateGamePlayer`에서 `SpawnManager.Instance` null 체크 누락.** 방어 코드 추가. (`RoomManager.cs`)

### 추가
- **생존자 0명일 때도 게임 종료.** 기존엔 "포식자 전멸"만 체크했음. `CheckGameOver(bool wasPredator)`로 바꿔 모든 죽음마다 체크하고, 포식자 전멸 또는 생존자 0명 중 하나면 종료. (`GameMamager.cs`, `GamePlayer.cs`)

## 2026-09-17

### 수정
- **색상 잠금이 퇴장 시 안 풀리던 문제.** `RpcColorReleased` RPC로 명시적으로 색상 해제를 클라이언트에 전파하도록 변경(기존엔 오브젝트 파괴 직전 SyncVar 훅에만 의존해 유실 가능). (`RoomPlayer.cs`, `RoomManager.cs`, `PlayerColorPalette.cs`)
- **한 명만 나가도 방 코드가 초기화되던 문제.** `RoomSessionData.Reset()`을 방에 아무도 안 남았을 때만 호출하도록 변경, 같은 조건에서 `roomHost.DeleteRoom()` 호출 추가. (`RoomManager.cs`)
- **캐릭터 배정 무한루프 가능성.** do-while 진입 전 풀 소진 여부 체크, 소진 시 경고 로그+중복 배정으로 안전하게 처리. (`RoomManager.cs`)
- **`_isCleaningUp` 플래그가 재설정 안 되던 문제.** `OnStartServer`/`OnStartClient`에서 재무장하도록 변경. (`RoomManager.cs`)
- **로비 복귀 자체가 안 되던 근본 문제.** `GameMamager.DeleteRoomPlayer()`가 게임 시작 직후 RoomPlayer를 파괴해버려 로비 복귀 시 되살릴 오브젝트가 없었음 — 해당 호출과 메서드 제거. (`GameMamager.cs`)
- **동물(직업) 재배정이 2번째 판부터 안 되던 문제.** 캐릭터 배정 시점을 "방 인원이 다 찼을 때"에서 "게임 시작 시점"으로 이동 — 매 판마다 새로 무작위 배정되도록 변경. 색상/닉네임은 영향 없음. (`RoomManager.cs`)
- **null 참조 위험 경고 4건.** `CmdSetColor` 범위 체크, `CmdSetData`/`CmdSetUserId`의 null 체크, `OnServerAddPlayer`의 `SpawnManager.Instance` null 체크 추가. (`RoomPlayer.cs`, `RoomManager.cs`)

### 제거
- **`PreviousHostId` 죽은 코드.** "방장이 나가면 다른 사람이 방장이 되는" 기능을 만들다 만 미완성 필드 — 실제 호스트 마이그레이션은 Mirror 리슨 서버 구조상 별도 대규모 작업이 필요해 이번엔 구현하지 않고 죽은 코드만 정리. (`RoomSessionData.cs`, `RoomManager.cs`)

---

## 리뷰만 진행하고 아직 수정 안 한 항목

세션 2(플레이어 이동 & 시야 시스템, 2026-09-18)에서 발견했으나 아직 코드 수정은 하지 않음. 상세는 `review-progress.md` 참고.
- 이동이 완전히 클라이언트 권위 (서버 위치 검증 없음 — 치트 취약)
- `LocalVisionLight.TransitionToVision`의 null 체크 누락
- 시야 반경 값(11/6/8) 하드코딩 중복 (`GameMamager.cs` / `VisionUIController.cs`)
- `VisionSystem.cs` 죽은 코드(어떤 씬/프리팹에도 연결 안 됨)
