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
        { CharacterType.Lion, new CharacterInfoData("����", TerritoryType.Field, "�������� ��Ƹ����� �����ϼ���.\n�� ����� ������ ����մϴ�.") },
        { CharacterType.Crocodile, new CharacterInfoData("�Ǿ�", TerritoryType.River, "�������� ��Ƹ����� �����ϼ���.\n�� ���带 �������� ������ ����մϴ�.") },
        { CharacterType.Eagle, new CharacterInfoData("������", TerritoryType.Sky, "�������� ��Ƹ����� �����ϼ���.\n�� ���带 �������� ������ ����մϴ�.") },
        { CharacterType.Hyena, new CharacterInfoData("���̿���", TerritoryType.Field, "���ڰ� ����ϸ� �¸��մϴ�.\n�� ���带 �������� ������ ����մϴ�.") },
        { CharacterType.Snake, new CharacterInfoData("��", TerritoryType.Forest, "9�� �̻��� �������� ����� �¸��մϴ�.") },
        { CharacterType.Chameleon, new CharacterInfoData("ī�᷹��", TerritoryType.Forest, "���� ������� ��Ƴ�������.") },
        { CharacterType.Deer, new CharacterInfoData("�罿", TerritoryType.Field, "���� ������� ��Ƴ�������. ") },
        { CharacterType.Otter, new CharacterInfoData("����", TerritoryType.River, "���� ������� ��Ƴ�������.") },
        { CharacterType.Rabbit, new CharacterInfoData("�䳢", TerritoryType.Forest, "���� ������� ��Ƴ�������.") },
        { CharacterType.Mallard, new CharacterInfoData("û�տ���", TerritoryType.Sky, "���� ������� ��Ƴ�������.") },
        { CharacterType.Crow, new CharacterInfoData("���", TerritoryType.Sky, "���� �¸� �����ϼ���.") },
        { CharacterType.Plover, new CharacterInfoData("�Ǿ��", TerritoryType.River, "�Ǿ ������ �� �ֵ��� �����ּ���.") },
        { CharacterType.Mouse, new CharacterInfoData("��", TerritoryType.Forest, "���ڰ� ������ �� �ֵ��� �����ּ���.") },
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
