using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HippoAbility : NetworkBehaviour, IAnimalAbility, IMissionCompleteHandler
{
    // 서버 전용(SyncVar 아님). 동기화하면 하마가 죽는 순간 살해자가 모든 클라이언트에 노출된다
    private uint revengeTargetNetId;
    private bool revengeActive;

    private GamePlayer local;

    private static readonly MissionType[] RevengeMissions =
    {
        MissionType.RevengeA,
        //MissionType.RevengeB,
        //MissionType.RevengeC,
        //MissionType.RevengeD,
        //MissionType.RevengeE,
    };

    [Server]
    public void ServerActivate(GamePlayer local, AnimalType type)
    {
        this.local = local;
        enabled = (type == AnimalType.Hippo);
        revengeTargetNetId = 0;
        revengeActive = false;
    }

    [Server]
    public void ServerOnlocalDied(uint killerNetId)
    {
        if (!enabled) return;
        if (local == null) return;

        revengeTargetNetId = killerNetId;
        revengeActive = (killerNetId != 0);

        local.Missions.Clear();
        for (int i = 0; i < RevengeMissions.Length; i++)
        {
            local.Missions.Add(new MissionSlot
            {
                Type = RevengeMissions[i],
                Status = MissionStatus.NotStarted
            });
        }
    }

    [Server]
    public void OnMissionCompletedServer(GamePlayer localPlayer, MissionType type)
    {
        if (!enabled) return;
        if (!revengeActive) return;
        if (localPlayer != local) return;

        if (!IsRevengeMission(type)) return;

        if (revengeTargetNetId == 0) return;

        if (!NetworkServer.spawned.TryGetValue(revengeTargetNetId, out var id) || id == null)
            return;

        var target = id.GetComponent<GamePlayer>();
        if (target == null || !target.isAlive) return;

        ApplyPenalty(type, target);
    }

    private bool IsRevengeMission(MissionType type)
    {
        for (int i = 0; i < RevengeMissions.Length; i++)
            if (RevengeMissions[i] == type) return true;
        return false;
    }

    [Server]
    private void ApplyPenalty(MissionType completedType, GamePlayer attacker)
    {
        switch (completedType)
        {
            case MissionType.RevengeA:
                Debug.Log("RevengeA 미션 완료");
                break;

            case MissionType.RevengeB:
                break;

            case MissionType.RevengeC:
                break;

            case MissionType.RevengeD:
                break;

            case MissionType.RevengeE:
                break;
        }
    }
}
