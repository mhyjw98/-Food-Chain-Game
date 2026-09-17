using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerAbility
{
    void OnNewDay(int day);
    void OnBeforeAttack(ref AttackContext ctx);
    //void OnAfterDeath(DeathContext ctx);
}
public interface IAnimalAbility
{
    [Server] void ServerActivate(GamePlayer owner, AnimalType type);
}
public interface ISpecialMissionProvider
{
    IEnumerable<SpecialMission> GetSpecialMissions();
}
public interface IMissionCompleteHandler
{
    void OnMissionCompletedServer(GamePlayer owner, MissionType type);
}
[Serializable]
public struct SpecialMission
{
    public string id;
    public string title;
    public int priority;
    public bool isAvailable;
    public string description;
}
public struct AttackContext
{
    public GamePlayer attacker;
    public GamePlayer target;
    public bool cancelAttack;
    public bool killAttacker;
    public bool killTarget;
    public string reason;
}
