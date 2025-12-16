using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SeedA : BaseMission
{
    [Header("Spawn")]
    [SerializeField] private RectTransform parent;
    [SerializeField] private RectTransform[] spawnPoints;
    [SerializeField] private Vector2 padding = new Vector2(50f, 50f);
    [SerializeField] private float spawnRadius = 150f;

    [Header("Setting")]
    [SerializeField] private int seedCount = 4;
    [SerializeField] private int stoneCount = 6;
    [SerializeField] private int trashCount = 4;
    [SerializeField] private int hiddenSeedCount = 2;

    [Header("Item")]
    [SerializeField] private SeedItem seedPrefab;
    [SerializeField] private SeedItem stonePrefab;
    [SerializeField] private SeedItem trashPrefab;

    [Header("Obj")]
    [SerializeField] private SeedBag bag;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private int targetSeedToCollect = 4;

    public SeedBag Bag => bag;
    private readonly List<SeedItem> _spawnedSeeds = new();
    private readonly List<SeedItem> _spawnedCoverObjects = new();
    private int _collectedCount;

    private float timeLimit = 20f;
    private float remainTime;
    private bool isComplete;

    private void Awake()
    {
        MissionType = MissionType.Seed_A;
    }
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

        ClearData();
       
        UpdateUI();

        SpawnAll();
    }

    public override void End()
    {
        base.End();
        ClearData();
    }

    private void ClearData()
    {
        foreach (var s in _spawnedSeeds)
        {
            if (s != null) Destroy(s.gameObject);
        }
        _spawnedSeeds.Clear();

        foreach (var c in _spawnedCoverObjects)
        {
            if (c != null) Destroy(c.gameObject);
        }
        _spawnedCoverObjects.Clear();

        statusText.text = "";
        isComplete = false;
        remainTime = timeLimit;
        _collectedCount = 0;
    }

    private void SpawnAll()
    {
        int initCount = seedCount + stoneCount + trashCount;

        int spawnPointLength = spawnPoints.Length;
        int spawnCount = 0;
        while (spawnCount != initCount)
        {
            if(spawnCount < seedCount)
            {
                var seed = SpawnItem(seedPrefab, BagBase.BagItemType.Seed, spawnCount % spawnPointLength);
                if (seed != null)
                    _spawnedSeeds.Add(seed);
            }
            else if(spawnCount < seedCount + stoneCount)
            {
                var stone = SpawnItem(stonePrefab, BagBase.BagItemType.Stone, spawnCount % spawnPointLength);
                if (stone != null)
                    _spawnedCoverObjects.Add(stone);
            }
            else
            {
                var trash = SpawnItem(trashPrefab, BagBase.BagItemType.Trash, spawnCount % spawnPointLength);
                if (trash != null)
                    _spawnedCoverObjects.Add(trash);
            }
            spawnCount++;
        }

        HideSomeSeedsUnderCovers();
    }
    private SeedItem SpawnItem(SeedItem prefab, BagBase.BagItemType type, int index)
    {
        if (prefab == null || parent == null)
            return null;

        SeedItem inst = Instantiate(prefab, parent);

        RectTransform rt = inst.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = GetRandomSpawnPointPosition(index);
            rt.localScale = Vector3.one;
        }

        inst.Init(this, type);
        return inst;
    }
    private Vector2 GetRandomSpawnPointPosition(int index)
    {
        Rect area = parent.rect;

        var sp = spawnPoints[index];

        Vector2 centerLocal = parent.InverseTransformPoint(sp.position);

        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector2 pos = centerLocal + offset;

        float xMin = area.xMin + padding.x;
        float xMax = area.xMax - padding.x;
        float yMin = area.yMin + padding.y;
        float yMax = area.yMax - padding.y;

        pos.x = Mathf.Clamp(pos.x, xMin, xMax);
        pos.y = Mathf.Clamp(pos.y, yMin, yMax);

        return pos;
    }
    
    private void HideSomeSeedsUnderCovers()
    {
        if (_spawnedSeeds.Count == 0 || _spawnedCoverObjects.Count == 0)
            return;

        int count = Mathf.Min(hiddenSeedCount, _spawnedSeeds.Count, _spawnedCoverObjects.Count);

        for (int i = 0; i < count; i++)
        {
            var seed = _spawnedSeeds[Random.Range(0, _spawnedSeeds.Count - 1)];
            var cover = _spawnedCoverObjects[Random.Range(0, _spawnedCoverObjects.Count - 1)];

            if (seed == null || cover == null)
                continue;

            RectTransform seedRt = seed.GetComponent<RectTransform>();
            RectTransform coverRt = cover.GetComponent<RectTransform>();

            if (seedRt == null || coverRt == null)
                continue;

            seedRt.anchoredPosition = coverRt.anchoredPosition + new Vector2(0f, -10f);

            int seedIndex = seedRt.GetSiblingIndex();
            int coverIndex = coverRt.GetSiblingIndex();

            if (coverIndex <= seedIndex)
            {
                coverRt.SetSiblingIndex(seedIndex + 1);
            }
        }
    }    
    public void OnSeedCollected(SeedItem seed)
    {
        _collectedCount++;
        UpdateUI();

        _spawnedSeeds.Remove(seed);

        if (_collectedCount >= targetSeedToCollect)
        {           
            StartCoroutine(CompleteCoroutine());
        }
    }
    IEnumerator CompleteCoroutine()
    {
        statusText.text = "성 공";
        statusText.color = Color.green;
        statusText.rectTransform.SetAsLastSibling();
        isComplete = true;
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
    private void UpdateUI()
    {
        if (progressText != null)
        {
            progressText.text = $"가방에 씨앗 담기 ({_collectedCount} / {targetSeedToCollect})";
        }
    }
}
    