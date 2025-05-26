using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AnimalWinCondition;
using static CharacterData;

public static class WinCondutionFactory
{
    private static readonly Dictionary<AnimalType, IWinCondition> conditions = new()
    {
        { AnimalType.Lion, new LionWinCondition() },
        { AnimalType.Crocodile, new CrocodileWinCondition() },
        { AnimalType.Mouse, new MouseWinCondition() },
        { AnimalType.Rabbit, new RabbitWinCondition() },
        { AnimalType.Deer, new DeerWinCondition() },
        { AnimalType.Otter, new OtterWinCondition() },
        { AnimalType.Snake, new SnakeWinCondition() },
        { AnimalType.Mallard, new MallardWinCondition() },
        { AnimalType.Eagle, new EagleWinCondition() },
        { AnimalType.Plover, new PloverWinCondition() },
        { AnimalType.Chameleon, new ChameleonWinCondition() },
        { AnimalType.Hyena, new HyenaWinCondition() },
    };

    public static IWinCondition GetCondition(AnimalType characterType)
    {
        return conditions.TryGetValue(characterType, out var condition)
            ? condition
            : null;
    }
}
