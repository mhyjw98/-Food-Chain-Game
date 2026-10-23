using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AnimalWinCondition;
using static CharacterData;

public static class WinCondutionFactory
{
    private static readonly Dictionary<AnimalType, IWinCondition> conditions = new()
    {
        // 포식자
        { AnimalType.Wolf, new WolfWinCondition() },
        { AnimalType.Crocodile, new CrocodileWinCondition() },
        { AnimalType.Hawk, new HawkWinCondition() },
        { AnimalType.Hyena, new HyenaWinCondition() },
        // 피식자
        { AnimalType.Meerkat, new MeerkatWinCondition() },
        { AnimalType.Zebra, new ZebraWinCondition() },
        { AnimalType.Hippo, new HippoWinCondition() },        
        { AnimalType.Owl, new OwlWinCondition() },
        { AnimalType.Ostrich, new OstrichWinCondition() },
        // 조력자
        { AnimalType.Plover, new PloverWinCondition() },
        { AnimalType.Crow, new CrowWinCondition() },
        // 중립
        { AnimalType.Scorpion, new ScorpionWinCondition() },
        { AnimalType.Chameleon, new ChameleonWinCondition() },
    };

    public static IWinCondition GetCondition(AnimalType characterType)
    {
        return conditions.TryGetValue(characterType, out var condition)
            ? condition
            : null;
    }
}
