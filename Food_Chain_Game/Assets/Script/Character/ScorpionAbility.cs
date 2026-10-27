using Mirror;
using UnityEngine;

public class ScorpionAbility : NetworkBehaviour, IAnimalAbility, IPlayerAbility, IMissionCompleteHandler
{
    // 독 보유 여부는 서버와 본인만 안다(이 컴포넌트는 Sync Mode = Owner)
    [SyncVar] private bool hasPoison;

    // 독 반격에 한 번이라도 성공했는지(승리 조건). 서버 전용이라 다른 클라이언트에 드러나지 않는다
    private bool _hasCountered;

    protected GamePlayer local;
    public bool NeedsPoisonMission => enabled && !hasPoison;
    public bool HasCountered => enabled && _hasCountered;

    private void Awake()
    {
        // 프리팹 설정이 바뀌더라도 전원 동기화로 되돌아가지 않도록 코드에서도 고정한다
        syncMode = SyncMode.Owner;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        local = GetComponentInParent<GamePlayer>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (local == null) local = GetComponentInParent<GamePlayer>();
    }
    [Server]
    public void ServerActivate(GamePlayer local, AnimalType type)
    {
        enabled = (type == AnimalType.Scorpion);
        if (!enabled) return;

        Debug.Log("[ScorpionAbility] ServerActivate 호출" + enabled);

        hasPoison = false;
        _hasCountered = false;

        ServerSyncPoisonMissionSlot();
    }

    [Server]
    public void OnNewDay(int day)
    {
        if (!enabled) return;
        if (!hasPoison) return;

        hasPoison = false;
        ServerSyncPoisonMissionSlot();     
    }
    [Server]
    public void OnBeforeAttack(ref AttackContext ctx)
    {
        if (!enabled) return;
        if (local == null) local = GetComponent<GamePlayer>();
        if (local == null) return;
        if (ctx.target != local) return;
        if (ctx.attacker == null) return;

        Debug.Log("독 상태 :" + hasPoison);

        // 독이 없으면 기본 규칙대로 피식자 판정(전갈 사망, 공격자는 포식 성공)
        if (!hasPoison) return;

        // 독이 있으면 전갈은 살고 공격자만 죽는다
        ctx.killTarget = false;
        ctx.killAttacker = true;
        ctx.reason = "ScorpionPoisonCounter";

        // 반격하면 GamePlayer.CmdAttack이 공격자를 바로 사망 처리한다(killAttacker)
        _hasCountered = true;
        hasPoison = false;
        ServerSyncPoisonMissionSlot();
    }

    [Server]
    public void OnMissionCompletedServer(GamePlayer local, MissionType type)
    {
        if (!enabled) return;
        if (type != MissionType.Scorpion_Poison) return;
        if (this.local == null || local != this.local) return;

        hasPoison = true;
        ServerSyncPoisonMissionSlot();
    }   

    [Server]
    private void ServerSyncPoisonMissionSlot()
    {
        if (!enabled) return;
        if (local == null) return;

        int existing = FindPoisonSlotIndex(local.Missions);
        if (existing >= 0)
            local.Missions.RemoveAt(existing);

        if (!hasPoison)
        {
            Debug.Log("ServerSyncPoisonMissionSlot 독 수집 미션 생성");
            local.Missions.Insert(0, new MissionSlot
            {
                Type = MissionType.Scorpion_Poison,
                Status = MissionStatus.NotStarted
            });
        }
    }

    private static int FindPoisonSlotIndex(SyncList<MissionSlot> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Type == MissionType.Scorpion_Poison)
                return i;
        }
        return -1;
    }
}
