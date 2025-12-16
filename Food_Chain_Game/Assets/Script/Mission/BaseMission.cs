using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MissionType
{
    Grass_A, Grass_B, Grass_C,
    Seed_A, Seed_B,
    Fruit_A, Fruit_B,
    Wood_A, Wood_B,
    Crops_A, Crops_B,    
    Insect_A, Insect_B,
    Fish_A, Fish_B,
    Meat_A, Meat_B
}
public enum MissionStatus
{
    NotAssigned,
    NotStarted,
    InProgress,
    Completed 
}
[Serializable]
public class MissionSlot
{
    public MissionType Type;
    public MissionStatus Status;
}
public abstract class BaseMission : MonoBehaviour
{
    
    public MissionType MissionType { get; protected set; }

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

    protected void Complete()
    {
        OnMissionCompleted?.Invoke(this);
        End();
    }

    protected void Fail()
    {
        OnMissionFailed?.Invoke(this);
        End();
    }
}