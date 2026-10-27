using Mirror;
using UnityEngine;

// 서버와 본인(오너)만 아는 플레이어 상태.
// 이 컴포넌트는 Sync Mode가 Owner라서 여기 있는 값은 다른 클라이언트에 전송되지 않는다.
// (전원에게 보내면 굶주림/공격 여부/미션 목록 등으로 다른 플레이어의 역할을 추정할 수 있다)
// 다른 플레이어의 값은 클라이언트에서 항상 기본값이므로, 판정은 반드시 서버에서 한다.
// 접근은 GamePlayer의 같은 이름 프로퍼티를 통해 한다.
public class PlayerPrivateState : NetworkBehaviour
{
    [SyncVar] public int hungryStreak = 0;
    [SyncVar] public bool hasEate = false;
    [SyncVar] public bool hasAttacked = false;
    [SyncVar] public int scanCount = 0;
    [SyncVar] public AnimalType predictedWinner = AnimalType.None;
    // 승리자로 예측한 플레이어(0이면 아직 예측하지 않음). 예측은 한 번만 할 수 있다
    [SyncVar] public uint predictedPlayerNetId;
    [SyncVar] public AnimalType disguisedAs = AnimalType.None;
    [SyncVar] public AnimalType predictedAs = AnimalType.None;
    [SyncVar] public bool predictedCorrectly;
    // 지금 있는 구역. 위치가 드러나므로 본인에게만 보낸다(구역 아이콘은 내 것만 표시)
    [SyncVar(hook = nameof(OnZoneChanged))] public ZoneType currentZone;

    public readonly SyncList<MissionSlot> Missions = new SyncList<MissionSlot>();

    private void Awake()
    {
        // 프리팹 설정이 바뀌더라도 전원 동기화로 되돌아가지 않도록 코드에서도 고정한다
        syncMode = SyncMode.Owner;
    }

    private void OnZoneChanged(ZoneType oldVal, ZoneType newVal)
    {
        var gamePlayer = GetComponent<GamePlayer>();
        if (gamePlayer != null) gamePlayer.HandleZoneChanged(newVal);
    }
}
