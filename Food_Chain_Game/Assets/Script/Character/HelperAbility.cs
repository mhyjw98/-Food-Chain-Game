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

        enabled = (type == AnimalType.Crow || type == AnimalType.Plover || type == AnimalType.Meerkat);
        Debug.Log("[HelperAbilty] ServerActivate 호출" + enabled);
    }
    [Server]
    public void OnNewDay(int day)
    {
    }
    [Server]
    public void OnBeforeAttack(ref AttackContext ctx)
    {
        // 피식자는 GamePlayer.CmdAttack의 기본 규칙으로 사망 처리된다. 전용 예외 없음
    }
}
