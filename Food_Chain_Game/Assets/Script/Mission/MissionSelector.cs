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

        Shuffle(uniqueCategories);

        var selected = new List<MissionType>(missionCount);
        var selectedSet = new HashSet<MissionType>();

        int firstCount = Mathf.Min(missionCount, uniqueCategories.Count);
        for (int i = 0; i < firstCount; i++)
        {
            if (TryPickUniqueMission(uniqueCategories[i], selectedSet, out var picked))
            {
                selected.Add(picked);
                selectedSet.Add(picked);
            }
        }

        int guard = 0;
        while (selected.Count < missionCount && guard++ < 200)
        {
            var cat = uniqueCategories[Random.Range(0, uniqueCategories.Count)];

            if (TryPickUniqueMission(cat, selectedSet, out var picked))
            {
                selected.Add(picked);
                selectedSet.Add(picked);
            }
        }

        return selected.ToArray();
    }

    private static bool TryPickUniqueMission(MissionCategory category, HashSet<MissionType> already, out MissionType picked)
    {
        picked = default;

        var pool = MissionMeta.GetTypesInCategory(category);
        if (pool == null || pool.Count == 0)
            return false;

        List<MissionType> candidates = null;
        for (int i = 0; i < pool.Count; i++)
        {
            var t = pool[i];
            if (already.Contains(t)) continue;

            candidates ??= new List<MissionType>();
            candidates.Add(t);
        }

        if (candidates == null || candidates.Count == 0)
            return false;

        picked = candidates[Random.Range(0, candidates.Count)];
        return true;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
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
        //{ MissionType.Grass_C, MissionCategory.Grass },

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
