using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MissionSelector;

public class MissionSelector : MonoBehaviour
{
    public enum MissionCategory
    {
        Grass, Seed, Fruit, Insect, Fish, Branch, Shellfish, Crops, Meat
    }
    private static readonly Dictionary<AnimalType, MissionCategory[]> _missionPool =
        new Dictionary<AnimalType, MissionCategory[]>
   
        {
            { AnimalType.Squirrel, new[] // 다람쥐
            {
                MissionCategory.Grass,                
                MissionCategory.Seed,
                MissionCategory.Fruit,
                MissionCategory.Insect
            }
        },

        { AnimalType.Ostrich, new[] // 타조
            {
                MissionCategory.Seed,
                MissionCategory.Branch,
                MissionCategory.Crops,
                MissionCategory.Insect
            }
        },

        { AnimalType.Badger, new[] // 오소리
            {
                MissionCategory.Fruit,
                MissionCategory.Branch,
                MissionCategory.Fish,
                MissionCategory.Insect,
            }
        },

        { AnimalType.Zebra, new[] // 얼룩말
            {
                MissionCategory.Grass,
                MissionCategory.Branch,
                MissionCategory.Fruit,
                MissionCategory.Crops,
            }
        },

        { AnimalType.Skunk, new[] // 스컹크
            {
                MissionCategory.Insect,
                MissionCategory.Fruit,
                MissionCategory.Grass,
                MissionCategory.Seed,
                MissionCategory.Fish,
            }
        },
        { AnimalType.Crow, new[] // 까마귀
            {
                MissionCategory.Insect,
                MissionCategory.Fruit,
                MissionCategory.Crops,
            }
        },

        { AnimalType.Plover, new[] // 악어새
            {
                MissionCategory.Meat,
                MissionCategory.Insect,
                MissionCategory.Seed,
            }
        },

        { AnimalType.Scorpion, new[] // 전갈
            {
                MissionCategory.Fish,
                MissionCategory.Insect,
                MissionCategory.Crops
            }
        },

        { AnimalType.Fox, new[]
            {
                MissionCategory.Insect,
                MissionCategory.Fruit,
                MissionCategory.Crops,
                MissionCategory.Meat,
                MissionCategory.Fish
            }
        },       
    };

    public static MissionCategory[] GetCategoriesForAnimal(AnimalType animal)
    {
        if (!_missionPool.TryGetValue(animal, out var categories))
            return System.Array.Empty<MissionCategory>();

        return categories;
    }

    public static MissionType[] GetRandomMissions(AnimalType animal, int missionCount)
    {
        var categories = GetCategoriesForAnimal(animal);
        if (categories == null || categories.Length == 0)
        {
            Debug.LogWarning($"[MissionSelector] {animal} 에 대한 카테고리 없음");
            return System.Array.Empty<MissionType>();
        }

        var uniqueCategories = new List<MissionCategory>();
        foreach (var c in categories)
        {
            if (!uniqueCategories.Contains(c))
                uniqueCategories.Add(c);
        }

        var selectedMissions = new List<MissionType>();

        for (int i = 0; i < missionCount; i++)
        {
            MissionCategory chosenCategory;

            if (i < uniqueCategories.Count)
            {
                chosenCategory = uniqueCategories[i];
            }
            else
            {
                int randIdx = UnityEngine.Random.Range(0, uniqueCategories.Count);
                chosenCategory = uniqueCategories[randIdx];
            }

            var pool = MissionMeta.GetTypesInCategory(chosenCategory);
            if (pool.Count == 0)
            {
                Debug.LogWarning($"[MissionSelector] 카테고리 {chosenCategory} 에 속한 미션이 없습니다.");
                continue;
            }

            int mi = Random.Range(0, pool.Count);
            var missionType = pool[mi];
            selectedMissions.Add(missionType);
        }

        return selectedMissions.ToArray();
    }
}

public static class MissionMeta
{
    public static readonly Dictionary<MissionType, MissionCategory> CategoryByType =
        new Dictionary<MissionType, MissionCategory>
    {
        // 풀
        { MissionType.Grass_A, MissionCategory.Grass },
        { MissionType.Grass_B, MissionCategory.Grass },
        { MissionType.Grass_C, MissionCategory.Grass },

        // 씨앗
        { MissionType.Seed_A, MissionCategory.Seed },
        { MissionType.Seed_B, MissionCategory.Seed },

        // 열매
        { MissionType.Fruit_A, MissionCategory.Fruit },
        { MissionType.Fruit_B, MissionCategory.Fruit },

        // 곤충
        { MissionType.Insect_A, MissionCategory.Insect },
        { MissionType.Insect_B, MissionCategory.Insect },

        // 어류
        { MissionType.Fish_A, MissionCategory.Fish },
        { MissionType.Fish_B, MissionCategory.Fish },

        // 나뭇가지
        { MissionType.Wood_A, MissionCategory.Branch },
        { MissionType.Wood_B, MissionCategory.Branch },

        // 작물
        { MissionType.Crops_A, MissionCategory.Crops },
        { MissionType.Crops_B, MissionCategory.Crops },

        // 고기
        { MissionType.Meat_A, MissionCategory.Meat },
        { MissionType.Meat_B, MissionCategory.Meat },
    };

    public static List<MissionType> GetTypesInCategory(MissionCategory category)
    {
        var list = new List<MissionType>();
        foreach (var kv in CategoryByType)
        {
            if (kv.Value == category)
                list.Add(kv.Key);
        }
        return list;
    }
}
