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
        // 공격 성공 여부는 GamePlayer.CmdAttack의 기본 규칙(서열)으로 정해진다. 늑대 전용 예외 없음
    }
}
