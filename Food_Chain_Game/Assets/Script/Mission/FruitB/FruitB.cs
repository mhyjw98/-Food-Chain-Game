using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FruitB : BaseMission
{
    [Header("Panel & Areas")]
    [SerializeField] private RectTransform playPanel;
    [SerializeField] private RectTransform controlArea;

    [Header("Fruits")]
    [SerializeField] private FruitBItem[] fruits;

    [Header("Stone")]
    [SerializeField] private FruitBStone stonePrefab;
    [SerializeField] private Canvas canvas;
    [SerializeField] private float stoneLifeTime = 1.5f;
    [SerializeField] private float stoneRespawnDelay = 1f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private float timeLimit = 30f;

    private float _remainTime;
    private bool _isComplete;

    private int _fruitTotal;
    private int _fruitHitCount;

    private FruitBStone _currentStone;
    private bool _stoneRespawnScheduled;

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
        MissionType = MissionType.Fruit_B;

        _isComplete = false;
        _remainTime = timeLimit;
        _fruitHitCount = 0;
        _stoneRespawnScheduled = false;

        if (statusText != null)
            statusText.text = "";

        _fruitTotal = 0;
        if (fruits != null)
        {
            foreach (var f in fruits)
            {
                if (f == null) continue;
                f.Init(this);
                _fruitTotal++;
            }
        }

        SpawnStone();
        UpdateProgressUI();
    }

    private void SpawnStone()
    {
        if (_isComplete) return;
        if (stonePrefab == null || playPanel == null) return;

        var inst = Instantiate(stonePrefab, playPanel);
        _currentStone = inst;

        inst.Init(
            mission: this,
            canvas: canvas != null ? canvas : playPanel.GetComponentInParent<Canvas>(),
            moveAreaRect: controlArea,
            boundsRect: playPanel,
            lifeTimeAfterThrow: stoneLifeTime
        );
    }

    public void OnStoneExpired(FruitBStone stone)
    {
        if (_isComplete) return;
        if (stone != _currentStone) return; // 이미 다른 돌이 생성된 경우

        if (_stoneRespawnScheduled) return;
        _stoneRespawnScheduled = true;

        StartCoroutine(RespawnStoneCoroutine());
    }

    private IEnumerator RespawnStoneCoroutine()
    {
        yield return new WaitForSeconds(stoneRespawnDelay);

        _stoneRespawnScheduled = false;
        SpawnStone();
    }

    public void OnFruitHit(FruitBItem fruit)
    {
        if (_isComplete) return;
        if (fruit == null) return;

        _fruitHitCount++;
        UpdateProgressUI();

        if (_fruitHitCount >= _fruitTotal)
        {
            StartCoroutine(CompleteCoroutine());
        }
    }

    private void UpdateProgressUI()
    {
        if (progressText != null)
            progressText.text = $"열매 맞추기: {_fruitHitCount} / {_fruitTotal}";
    }

    public void TryHitFruits(RectTransform stoneRect)
    {
        if (_isComplete) return;
        if (stoneRect == null) return;
        if (fruits == null || fruits.Length == 0) return;

        Canvas cvs = canvas != null ? canvas : playPanel.GetComponentInParent<Canvas>();
        Camera cam = cvs != null ? cvs.worldCamera : null;

        Vector2 stoneScreenPos = RectTransformUtility.WorldToScreenPoint(
            cam,
            stoneRect.position
        );

        foreach (var f in fruits)
        {
            if (f == null) continue;
            f.TryHit(stoneScreenPos, cam);
        }
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
