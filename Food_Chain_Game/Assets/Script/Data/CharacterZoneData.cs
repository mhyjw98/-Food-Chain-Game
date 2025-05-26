using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CharacterZoneData
{
    public static readonly Dictionary<string, ZoneType> CharacterHomeZone = new Dictionary<string, ZoneType>()
    {
        { "Lion", ZoneType.Field },
        { "Mouse", ZoneType.Forest },
        { "Crocodile", ZoneType.River },
        { "Rabbit", ZoneType.Forest },
        { "Deer", ZoneType.Field },
        { "Otter", ZoneType.River },
        { "Snake", ZoneType.Forest },
        { "Mallard", ZoneType.Sky },
        { "Eagle", ZoneType.Sky },
        { "Plover", ZoneType.River },
        { "Chameleon", ZoneType.Forest },
        { "Crow", ZoneType.Sky },
        { "Hyena", ZoneType.Field },
    };
}
