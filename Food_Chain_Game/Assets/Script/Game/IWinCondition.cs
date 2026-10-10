using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWinCondition
{
    bool Evaluate(GamePlayer player, SyncList<GamePlayer> allPlayers);
}
