using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class GrassB : BaseMission, IPointerDownHandler, IDragHandler
{
    [SerializeField] private GrassItemB[] grassStacks;

    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Canvas canvas;

    [SerializeField] private float timeLimit = 20f;

    private float _remainTime;
    private bool _isComplete;
    private int _currentLevel = 1;

    private int _totalBlades;
    private int _cutCount;

    private int _level1Total, _level2Total, _level3Total;
    private int _level1Cut, _level2Cut, _level3Cut;

    public bool IsComplete => _isComplete;
    public int CurrentLevel => _currentLevel;

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
        MissionType = MissionType.Grass_B;

        _isComplete = false;
        _remainTime = timeLimit;
        _cutCount = 0;
        _currentLevel = 1;

        statusText.text = "";

        _level1Total = _level2Total = _level3Total = 0;
        _level1Cut = _level2Cut = _level3Cut = 0;

        if (grassStacks != null)
        {
            foreach (var g in grassStacks)
            {
                if (g == null) continue;

                g.Init(this);

                switch (g.Level)
                {
                    case 1: _level1Total++; break;
                    case 2: _level2Total++; break;
                    case 3: _level3Total++; break;
                }
            }
        }
        _totalBlades = _level1Total + _level2Total + _level3Total;

        UpdateProgressUI();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TryCutAtScreenPosition(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        TryCutAtScreenPosition(eventData.position);
    }

    private void TryCutAtScreenPosition(Vector2 screenPos)
    {
        if (_isComplete) return;

        Camera cam = canvas != null ? canvas.worldCamera : null;       

        if (grassStacks != null)
        {
            foreach (var b in grassStacks)
            {
                if (b == null) continue;
                b.TryCut(screenPos, cam, _currentLevel);
            }
        }
    }    

    public void OnBladeCut(GrassItemB stack)
    {
        if (_isComplete) return;
        if (stack == null) return;

        _cutCount++;

        switch (stack.Level)
        {
            case 1: _level1Cut++; break;
            case 2: _level2Cut++; break;
            case 3: _level3Cut++; break;
        }

        CheckLevelProgress();
        CheckComplete();
    }

    private void CheckLevelProgress()
    {
        if (_currentLevel == 1 && _level1Total > 0 && _level1Cut >= _level1Total)
        {
            _currentLevel = 2;
        }
        else if (_currentLevel == 2 && _level2Total > 0 && _level2Cut >= _level2Total)
        {
            _currentLevel = 3;
        }
    }

    private void CheckComplete()
    {
        if (_cutCount >= _totalBlades && !_isComplete)
        {
            StartCoroutine(CompleteCoroutine());
        }
    }

    private void UpdateProgressUI()
    {
        if (progressText != null)
        {
            progressText.text = $"모든 잡초를 차례대로 베어 수집하세요.";
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
