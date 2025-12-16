using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MeatA : BaseMission
{
    [Header("UI")]
    [SerializeField] private RectTransform playPanel;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float timeLimit = 20f;

    [Header("Meat")]
    [SerializeField] private RectTransform meatArea;

    [Header("Scanner")]
    [SerializeField] private MeatAScanner scannerPrefab;
    [SerializeField] private Canvas canvas;

    private MeatAScanner _scanner;
    private float _remainTime;
    private bool _isFinished;

    public bool IsMissionFinished => _isFinished;
    public RectTransform PlayPanel => playPanel;
    public RectTransform MeatArea => meatArea;

    private void Update()
    {
        if (_isFinished) return;

        _remainTime -= Time.deltaTime;
        if (_remainTime < 0f) _remainTime = 0f;

        if (timerText != null)
            timerText.text = _remainTime.ToString("0.0");

        if (_remainTime <= 0f && !_isFinished)
        {
            StartCoroutine(FailCoroutine());
        }
    }

    public override void Begin()
    {
        base.Begin();
        MissionType = MissionType.Meat_A;

        ClearObjects();

        _isFinished = false;
        _remainTime = timeLimit;

        if (statusText != null)
        {
            statusText.text = "";
        }

        SpawnScanner();
    }
    private void SpawnScanner()
    {
        if (scannerPrefab == null || playPanel == null || meatArea == null)
        {
            return;
        }

        _scanner = Instantiate(scannerPrefab, playPanel);
        _scanner.Init(this, playPanel, meatArea, canvas);
    }
    public override void End()
    {
        base.End();
        ClearObjects();
    }

    private void ClearObjects()
    {
        if (_scanner != null)
        {
            Destroy(_scanner.gameObject);
            _scanner = null;
        }
    }

    public void OnScanSuccess()
    {
        if (_isFinished) return;
        StartCoroutine(CompleteCoroutine());
    }

    private IEnumerator CompleteCoroutine()
    {
        _isFinished = true;

        if (statusText != null)
        {
            statusText.text = "성 공";
            statusText.color = Color.green;
            statusText.rectTransform.SetAsLastSibling();
        }

        yield return new WaitForSeconds(1.5f);
        Complete();
    }

    private IEnumerator FailCoroutine()
    {
        _isFinished = true;

        if (statusText != null)
        {
            statusText.text = "실 패";
            statusText.color = Color.red;
            statusText.rectTransform.SetAsLastSibling();
        }

        yield return new WaitForSeconds(1.5f);
        Fail();
    }
}
