using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SoilCell : MonoBehaviour, IPointerClickHandler
{
    public enum State { Normal, Dug, SeedPlanted, Covered }

    [SerializeField] private SeedB mission;
    [SerializeField] private GameObject dugVisual;
    [SerializeField] private GameObject seedVisual;
    [SerializeField] private GameObject coveredVisual;

    private State _state;

    public void Init(SeedB mission)
    {
        this.mission = mission;
        _state = State.Normal;

        dugVisual.SetActive(false);
        seedVisual.SetActive(false);
        coveredVisual.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_state == State.Normal && mission.CurrentPhase == SeedB.Phase.Digging)
        {
            _state = State.Dug;
            dugVisual.SetActive(true);
            mission.OnSoilDug(this);
        }
        else if (_state == State.SeedPlanted && mission.CurrentPhase == SeedB.Phase.Covering)
        {
            _state = State.Covered;
            coveredVisual.SetActive(true);
            mission.OnSoilCovered(this);
        }
    }
    public void AcceptSeed(PlantSeed seed)
    {
        if (mission.CurrentPhase != SeedB.Phase.Planting)
            return;

        if (_state != State.Dug)
            return;

        _state = State.SeedPlanted;
        seedVisual.SetActive(true);

        mission.AddSeedData(seed);
        mission.OnSeedPlanted(this);
    }
}
