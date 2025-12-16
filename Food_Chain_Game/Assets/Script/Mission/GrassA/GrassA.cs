using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class GrassData
{
    public int id;
    public Sprite sprite;
}
public class GrassA : BaseMission
{
    [Header("Spawn Area")]
    [SerializeField] private RectTransform parent;
    [SerializeField] private RectTransform bagRect;
    [SerializeField] private Vector2 offsetRange = new Vector2(40f, 40f);
    [SerializeField] private Vector2 padding = new Vector2(50f, 50f);
    [SerializeField] private RectTransform[] spawnGroup;

    [Header("Data")]
    [SerializeField] private GrassItem grassPrefab;
    [SerializeField] private GrassData[] grassCandidates;
    public GrassBag bag;
    public int _targetId;

    [Header("Setting")]
    [SerializeField] private int totalObjectCount = 8;

    [Header("UI")]
    [SerializeField] private Image sampleImage;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timerText;

    private int _targetIndex;    

    private List<GrassItem> _spawnedItems = new List<GrassItem>();

    private float timeLimit = 20f;
    private float remainTime;
    private bool isComplete;
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
        MissionType = MissionType.Grass_A;
        isComplete = false;
        remainTime = timeLimit;
        statusText.text = "";

        ClearSpawned();

        _targetIndex = Random.Range(0, grassCandidates.Length);
        GrassData target = grassCandidates[_targetIndex];
        _targetId = target.id;

        if (sampleImage != null)
            sampleImage.sprite = target.sprite;

        SpawnAll();
    }

    private void ClearSpawned()
    {
        foreach (var item in _spawnedItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        _spawnedItems.Clear();
    }

    private void SpawnAll()
    {
        int maxCount = Mathf.Min(totalObjectCount, spawnGroup.Length, grassCandidates.Length);

        for (int i = 0; i < maxCount; i++)
        {
            SpawnOneAtIndex(i);
        }
    }

    private void SpawnOneAtIndex(int index)
    {
        if (grassPrefab == null || parent == null) return;
        if (index < 0 || index >= spawnGroup.Length) return;

        var inst = Instantiate(grassPrefab, parent);
        var rt = inst.GetComponent<RectTransform>();

        if (rt != null)
        {
            rt.anchoredPosition = GetRandomPositionNearSpawn(index);
            rt.localScale = Vector3.one;
        }

        inst.Init(this, grassCandidates[index].id, grassCandidates[index].sprite);
        _spawnedItems.Add(inst);
    }
    private Vector2 GetRandomPositionNearSpawn(int index)
    {
        RectTransform spawnPoint = spawnGroup[index];

        Vector2 basePos = spawnPoint.anchoredPosition;

        float offsetX = Random.Range(-offsetRange.x, offsetRange.x);
        float offsetY = Random.Range(-offsetRange.y, offsetRange.y);

        Vector2 pos = basePos + new Vector2(offsetX, offsetY);

        Rect r = parent.rect;
        float xMin = r.xMin + padding.x;
        float xMax = r.xMax - padding.x;
        float yMin = r.yMin + padding.y;
        float yMax = r.yMax - padding.y;

        pos.x = Mathf.Clamp(pos.x, xMin, xMax);
        pos.y = Mathf.Clamp(pos.y, yMin, yMax);

        return pos;
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

    public void OnGrassPutInBag(GrassItem item)
    {
        if (item == null) return;

        bool isCorrect = (item.TypeId == _targetId);

        Destroy(item.gameObject);
        _spawnedItems.Remove(item);

        if (isCorrect)
        {
            StartCoroutine(CompleteCoroutine());
        }
        else
        {
            StartCoroutine(FailCoroutine());
        }      
    }

    private IEnumerator CompleteCoroutine()
    {
        isComplete = true;
        statusText.text = "성 공";
        statusText.color = Color.green;
        statusText.rectTransform.SetAsLastSibling();
        yield return new WaitForSeconds(2f);

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
