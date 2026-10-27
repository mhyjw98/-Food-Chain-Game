using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 프리팹/씬에 정수로 저장되므로 값을 명시한다. 새 항목은 반드시 끝 번호 뒤에 추가하고 기존 값은 바꾸지 말 것
public enum MissionType
{
    Grass_A = 0, Grass_B = 1, Grass_C = 2,
    Seed_A = 3, Seed_B = 4,
    Fruit_A = 5, Fruit_B = 6,
    Wood_A = 7, Wood_B = 8,
    Crops_A = 9, Crops_B = 10,
    Insect_A = 11, Insect_B = 12,
    Fish_A = 13, Fish_B = 14,
    Meat_A = 15, Meat_B = 16,
    Scorpion_Poison = 17,
    RevengeA = 18, RevengeB = 19, RevengeC = 20, RevengeD = 21, RevengeE = 22
}
public enum MissionDenyReason : byte
{
    None,
    NotAssigned,
    AlreadyCompleted,
    CannotDoMissions,
    OutOfRange,
    InUse
}
public enum MissionStatus
{
    NotAssigned,
    NotStarted,
    InProgress,
    Completed 
}
public struct MissionSlot
{
    public MissionType Type;
    public MissionStatus Status;
}
public abstract class BaseMission : MonoBehaviour
{
    [SerializeField] private MissionType missionType;
    public MissionType MissionType => missionType;

    public Action<BaseMission> OnMissionCompleted;
    public Action<BaseMission> OnMissionFailed;

    public virtual void Begin()
    {
        gameObject.SetActive(true);
    }

    public virtual void End()
    {
        gameObject.SetActive(false);
    }

    // 완료/실패는 인스턴스당 한 번만 처리(같은 프레임에 두 번 호출돼도 콜백이 중복 실행되지 않게)
    private bool _hasEnded;

    protected void Complete()
    {
        if (_hasEnded) return;
        _hasEnded = true;

        OnMissionCompleted?.Invoke(this);
        End();
    }

    public virtual void Fail()
    {
        if (_hasEnded) return;
        _hasEnded = true;

        OnMissionFailed?.Invoke(this);
        End();
    }
}