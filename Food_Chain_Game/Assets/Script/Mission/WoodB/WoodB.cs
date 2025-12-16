using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WoodB : BaseMission
{
    [Header("Spawn Area")]
    [SerializeField] private TreeObject tree;
    [SerializeField] private RectTransform[] spawnArea;
    [SerializeField] private RectTransform playPanel;

    [Header("Prefabs")]
    [SerializeField] private WoodItemB woodPrefab;   
    [SerializeField] private WoodItemB leafPrefab;
    [SerializeField] private WoodItemB stonePrefab;
    [SerializeField] private WoodItemB wormPrefab;

    [Header("Bag")]
    public WoodBag bag;
    [SerializeField] private RectTransform bagRect;
    [SerializeField] private int targetWoodCount = 8;

    [Header("Spawn Limits")]
    [SerializeField] private int maxWoodSpawnCount = 8;
    [SerializeField] private int maxJunkSpawnCount = 8;

    [Header("Wood Probability")]
    [SerializeField][Range(0f, 1f)] private float baseChance = 0.2f;
    [SerializeField][Range(0f, 1f)] private float bonusPerFail = 0.15f;
    [SerializeField][Range(0f, 1f)] private float maxChance = 1f;

    [Header("Junk Spawn")]
    [SerializeField][Range(0f, 1f)] private float stoneWeight = 0.2f;
    [SerializeField][Range(0f, 1f)] private float leafWeight = 0.3f;
    [SerializeField][Range(0f, 1f)] private float wormWeight = 0.1f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private float timeLimit = 30f;

    private float _remainTime;
    private bool _isComplete;

    private int _woodCollected;
    private int _failCount;
    private int _spawnedWoodCount;
    private int _spawnedJunkCount;
    private List<WoodItemB> _spawnedItems = new List<WoodItemB>();
    private void Update()
    {
        if (_isComplete) return;

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
        MissionType = MissionType.Wood_B;

        ClearObject();

        _isComplete = false;
        _remainTime = timeLimit;
        _failCount = 0;
        _woodCollected = 0;
        _spawnedWoodCount = 0;
        _spawnedJunkCount = 0;

        statusText.text = "";

        tree.OnTreeShaken = OnTreeShaken;

        UpdateProgressUI();
    }
    private void ClearObject()
    {
        foreach (var item in _spawnedItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        _spawnedItems.Clear();
    }
    public void OnTreeShaken()
    {
        if (_isComplete) return;

        bool woodCap = _spawnedWoodCount >= maxWoodSpawnCount;
        bool junkCap = _spawnedJunkCount >= maxJunkSpawnCount;
        if (woodCap && junkCap)
            return;

        TrySpawnDrop(woodCap, junkCap);
    }

    private void TrySpawnDrop(bool woodCap, bool junkCap)
    {
        bool isWood = false;

        if (!woodCap)
        {
            float chance = Mathf.Min(
                baseChance + _failCount * bonusPerFail,
                maxChance
            );

            float roll = Random.value;
            if (roll < chance)
                isWood = true;
        }

        if (isWood && !woodCap)
        {
            if (SpawnWood())
            {
                _failCount = 0;
                _spawnedWoodCount++;
            }
        }
        else
        {
            _failCount++;

            if (!junkCap)
            {
                bool spawned = SpawnJunk();
                if (spawned)
                    _spawnedJunkCount++;
            }
        }
    }
    private bool SpawnWood()
    {
        if (woodPrefab == null) return false;

        if (!TryGetRandomSpawnArea(out Vector2 pos))
            return false;

        WoodItemB inst = Instantiate(woodPrefab, playPanel);

        _spawnedItems.Add(inst);
        inst.Init(this, pos);

        return true;
    }

    private bool SpawnJunk()
    {
        if (!TryGetRandomSpawnArea(out Vector2 pos))
            return false;

        float total = wormWeight + leafWeight + stoneWeight;
        float roll = Random.value;

        if (roll > total)
            return false;

        WoodItemB prefab = null;
        float acc = wormWeight;
        if (roll <= acc)
        {
            prefab = wormPrefab;
        }
        else
        {
            acc += leafWeight;
            if (roll <= acc)
                prefab = leafPrefab;
            else
                prefab = stonePrefab;
        }

        if (prefab == null) return false;

        WoodItemB inst = Instantiate(prefab, playPanel);

        _spawnedItems.Add(inst);
        inst.Init(this, pos);

        return true;
    }

    private bool TryGetRandomSpawnArea(out Vector2 anchoredPos)
    {
        anchoredPos = Vector2.zero;

        if (playPanel == null) return false;
        if (spawnArea == null || spawnArea.Length == 0) return false;

        var candidates = System.Array.FindAll(spawnArea, sp => sp != null);
        if (candidates.Length == 0) return false;

        RectTransform spawnPoint = candidates[Random.Range(0, candidates.Length)];

        Canvas canvas = playPanel.GetComponentInParent<Canvas>();
        Camera cam = canvas != null ? canvas.worldCamera : null;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
            cam,
            spawnPoint.position
        );

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            playPanel,
            screenPos,
            cam,
            out localPos
        );

        anchoredPos = localPos;

        anchoredPos += new Vector2(Random.Range(-20f, 20f), Random.Range(-10f, 10f));

        return true;
    }

    public bool IsInBagArea(Vector2 screenPos)
    {
        if (bagRect == null) return false;

        Canvas canvas = bagRect.GetComponentInParent<Canvas>();
        Camera cam = canvas != null ? canvas.worldCamera : null;

        return RectTransformUtility.RectangleContainsScreenPoint(
            bagRect,
            screenPos,
            cam
        );
    }

    public void OnWoodCollected(WoodItemB item)
    {
        if (_isComplete) return;

        Destroy(item.gameObject);
        _spawnedItems.Remove(item);

        _woodCollected++;
        UpdateProgressUI();

        if (_woodCollected >= targetWoodCount)
        {
            StartCoroutine(CompleteCoroutine());
        }
    }

    public void OnWoodPutInBag(WoodItemB item)
    {
        if (item == null) return;

        bool isCorrect = (item.itemType == BagBase.BagItemType.Wood);

        if (isCorrect)
        {
            OnWoodCollected(item);
        }

        UpdateProgressUI();
    }
    private void UpdateProgressUI()
    {
        if (progressText != null)
        {
            progressText.text = $"나뭇가지 담기: {_woodCollected} / {targetWoodCount}";
        }
    }

    private IEnumerator CompleteCoroutine()
    {
        _isComplete = true;

        statusText.text = "성 공";
        statusText.color = Color.green;
        statusText.rectTransform.SetAsLastSibling();

        yield return new WaitForSeconds(2f);

        Complete();
    }

    private IEnumerator FailCoroutine()
    {
        _isComplete = true;

        statusText.text = "실 패";
        statusText.color = Color.red;
        statusText.rectTransform.SetAsLastSibling();

        yield return new WaitForSeconds(2f);

        Fail();
    }
}
