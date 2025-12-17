using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InsectB : BaseMission
{
    [Header("Play Area")]
    [SerializeField] private RectTransform playPanel;
    [SerializeField] private RectTransform bugSpawnPoint;

    [Header("Object")]
    [SerializeField] private InsectBugB bugPrefab;
    [SerializeField] private InsectBushB[] bushes;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float timeLimit = 20f;

    private InsectBugB _bugInstance;
    private float _remainTime;
    private bool _isComplete;

    public bool IsMissionFinished => _isComplete;

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

        ClearObject();

        _isComplete = false;
        _remainTime = timeLimit;

        statusText.text = "";

        SpawnBug();
    }

    private void ClearObject()
    {
        if (_bugInstance != null)
        {
            Destroy(_bugInstance.gameObject);
            _bugInstance = null;
        }

        foreach(var bush in bushes)
        {
            bush.initBush();
        }
    }

    private void SpawnBug()
    {
        if (bugPrefab == null || playPanel == null || bushes == null || bushes.Length == 0)
        {
            Debug.LogWarning("[InsectB] 세팅이 부족합니다.");
            return;
        }

        _bugInstance = Instantiate(bugPrefab, bugSpawnPoint);

        RectTransform[] bushPoints = new RectTransform[bushes.Length];
        for (int i = 0; i < bushes.Length; i++)
        {
            if (bushes[i] != null)
                bushPoints[i] = bushes[i].GetComponent<RectTransform>();
        }

        _bugInstance.Init(this, playPanel, bushPoints, bushes);
    }
    public void OnBugCaught(InsectBugB bug)
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
