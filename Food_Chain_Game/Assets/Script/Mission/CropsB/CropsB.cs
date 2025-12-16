using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CropsB : BaseMission
{
    [Header("Spawn")]
    [SerializeField] private CropsItemB plantPrefab;
    [SerializeField] private RectTransform playPanel;
    [SerializeField] private RectTransform[] spawnPoints;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private float timeLimit = 20f;

    private int totalCount;
    private readonly List<CropsItemB> _plants = new List<CropsItemB>();

    private float _remainTime;
    private bool _isComplete;
    private int _successCount;

    public bool IsMissionFinished => _isComplete;
    
    private void Update()
    {
        if (_isComplete) return;

        // 전체 미션 타이머
        _remainTime -= Time.deltaTime;
        if (_remainTime < 0f) _remainTime = 0f;

        if (timerText != null)
            timerText.text = _remainTime.ToString("0.0");

        if (_remainTime <= 0f && !_isComplete)
        {
            StartCoroutine(FailCoroutine());
        }
    }

    public override void Begin()
    {
        base.Begin();
        MissionType = MissionType.Crops_B;

        ClearPlants();
        SpawnPlants();

        _isComplete = false;
        _remainTime = timeLimit;
        _successCount = 0;
        totalCount = _plants.Count;

        if (statusText != null)
        {
            statusText.text = "";
            statusText.color = Color.white;
        }       
    }
    private void ClearPlants()
    {
        foreach (var p in _plants)
        {
            if (p != null)
                Destroy(p.gameObject);
        }
        _plants.Clear();
    }

    private void SpawnPlants()
    {
        foreach (var sp in spawnPoints)
        {
            if (sp == null) continue;

            var inst = Instantiate(plantPrefab, playPanel);
            var instRect = inst.GetComponent<RectTransform>();
            var spRect = sp as RectTransform;

            if (instRect != null)
            {
                if (spRect != null)
                {
                    instRect.anchoredPosition = spRect.anchoredPosition;
                    instRect.localRotation = spRect.localRotation;
                }
                else
                {
                    Vector3 localPos = playPanel.InverseTransformPoint(sp.position);
                    instRect.localPosition = localPos;
                    instRect.localRotation = sp.rotation;
                }
            }

            float timeScale = Random.Range(0.6f, 1);
            inst.Init(this, timeScale);

            _plants.Add(inst);
        }
    }

    public void OnHarvestSuccess(CropsItemB plant)
    {
        if (_isComplete) return;
        if (!_plants.Contains(plant)) return;

        _successCount++;

        _plants.Remove(plant);
        Destroy(plant.gameObject);

        if (_successCount >= totalCount)
        {
            StartCoroutine(CompleteCoroutine());
        }
    }

    public void OnHarvestFailure(CropsItemB plant)
    {
        if (_isComplete) return;

        StartCoroutine(FailCoroutine());
    }

    private IEnumerator CompleteCoroutine()
    {
        _isComplete = true;

        if (statusText != null)
        {
            statusText.text = "성 공";
            statusText.color = Color.green;
            statusText.rectTransform.SetAsLastSibling();
        }

        yield return new WaitForSeconds(2f);

        Complete();
    }

    private IEnumerator FailCoroutine()
    {
        _isComplete = true;

        if (statusText != null)
        {
            statusText.text = "실 패";
            statusText.color = Color.red;
            statusText.rectTransform.SetAsLastSibling();
        }

        yield return new WaitForSeconds(2f);

        Fail();
    }
}
