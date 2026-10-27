using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static CharacterData;

public class AnimalWinCondition : MonoBehaviour
{
    public class WolfWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, SyncList<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
    public class CrocodileWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, SyncList<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
    public class HawkWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, SyncList<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
    public class HyenaWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, SyncList<GamePlayer> allPlayers)
        {
            foreach (var p in allPlayers)
            {
                if (p.animalType == AnimalType.Wolf)
                {
                    return !p.isAlive; // 늑대가 죽으면 승리
                }
            }
            // 목록에 늑대가 없다 = 게임에서 나감 → 사망으로 취급
            return true;
        }
    }
    public class OstrichWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, SyncList<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
    public class MeerkatWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, SyncList<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
    public class ZebraWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, SyncList<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
    public class HippoWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, SyncList<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
    public class ScorpionWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, SyncList<GamePlayer> allPlayers)
        {
            // 독으로 포식자 반격에 성공했으면 승리(전갈 생존 여부와 무관)
            var ability = player.GetComponent<ScorpionAbility>();
            return ability != null && ability.HasCountered;
        }
    }
    public class CrowWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, SyncList<GamePlayer> allPlayers)
        {
            foreach (var p in allPlayers)
            {
                if (p.animalType == AnimalType.Wolf)
                {
                    return p.isAlive; // 늑대의 승리
                }
            }
            return false;
        }
    }
    public class PloverWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, SyncList<GamePlayer> allPlayers)
        {
            foreach (var p in allPlayers)
            {
                if (p.animalType == AnimalType.Crocodile)
                {
                    return p.isAlive; // 악어의 승리
                }
            }
            return false;
        }
    }
    // 올빼미: 예측 오브젝트에서 고른 플레이어가 승리하면 함께 승리한다(올빼미 본인의 생존 여부와 무관).
    // 예측하지 않았거나 고른 플레이어가 게임에서 나갔으면 패배
    public class OwlWinCondition : IDependentWinCondition
    {
        // 결과는 GameResultEvaluator가 GetDependency로 계산한다. 단독으로 불리면 판단할 수 없으므로 패배
        public bool Evaluate(GamePlayer player, SyncList<GamePlayer> allPlayers)
        {
            return false;
        }

        public GamePlayer GetDependency(GamePlayer player, SyncList<GamePlayer> allPlayers)
        {
            uint predictedId = player.predictedPlayerNetId;
            if (predictedId == 0) return null;

            foreach (var p in allPlayers)
            {
                if (p != null && p != player && p.netId == predictedId)
                    return p;
            }
            return null;
        }
    }

    public class ChameleonWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, SyncList<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
}
