using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CharacterData
{
    public enum CharacterType
    {
        Lion, Crocodile, Eagle, Hyena, Snake,
        Chameleon, Deer, Otter, Rabbit, Mallard,
        Crow, Plover, Mouse
    }

    public enum TerritoryType
    {
        Sky, River, Field, Forest
    }

    public static class CharacterConfig
    {
        public static readonly Dictionary<CharacterType, CharacterInfoData> Characters = new()
    {
        { CharacterType.Lion, new CharacterInfoData("사자", TerritoryType.Field, "동물들을 잡아먹으며 생존하세요.\n한 라운드라도 굶으면 사망합니다.") },
        { CharacterType.Crocodile, new CharacterInfoData("악어", TerritoryType.River, "동물들을 잡아먹으며 생존하세요.\n두 라운드를 연속으로 굶으면 사망합니다.") },
        { CharacterType.Eagle, new CharacterInfoData("독수리", TerritoryType.Sky, "동물들을 잡아먹으며 생존하세요.\n두 라운드를 연속으로 굶으면 사망합니다.") },
        { CharacterType.Hyena, new CharacterInfoData("하이에나", TerritoryType.Field, "사자가 사망하면 승리합니다.\n세 라운드를 연속으로 굶으면 사망합니다.") },
        { CharacterType.Snake, new CharacterInfoData("뱀", TerritoryType.Forest, "9명 이상의 동물들이 사망시 승리합니다.") },
        { CharacterType.Chameleon, new CharacterInfoData("카멜레온", TerritoryType.Forest, "게임 종료까지 살아남으세요.") },
        { CharacterType.Deer, new CharacterInfoData("사슴", TerritoryType.Field, "게임 종료까지 살아남으세요. ") },
        { CharacterType.Otter, new CharacterInfoData("수달", TerritoryType.River, "게임 종료까지 살아남으세요.") },
        { CharacterType.Rabbit, new CharacterInfoData("토끼", TerritoryType.Forest, "게임 종료까지 살아남으세요.") },
        { CharacterType.Mallard, new CharacterInfoData("청둥오리", TerritoryType.Sky, "게임 종료까지 살아남으세요.") },
        { CharacterType.Crow, new CharacterInfoData("까마귀", TerritoryType.Sky, "누가 승리 예측하세요.") },
        { CharacterType.Plover, new CharacterInfoData("악어새", TerritoryType.River, "악어가 생존할 수 있도록 도와주세요.") },
        { CharacterType.Mouse, new CharacterInfoData("쥐", TerritoryType.Forest, "사자가 생존할 수 있도록 도와주세요.") },
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
        { PredatorType.Snake, -1 },
        { PredatorType.Hyena, 1 },
        { PredatorType.Eagle, 2 },
        { PredatorType.Crocodile, 3 },
        { PredatorType.Lion, 4 },
        { PredatorType.Prey, 0 }
    };

    public static int GetPriority(PredatorType type)
    {
        return priorityMap.TryGetValue(type, out var p) ? p : 0;
    }

    public static bool CanAttack(PredatorType attacker, PredatorType target)
    {
        if (attacker == PredatorType.Snake) return false;
        if (target == PredatorType.Snake) return false;
        return GetPriority(attacker) > GetPriority(target);
    }
}
