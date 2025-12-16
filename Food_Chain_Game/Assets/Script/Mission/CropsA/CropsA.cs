using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CropsData
{
    public int id;
    public Sprite sprite;
}
public class CropsA : BaseMission
{
    [Header("Spawn Area")]
    [SerializeField] private RectTransform parent;          // Play Panel
    [SerializeField] private RectTransform bagRect;         // 가방 영역
    [SerializeField] private Vector2 offsetRange = new Vector2(40f, 40f);
    [SerializeField] private Vector2 padding = new Vector2(50f, 50f);
    [SerializeField] private RectTransform[] spawnGroup;    // 8개 스폰 포인트

    [Header("Data")]
    [SerializeField] private CropsItemA cropsPrefab;
    [SerializeField] private CropsData[] cropsCandidates;   // 여러 작물 데이터(id + sprite)

    [Header("Setting")]
    [SerializeField] private int totalObjectCount = 8;      // 필드 위에 놓일 총 작물 수
    [SerializeField] private int targetCount = 3;           // 정답 작물 개수 (3개)

    [Header("UI")]
    [SerializeField] private Image[] sampleImages;          // 정답 3개 보여줄 칸 (3개짜리 배열 권장)
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float timeLimit = 20f;

    // 내부 상태
    private int[] _targetIndices;                   // 정답 작물의 cropsCandidates 인덱스 3개
    private HashSet<int> _targetSet = new HashSet<int>();
    private int _currentCorrectCount;               // 가방에 담은 정답 수

    private List<CropsItemA> _spawnedItems = new List<CropsItemA>();

    private float _remainTime;
    private bool _isComplete;

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
        MissionType = MissionType.Crops_A;

        ClearObjects();

        _isComplete = false;
        _remainTime = timeLimit;
        _currentCorrectCount = 0;

        if (statusText != null)
        {
            statusText.text = "";
        }

        ChooseTargets();
        SpawnAll();
    }
    private void ClearObjects()
    {
        foreach (var item in _spawnedItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        _spawnedItems.Clear();
    }
    private void ChooseTargets()
    {
        _targetSet.Clear();

        int pickCount = Mathf.Min(targetCount, cropsCandidates.Length);

        List<int> indices = new List<int>();
        for (int i = 0; i < cropsCandidates.Length; i++)
            indices.Add(i);

        for (int i = 0; i < indices.Count; i++)
        {
            int swap = Random.Range(i, indices.Count);
            (indices[i], indices[swap]) = (indices[swap], indices[i]);
        }

        _targetIndices = new int[pickCount];
        for (int i = 0; i < pickCount; i++)
        {
            _targetIndices[i] = indices[i];
            _targetSet.Add(indices[i]);
        }

        if (sampleImages != null && sampleImages.Length > 0)
        {
            for (int i = 0; i < sampleImages.Length; i++)
            {
                if (i < _targetIndices.Length && sampleImages[i] != null)
                {
                    int idx = _targetIndices[i];
                    sampleImages[i].sprite = cropsCandidates[idx].sprite;
                    sampleImages[i].enabled = true;
                }
                else if (sampleImages[i] != null)
                {
                    sampleImages[i].enabled = false;
                }
            }
        }
    }

    private void SpawnAll()
    {
        if (cropsPrefab == null || parent == null || spawnGroup == null)
            return;

        if (spawnGroup.Length < totalObjectCount)
        {
            Debug.LogWarning("[CropsA] spawnGroup 개수가 totalObjectCount보다 적습니다.");
            totalObjectCount = Mathf.Min(totalObjectCount, spawnGroup.Length);
        }

        List<int> typeList = BuildSpawnTypeList();

        for (int i = 0; i < totalObjectCount; i++)
        {
            SpawnOneAtIndex(i, typeList[i]);
        }
    }
    private List<int> BuildSpawnTypeList()
    {
        List<int> result = new List<int>();

        // 정답 3개
        if (_targetIndices != null)
        {
            for (int i = 0; i < _targetIndices.Length; i++)
                result.Add(_targetIndices[i]);
        }

        // 오답
        List<int> wrongPool = new List<int>();
        for (int i = 0; i < cropsCandidates.Length; i++)
        {
            if (!_targetSet.Contains(i))
                wrongPool.Add(i);
        }

        while (result.Count < totalObjectCount)
        {
            if (wrongPool.Count == 0)
            {
                int anyIdx = Random.Range(0, cropsCandidates.Length);
                result.Add(anyIdx);
            }
            else
            {
                int rand = Random.Range(0, wrongPool.Count);
                int idx = wrongPool[rand];
                result.Add(idx);
            }
        }

        for (int i = 0; i < result.Count; i++)
        {
            int swap = Random.Range(i, result.Count);
            (result[i], result[swap]) = (result[swap], result[i]);
        }

        return result;
    }

    private void SpawnOneAtIndex(int index, int cropIndex)
    {
        if (index < 0 || index >= spawnGroup.Length) return;
        if (cropIndex < 0 || cropIndex >= cropsCandidates.Length) return;

        var inst = Instantiate(cropsPrefab, parent);
        var rt = inst.GetComponent<RectTransform>();

        if (rt != null)
        {
            rt.anchoredPosition = GetRandomPositionNearSpawn(index);
            rt.localScale = Vector3.one;
        }

        var data = cropsCandidates[cropIndex];
        inst.Init(this, data.id, data.sprite);
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

    public void OnCropPutInBag(CropsItemA item)
    {
        if (item == null) return;
        if (_isComplete) return;

        bool isCorrect = _targetSet.Contains(item.TypeId);

        if (isCorrect)
        {
            _currentCorrectCount++;
        }
        else
        {
            StartCoroutine(FailCoroutine());
        }

        _spawnedItems.Remove(item);

        if (_currentCorrectCount >= targetCount)
            StartCoroutine(CompleteCoroutine());
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
        if (_isComplete) yield break;
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
