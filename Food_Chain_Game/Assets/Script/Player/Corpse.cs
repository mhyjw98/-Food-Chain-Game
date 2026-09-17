using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class Corpse : NetworkBehaviour
{
    [SyncVar] public List<uint> scanPlayers;
    [SyncVar] public uint deadPlayerNetId;
    [SyncVar] public AnimalType KillerType;
    [SyncVar] public AnimalType deadAnimalType;

    [SyncVar] public double deathServerTime;
    [SyncVar] public int deathDay;
    [SyncVar] public bool deathWasNight;
    [SyncVar] public float ElapsedSec;

    [Server]
    public void Init(uint deadId, AnimalType killerType, AnimalType corpseType, double serverTime, int day, bool phase, float sec)
    {       
        deadPlayerNetId = deadId;
        KillerType = killerType;
        deadAnimalType = corpseType;
        deathServerTime = serverTime;
        deathDay = day;
        deathWasNight = phase;
        ElapsedSec = sec;
    }
}
