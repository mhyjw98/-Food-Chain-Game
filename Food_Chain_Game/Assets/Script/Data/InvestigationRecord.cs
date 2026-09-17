using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InvestigationType
{
    Corpse,
    PlayerIdentity
}
public class InvestigationRecord
{
    // °ø¿ë
    public string recordId;
    public InvestigationType type;

    public string title;
    public string subtitle;
    public double createdAtLocal;

    
    public AnimalType targetAnimalType;

    // Corpse
    public AnimalType corpseAnimalType;   
    public int Day;
    public bool WasNight;
    public float ElapsedSec;
}
