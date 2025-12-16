using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MissionData
{
    private static readonly Dictionary<MissionType, string> _table
        = new Dictionary<MissionType, string>
    {
        // === 풀 ===
        {
            MissionType.Grass_A, "맛있는 풀 모으기"            
        },
        {
            MissionType.Grass_B, "풀 수집하기"
        },
        {
            MissionType.Grass_C, "풀 세척하기"
        },

        // === 씨앗 ===
        {
            MissionType.Seed_A, "씨앗 수집하기"
        },
        {
            MissionType.Seed_B, "씨앗 심기"
        },

        // === 열매 ===
        {
            MissionType.Fruit_A, "열매 수집하기"
        },
        {
            MissionType.Fruit_B, "열매 수집하기"
        },

        // === 곤충 ===
        {
            MissionType.Insect_A, "벌레 잡아먹기"
        },
        {
            MissionType.Insect_B, "풀숲에서 벌레 수집하기"
        },

        // === 어류 ===
        {
            MissionType.Fish_A, "물고기 낚기"
        },
        {
            MissionType.Fish_B, "물고기 잡기"
        },

        // === 나뭇가지 ===
        {
            MissionType.Wood_A, "잔가지 수집하기"
        },
        {
            MissionType.Wood_B, "잔가지 수집하기"
        },

        // === 작물 ===
        {
            MissionType.Crops_A, "작물 수집하기"
        },
        {
            MissionType.Crops_B, "작물 수확하기"
        },


        // === 고기 ===
        {
            MissionType.Meat_A, "악어 이빨 청소하기"
        },
        {
            MissionType.Meat_B, "고기 수집하기"
        },
    };

    public static string GetName(MissionType type)
    {
        return _table.TryGetValue(type, out var data)
            ? data
            : type.ToString();
    }
}
