using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FruitA : BaseMission
{
    [Header("Spawn Area")]
    [SerializeField] private TreeObject tree;
    [SerializeField] private RectTransform[] spawnArea;
    [SerializeField] private RectTransform playPanel;

    [Header("Prefabs")]
    [SerializeField] private FruitItemA fruitPrefab;
    [SerializeField] private FruitItemA woodPrefab;
    [SerializeField] private FruitItemA leafPrefab;
    [SerializeField] private FruitItemA stonePrefab;

    [Header("Bag")]
    public FruitBag bag;
    [SerializeField] private RectTransform bagRect;
    [SerializeField] private int targetFruitCount = 4;

    [Header("Spawn Limits")]
    [SerializeField] private int maxFruitSpawnCount = 4;
    [SerializeField] private int maxJunkSpawnCount = 10;

    [Header("Fruit Probability")]
    [SerializeField][Range(0f, 1f)] private float baseChance = 0.2f;
    [SerializeField][Range(0f, 1f)] private float bonusPerFail = 0.15f;
    [SerializeField][Range(0f, 1f)] private float maxChance = 1f; 

    [Header("Junk Spawn")]
    [SerializeField][Range(0f, 1f)] private float woodWeight = 0.2f;
    [SerializeField][Range(0f, 1f)] private float leafWeight = 0.3f;
    [SerializeField][Range(0f, 1f)] private float stoneWeight = 0.1f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private float timeLimit = 30f;

    private float _remainTime;
    private bool _isComplete;

    private int _fruitCollected;
    private int _failCount;
    private int _spawnedFruitCount;
    private int _spawnedJunkCount;
    private List<FruitItemA> _spawnedItems = new List<FruitItemA>();
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
        MissionType = MissionType.Fruit_A;

        ClearObject();

        _isComplete = false;
        _remainTime = timeLimit;
        _failCount = 0;
        _fruitCollected = 0;
        _spawnedFruitCount = 0;
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

        bool fruitCap = _spawnedFruitCount >= maxFruitSpawnCount;
        bool junkCap = _spawnedJunkCount >= maxJunkSpawnCount;
        if (fruitCap && junkCap)
            return;

        TrySpawnDrop(fruitCap, junkCap);
    }

    private void TrySpawnDrop(bool fruitCap, bool junkCap)
    {
        bool isFruit = false;

        if (!fruitCap)
        {
            float chance = Mathf.Min(
                baseChance + _failCount * bonusPerFail,
                maxChance
            );

            float roll = Random.value;
            if (roll < chance)
                isFruit = true;            
        }

        if (isFruit && !fruitCap)
        {
            if (SpawnFruit())
            {
                _failCount = 0;
                _spawnedFruitCount++;
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
    private bool SpawnFruit()
    {
        if (fruitPrefab == null) return false;

        if (!TryGetRandomSpawnArea(out Vector2 pos))
            return false;

        FruitItemA inst = Instantiate(fruitPrefab, playPanel);

        _spawnedItems.Add(inst);
        inst.Init(this, pos);

        return true;
    }

    private bool SpawnJunk()
    {
        if (!TryGetRandomSpawnArea(out Vector2 pos))
            return false;

        float total = woodWeight + leafWeight + stoneWeight;
        float roll = Random.value;

        if (roll > total)
            return false;

        FruitItemA prefab = null;
        float acc = woodWeight;
        if (roll <= acc)
        {
            prefab = woodPrefab;
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

        FruitItemA inst = Instantiate(prefab, playPanel);

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

    public void OnFruitCollected(FruitItemA item)
    {
        if (_isComplete) return;

        Destroy(item.gameObject);
        _spawnedItems.Remove(item);

        _fruitCollected++;
        UpdateProgressUI();

        if (_fruitCollected >= targetFruitCount)
        {
            StartCoroutine(CompleteCoroutine());
        }
    }

    public void OnFruitPutInBag(FruitItemA item)
    {
        if (item == null) return;

        bool isCorrect = (item.itemType == BagBase.BagItemType.Fruit);

        if (isCorrect)
        {
            OnFruitCollected(item);
        }

        UpdateProgressUI();
    }
    private void UpdateProgressUI()
    {
        if (progressText != null)
        {
            progressText.text = $"열매 담기: {_fruitCollected} / {targetFruitCount}";
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
