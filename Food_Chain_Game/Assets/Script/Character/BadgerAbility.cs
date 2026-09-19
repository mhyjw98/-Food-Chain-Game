using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BadgerAbility : NetworkBehaviour, IAnimalAbility, IMissionCompleteHandler
{
    [SyncVar] private uint revengeTargetNetId;
    [SyncVar] private bool revengeActive;

    private GamePlayer local;

    private static readonly MissionType[] RevengeMissions =
    {
        MissionType.BadgerA,
        //MissionType.BadgerB,
        //MissionType.BadgerC,
        //MissionType.BadgerD,
        //MissionType.BadgerE,
    };

    [Server]
    public void ServerActivate(GamePlayer local, AnimalType type)
    {
        this.local = local;
        enabled = (type == AnimalType.Badger);
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
            case MissionType.BadgerA:
                Debug.Log("BadgerA 미션 완료");
                break;

            case MissionType.BadgerB:
                break;

            case MissionType.BadgerC:
                break;

            case MissionType.BadgerD:
                break;

            case MissionType.BadgerE:
                break;
        }
    }
}
