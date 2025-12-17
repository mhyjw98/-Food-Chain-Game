using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public static class CharacterData
{

    public enum TerritoryType
    {
        Sky, River, Field, Forest
    }

    public static class CharacterConfig
    {
        public static readonly Dictionary<AnimalType, CharacterInfoData> Characters = new()
        {
            { AnimalType.Wolf, new CharacterInfoData("늑대", TerritoryType.Field, "동물들을 잡아먹으며 생존하세요.\n한 라운드라도 굶으면 사망합니다.") },
            { AnimalType.Crocodile, new CharacterInfoData("악어", TerritoryType.River, "동물들을 잡아먹으며 생존하세요.\n두 라운드를 연속으로 굶으면 사망합니다.") },
            { AnimalType.Hawk, new CharacterInfoData("매", TerritoryType.Sky, "동물들을 잡아먹으며 생존하세요.\n두 라운드를 연속으로 굶으면 사망합니다.") },
            { AnimalType.Hyena, new CharacterInfoData("하이에나", TerritoryType.Field, "늑대가 사망하면 승리합니다.\n세 라운드를 연속으로 굶으면 사망합니다.") },
            { AnimalType.Scorpion, new CharacterInfoData("전갈", TerritoryType.Forest, "독으로 반격해 포식자를 죽이면 승리합니다.") },
            { AnimalType.Skunk, new CharacterInfoData("스컹크", TerritoryType.Forest, "게임 종료까지 살아남으세요.") },
            { AnimalType.Zebra, new CharacterInfoData("얼룩말", TerritoryType.Field, "게임 종료까지 살아남으세요. ") },
            { AnimalType.Badger, new CharacterInfoData("오소리", TerritoryType.River, "게임 종료까지 살아남으세요.") },
            { AnimalType.Squirrel, new CharacterInfoData("다람쥐", TerritoryType.Forest, "게임 종료까지 살아남으세요.") },
            { AnimalType.Ostrich, new CharacterInfoData("타조", TerritoryType.Sky, "게임 종료까지 살아남으세요.") },
            { AnimalType.Crow, new CharacterInfoData("까마귀", TerritoryType.Sky, "늑대가 생존할 수 있도록 도와주세요. 늑대가 사망시 패배합니다.") },
            { AnimalType.Plover, new CharacterInfoData("악어새", TerritoryType.River, "악어가 생존할 수 있도록 도와주세요. 악어가 사망시 패배합니다.") },
            { AnimalType.Fox, new CharacterInfoData("여우", TerritoryType.Forest, "포식자, 피식자 중 어느편에 붙을지 선택할 수 있습니다. 해당 진영 승리시 승리합니다.") },
        };
    }
    public struct CharacterInfoData
    {
        public string DisplayName;
        public TerritoryType HomeTerritory;
        public string WinConditionDescription;

        public CharacterInfoData(string displayName, TerritoryType territory, string winCondition)
        {
            DisplayName = displayName;
            HomeTerritory = territory;
            WinConditionDescription = winCondition;
        }
    }
}
public static class PredatorPriority
{
    private static readonly Dictionary<PredatorType, int> priorityMap = new()
    {
        { PredatorType.Scorpion, -1 },
        { PredatorType.Hyena, 1 },
        { PredatorType.Hawk, 2 },
        { PredatorType.Crocodile, 3 },
        { PredatorType.Wolf, 4 },
        { PredatorType.Prey, 0 }
    };

    public static int GetPriority(PredatorType type)
    {
        return priorityMap.TryGetValue(type, out var p) ? p : 0;
    }
    

    public static bool CanAttack(PredatorType attacker, PredatorType target)
    {
        if (attacker == PredatorType.Scorpion) return false;
        if (target == PredatorType.Scorpion) return false;
        return GetPriority(attacker) > GetPriority(target);
    }
}
