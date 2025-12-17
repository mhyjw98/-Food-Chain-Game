using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static CharacterData;

public class AnimalWinCondition : MonoBehaviour
{
    public class WolfWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
    public class CrocodileWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
    public class HawkWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
    public class HyenaWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            foreach (var p in allPlayers)
            {
                if (p.animalType == AnimalType.Wolf)
                {
                    return !p.isAlive; // »çÀÚ°¡ Á×À¸¸é ½Â¸®
                }
            }
            return false;
        }
    }
    public class OstrichWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
    public class SquirrelWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
    public class ZebraWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
    public class BadgerWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
    public class ScorpionWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            // 8¸í ÀÌ»ó »ç¸Á
            int deadCount = allPlayers.Count(p => !p.isAlive);
            return deadCount >= 8;
        }
    }
    public class CrowWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            foreach (var p in allPlayers)
            {
                if (p.animalType == AnimalType.Wolf)
                {
                    return p.isAlive; // ´Á´ëÀÇ ½Â¸®
                }
            }
            return false;
        }
    }
    public class PloverWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            foreach (var p in allPlayers)
            {
                if (p.animalType == AnimalType.Crocodile)
                {
                    return p.isAlive; // ¾Ç¾îÀÇ ½Â¸®
                }
            }
            return false;
        }
    }
    public class SkunkWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }

    public class FoxWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
}
