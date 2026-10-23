using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWinCondition
{
    bool Evaluate(GamePlayer player, SyncList<GamePlayer> allPlayers);
}

// 다른 플레이어의 승패를 그대로 따르는 조건(예: 올빼미의 승리 예측).
// 나머지 플레이어의 결과가 정해진 뒤 GameResultEvaluator가 GetDependency로 대상을 찾아 그 결과를 쓴다
public interface IDependentWinCondition : IWinCondition
{
    // 결과를 따를 플레이어. 없으면 null(패배)
    GamePlayer GetDependency(GamePlayer player, SyncList<GamePlayer> allPlayers);
}
