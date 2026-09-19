using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WolfAbility : NetworkBehaviour, IAnimalAbility, IPlayerAbility
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
        enabled = (type == AnimalType.Wolf);
        Debug.Log("[WolfAbility] ServerActivate 호출" + enabled);
    }
    [Server]
    public void OnNewDay(int day)
    {
    }
    [Server]
    public void OnBeforeAttack(ref AttackContext ctx)
    {
        if (!enabled) return;
        if (local == null) local = GetComponent<GamePlayer>();
        if (local == null) return;
        if (ctx.attacker == null) return;
        if (ctx.target == null) return;
        if (ctx.attacker != local) return;

        ctx.killTarget = true;        
    }
}
