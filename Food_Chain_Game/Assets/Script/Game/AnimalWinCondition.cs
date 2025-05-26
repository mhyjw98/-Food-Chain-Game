using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static CharacterData;

public class AnimalWinCondition : MonoBehaviour
{
    public class LionWinCondition : IWinCondition
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
    public class EagleWinCondition : IWinCondition
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
                if (p.animalType == AnimalType.Lion)
                {
                    return !p.isAlive; // ���ڰ� ������ �¸�
                }
            }
            return false;
        }
    }
    public class MallardWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
    public class RabbitWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
    public class DeerWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
    public class OtterWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
    public class SnakeWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            // 8�� �̻� ���
            int deadCount = allPlayers.Count(p => !p.isAlive);
            return deadCount >= 8;
        }
    }
    public class MouseWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            foreach (var p in allPlayers)
            {
                if (p.animalType == AnimalType.Lion)
                {
                    return p.isAlive; // ������ �¸�
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
                    return p.isAlive; // �Ǿ��� �¸�
                }
            }
            return false;
        }
    }
    public class ChameleonWinCondition : IWinCondition
    {
        public bool Evaluate(GamePlayer player, List<GamePlayer> allPlayers)
        {
            return player.isAlive;
        }
    }
}
