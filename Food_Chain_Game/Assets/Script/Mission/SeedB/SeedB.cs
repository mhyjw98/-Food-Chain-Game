using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

public class SeedB : BaseMission
{
    public enum Phase
    {
        Digging,
        Planting,
        Covering,
        Completed
    }    
    [SerializeField] private SoilCell[] soilCells;
    [SerializeField] private ShovelFollow shovel;
    [SerializeField] private SeedPacket packet;
    [SerializeField] private SeedPacket seedPacket;

    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private int targetSeedCount = 4;

    private Phase _phase;
    private int _dugCount;
    private int _plantedCount;
    private int _coveredCount;
    private List<PlantSeed> plantSeeds = new();

    private float timeLimit = 20f;
    private float remainTime;
    private bool isComplete;
    public Phase CurrentPhase => _phase;

    private void Update()
    {
        if (isComplete) return;

        remainTime -= Time.deltaTime;
        timerText.text = remainTime.ToString("0.0");

        if (remainTime <= 0f)
        {
            remainTime = 0f;
            StartCoroutine(FailCoroutine());
        }
    }
    public override void Begin()
    {
        base.Begin();
        MissionType = MissionType.Seed_B;

        remainTime = timeLimit;
        _phase = Phase.Digging;
        _dugCount = 0;
        _plantedCount = 0;
        _coveredCount = 0;
        statusText.text = "";
        isComplete = false;

        shovel.SetFollow(true);
        shovel.SetMode(ShovelFollow.Mode.Dig);

        seedPacket.SetOpened(false);

        foreach (var cell in soilCells)
            cell.Init(this);

        ResetSeed();
    }

    public void OnSoilDug(SoilCell cell)
    {
        if (_phase != Phase.Digging) return;

        _dugCount++;
        if (_dugCount >= soilCells.Length)
        {
            _phase = Phase.Planting;
            shovel.SetFollow(false);
            shovel.GoToRestPosition();
            seedPacket.SetOpened(true);
        }
    }

    public void OnSeedPlanted(SoilCell cell)
    {
        if (_phase != Phase.Planting) return;

        _plantedCount++;
        if (_plantedCount >= targetSeedCount)
        {
            _phase = Phase.Covering;
            shovel.SetFollow(true);
            shovel.SetMode(ShovelFollow.Mode.Cover);
            seedPacket.SetOpened(false);
        }
    }

    public void OnSoilCovered(SoilCell cell)
    {
        if (_phase != Phase.Covering) return;

        _coveredCount++;
        if (_coveredCount >= soilCells.Length)
        {
            _phase = Phase.Completed;
            StartCoroutine(CompleteCoroutine());
        }
    }

    public void AddSeedData(PlantSeed seed)
    {
        plantSeeds.Add(seed);
    }

    public void ResetSeed()
    {
        foreach (var item in plantSeeds)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        plantSeeds.Clear();
    }

    IEnumerator CompleteCoroutine()
    {
        isComplete = true;
        statusText.text = "성 공";
        statusText.color = Color.green;
        statusText.rectTransform.SetAsLastSibling();
        yield return new WaitForSeconds(2);

        Complete();
    }

    IEnumerator FailCoroutine()
    {
        statusText.text = "실 패";
        statusText.color = Color.red;
        statusText.rectTransform.SetAsLastSibling();
        yield return new WaitForSeconds(2);

        Fail();
    }
}
