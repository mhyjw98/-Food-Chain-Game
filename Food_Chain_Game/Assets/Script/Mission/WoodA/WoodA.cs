using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class WoodA : BaseMission
{
    [SerializeField] private WoodItemA woodPrefab;
    [SerializeField] private Transform[] woodSpawnPoints;
    [SerializeField] private RectTransform playPanel;
    [SerializeField] private Canvas canvas;

    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private float timeLimit = 30f;

    [SerializeField] private int _totalWood = 6;

    private float _remainTime;
    private bool _isComplete;

    private int _collectCount;    

    private List<WoodItemA> _spawnedItems = new List<WoodItemA>();
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
        if (Input.GetMouseButton(0))
        {
            SweepCutWood();
        }
    }

    public override void Begin()
    {
        base.Begin();

        ClearObject();

        _isComplete = false;
        _remainTime = timeLimit;
        _collectCount = 0;

        statusText.text = "";

        SpawnWoods();
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
    private void SweepCutWood()
    {
        if (_spawnedItems == null || _spawnedItems.Count == 0)
            return;

        Camera cam = canvas.worldCamera;

        Vector2 screenPos = Input.mousePosition;

        foreach (var item in _spawnedItems)
        {
            if (item == null) continue;
            item.TryCutBySweep(screenPos, cam);
        }
    }
    private void SpawnWoods()
    {
        if (woodPrefab == null || woodSpawnPoints == null)
            return;

        foreach (var sp in woodSpawnPoints)
        {
            if (sp == null) continue;

            var inst = Instantiate(woodPrefab, playPanel);
            var instRect = inst.GetComponent<RectTransform>();
            var spRect = sp as RectTransform;

            Vector3 localPos = playPanel.InverseTransformPoint(sp.position);
            instRect.localPosition = localPos;
            instRect.localRotation = sp.rotation;


            inst.Init(this, playPanel);
            _spawnedItems.Add(inst);
        }

        _totalWood = _spawnedItems.Count;
    }

    public void OnWoodCollected(WoodItemA item)
    {
        if (_isComplete) return;
        if (item == null) return;

        if (_spawnedItems.Contains(item))
            _spawnedItems.Remove(item);

        _collectCount++;
        UpdateProgressUI();

        if (_collectCount >= _totalWood)
        {
            StartCoroutine(CompleteCoroutine());
        }
    }
    private void UpdateProgressUI()
    {
        if (progressText != null)
        {
            progressText.text = $"나뭇가지 수집: {_collectCount} / {_totalWood}";
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
