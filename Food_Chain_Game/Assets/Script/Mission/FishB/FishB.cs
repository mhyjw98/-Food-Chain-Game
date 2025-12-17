using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FishB : BaseMission
{
    [Header("Area")]
    [SerializeField] private RectTransform playPanel;
    [SerializeField] private RectTransform fishingArea;
    [SerializeField] private RectTransform controlArea;

    [Header("Refs")]
    [SerializeField] private FishSchoolB fishSchool;
    [SerializeField] private FishNetB fishNet;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float timeLimit = 20f;

    private float _remainTime;
    private bool _isComplete;

    public bool IsMissionFinished => _isComplete;
    public RectTransform FishingArea => fishingArea;
    public RectTransform ControlArea => controlArea;

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

        _isComplete = false;
        _remainTime = timeLimit;

        if (statusText != null)
            statusText.text = "";

        if (fishSchool != null)
            fishSchool.Init(this, fishingArea);

        if (fishNet != null)
            fishNet.Init(this, controlArea, fishingArea);
    }

    public void OnNetSuccess()
    {
        if (_isComplete) return;
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
