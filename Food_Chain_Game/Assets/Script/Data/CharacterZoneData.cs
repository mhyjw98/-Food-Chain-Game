using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CharacterZoneData
{
    public static readonly Dictionary<string, ZoneType> CharacterHomeZone = new Dictionary<string, ZoneType>()
    {
        // CharacterData.CharacterConfig의 HomeTerritory와 같은 값으로 유지할 것
        { "Wolf", ZoneType.Field },
        { "Crocodile", ZoneType.River },
        { "Hawk", ZoneType.Sky },
        { "Hyena", ZoneType.Field },
        { "Scorpion", ZoneType.Forest },
        { "Skunk", ZoneType.Forest },
        { "Zebra", ZoneType.Field },
        { "Badger", ZoneType.River },
        { "Squirrel", ZoneType.Forest },
        { "Ostrich", ZoneType.Sky },
        { "Crow", ZoneType.Sky },
        { "Plover", ZoneType.River },
        { "Fox", ZoneType.Forest },
    };
}
