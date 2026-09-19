using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HawkAbility : NetworkBehaviour, IAnimalAbility, IPlayerAbility
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

        enabled = (type == AnimalType.Hawk);
        Debug.Log("[HawkAbility] ServerActivate 호출" + enabled);
    }
    [Server]
    public void OnNewDay(int day)
    {
    }
    [Server]
    public void OnBeforeAttack(ref AttackContext ctx)
    {
        if (!enabled) return;
        if (local == null) GetComponent<GamePlayer>();
        if (local == null) return;
        if (ctx.attacker == null) return;
        if (ctx.attacker != local) return;

        bool canAttack = PredatorPriority.CanAttack(ctx.attacker.predatorType, ctx.target.predatorType);

        ctx.killAttacker = !canAttack;
        ctx.killTarget = canAttack;
    }
}
