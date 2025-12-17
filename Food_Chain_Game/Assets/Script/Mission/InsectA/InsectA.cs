using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InsectA : BaseMission
{
    [Header("UI")]
    [SerializeField] private RectTransform playPanel;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float timeLimit = 20f;

    [Header("Prefab")]
    [SerializeField] private InsectBugA insectPrefab;
    [SerializeField] private FlySwatter swatterPrefab;
    [SerializeField] private RectTransform[] landTargets;

    private InsectBugA _insect;
    private FlySwatter _swatter;

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

        ClearObjects();

        _isComplete = false;
        _remainTime = timeLimit;

        statusText.text = "";

        SpawnInsect();
        SpawnSwatter();
    }

    private void ClearObjects()
    {
        if (_insect != null)
            Destroy(_insect.gameObject);
        if (_swatter != null)
            Destroy(_swatter.gameObject);

        _insect = null;
        _swatter = null;
    }

    private void SpawnInsect()
    {
        if (insectPrefab == null || playPanel == null)
        {
            Debug.LogWarning("[InsectA] insectPrefab 또는 playPanel이 비어있습니다.");
            return;
        }

        _insect = Instantiate(insectPrefab, playPanel);
        _insect.Init(this, playPanel, landTargets);
    }

    private void SpawnSwatter()
    {
        if (swatterPrefab == null || playPanel == null)
        {
            Debug.LogWarning("[InsectA] swatterPrefab 또는 playPanel이 비어있습니다.");
            return;
        }

        _swatter = Instantiate(swatterPrefab, playPanel);
        _swatter.Init(this, playPanel);
    }

    public void OnSwatterHit(Vector2 swatterLocalPos)
    {
        if (_isComplete) return;
        if (_insect == null) return;

        bool caught = _insect.TryHit(swatterLocalPos);
        if (caught)
        {
            StartCoroutine(CompleteCoroutine());
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
