using Mirror;
using System.Collections.Generic;

// 게임 종료 시 전원의 승패를 계산한다(서버 전용).
// 다른 플레이어의 승패를 따르는 조건(IDependentWinCondition, 예: 올빼미)은 나머지 결과가 정해진 뒤에 계산한다
public static class GameResultEvaluator
{
    public static Dictionary<GamePlayer, bool> EvaluateAll(SyncList<GamePlayer> players)
    {
        // 조건 코드가 null을 신경 쓰지 않아도 되도록 빈 칸을 뺀 목록으로 계산한다
        var valid = new SyncList<GamePlayer>();
        foreach (var p in players)
        {
            if (p != null) valid.Add(p);
        }

        var results = new Dictionary<GamePlayer, bool>();

        foreach (var p in valid)
        {
            IWinCondition condition = WinCondutionFactory.GetCondition(p.animalType);
            if (condition is IDependentWinCondition) continue;

            // 승리 조건이 정의되지 않은 캐릭터는 패배 처리
            results[p] = condition != null && condition.Evaluate(p, valid);
        }

        var resolving = new HashSet<GamePlayer>();
        foreach (var p in valid)
        {
            if (!results.ContainsKey(p))
                Resolve(p, valid, results, resolving);
        }

        return results;
    }

    private static bool Resolve(GamePlayer player, SyncList<GamePlayer> all,
        Dictionary<GamePlayer, bool> results, HashSet<GamePlayer> resolving)
    {
        if (results.TryGetValue(player, out bool known)) return known;

        // 올빼미 둘이 서로를 예측한 경우처럼 순환하면 정할 수 없으므로 패배
        if (!resolving.Add(player)) return false;

        bool win = false;
        if (WinCondutionFactory.GetCondition(player.animalType) is IDependentWinCondition dependent)
        {
            GamePlayer target = dependent.GetDependency(player, all);
            win = target != null && Resolve(target, all, results, resolving);
        }

        results[player] = win;
        return win;
    }
}
