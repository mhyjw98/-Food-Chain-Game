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
                    return !p.isAlive; // 사자가 죽으면 승리
                }
            }
            return false;
        }
    }
    public class OstrichWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, SyncList<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
    public class SquirrelWinCondition : IWinCondition
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
    public class BadgerWinCondition : IWinCondition
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
            // 8명 이상 사망
            int deadCount = allPlayers.Count(p => !p.isAlive);
            return deadCount >= 8;
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
    public class SkunkWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, SyncList<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }

    public class FoxWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, SyncList<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
}
