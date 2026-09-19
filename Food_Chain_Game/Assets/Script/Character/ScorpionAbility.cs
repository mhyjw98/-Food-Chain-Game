using Mirror;
using UnityEngine;

public class ScorpionAbility : NetworkBehaviour, IAnimalAbility, IPlayerAbility, IMissionCompleteHandler
{
    [SyncVar] private bool hasPoison;

    protected GamePlayer local;
    public bool NeedsPoisonMission => enabled && !hasPoison;

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
        if (local == null) GetComponent<GamePlayer>();
        if (local == null) return;
        if (ctx.target != local) return;
        if (ctx.attacker == null) return;

        Debug.Log("독 상태 :" + hasPoison);
        if (hasPoison)
        {
            ctx.killAttacker = true;
            ctx.reason = "ScorpionPoisonCounter";

            hasPoison = false;
            ServerSyncPoisonMissionSlot();
        }
        else
            ctx.killTarget = true;
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
