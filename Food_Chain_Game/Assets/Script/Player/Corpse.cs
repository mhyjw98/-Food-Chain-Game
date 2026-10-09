using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class Corpse : NetworkBehaviour
{
    // 사망 정보는 서버만 보관한다(SyncVar 아님).
    // 조사에 성공한 조력자에게만 GamePlayer.TargetCorpseScanResult로 전달된다.
    public uint deadPlayerNetId;
    public AnimalType KillerType;
    public AnimalType deadAnimalType;
    public DeathReason deathReason;

    public double deathServerTime;
    public int deathDay;
    public bool deathWasNight;
    public float ElapsedSec;

    [Server]
    public void Init(uint deadId, AnimalType killerType, AnimalType corpseType, DeathReason reason, double serverTime, int day, bool phase, float sec)
    {
        deadPlayerNetId = deadId;
        KillerType = killerType;
        deadAnimalType = corpseType;
        deathReason = reason;
        deathServerTime = serverTime;
        deathDay = day;
        deathWasNight = phase;
        ElapsedSec = sec;
    }
}
