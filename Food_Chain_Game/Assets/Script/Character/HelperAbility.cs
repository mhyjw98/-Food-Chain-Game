using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelperAbility : NetworkBehaviour, IAnimalAbility, IPlayerAbility
{
    protected GamePlayer local;
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
    public void ServerActivate(GamePlayer local, AnimalType type)
    {
        this.local = local;

        enabled = (type == AnimalType.Crow || type == AnimalType.Plover || type == AnimalType.Squirrel);
        Debug.Log("[HelperAbilty] ServerActivate 호출" + enabled);
    }
    [Server]
    public void OnNewDay(int day)
    {
    }
    [Server]
    public void OnBeforeAttack(ref AttackContext ctx)
    {
        if (!enabled) return;
        if (ctx.attacker == null) return;
        if (ctx.target != local) return;

        ctx.killTarget = true;
    }
}
