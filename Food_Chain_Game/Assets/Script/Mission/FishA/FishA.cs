using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class FishA : BaseMission, IPointerDownHandler
{
    [Header("UI")]
    [SerializeField] private RectTransform playPanel;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float timeLimit = 20f;

    [Header("Rod")]
    [SerializeField] private FishRod rod;

    [Header("Fish Timing")]
    [SerializeField] private float biteWaitMin = 5f;
    [SerializeField] private float biteWaitMax = 10f;
    [SerializeField] private float reactWindowMin = 0.5f;
    [SerializeField] private float reactWindowMax = 1.0f;

    private float _remainTime;
    private bool _isComplete;

    private bool _canReact;
    private Coroutine _flowRoutine;
    private bool _failRoutineStarted;

    public bool IsMissionFinished => _isComplete;

    private void Update()
    {
        if (_isComplete) return;

        _remainTime -= Time.deltaTime;
        if (_remainTime < 0f) _remainTime = 0f;

        if (timerText != null)
            timerText.text = _remainTime.ToString("0.0");

        if (_remainTime <= 0f && !_failRoutineStarted)
        {
            _failRoutineStarted = true;
            StartCoroutine(FailCoroutine());
        }
    }

    public override void Begin()
    {
        base.Begin();
        MissionType = MissionType.Fish_A;

        if (_flowRoutine != null)
        {
            StopCoroutine(_flowRoutine);
            _flowRoutine = null;
        }

        _isComplete = false;
        _canReact = false;
        _failRoutineStarted = false;
        _remainTime = timeLimit;

        if (statusText != null)
            statusText.text = "";

        if (rod != null)
            rod.ResetVisual();

        _flowRoutine = StartCoroutine(FlowRoutine());
    }

    public override void End()
    {
        if (_flowRoutine != null)
        {
            StopCoroutine(_flowRoutine);
            _flowRoutine = null;
        }

        _canReact = false;
        base.End();
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_isComplete) return;
        if (!_canReact) return;

        _canReact = false;
        StartCoroutine(CompleteCoroutine());
    }

    private IEnumerator FlowRoutine()
    {
        while (!_isComplete && _remainTime > 0f)
        {
            float wait = Random.Range(biteWaitMin, biteWaitMax);
            while (wait > 0f && !_isComplete && _remainTime > 0f)
            {
                wait -= Time.deltaTime;
                yield return null;
            }

            if (_isComplete || _remainTime <= 0f)
                yield break;

            // AudioManager.Instance.PlayEffectSfx(AudioManager.EffectSfx.fishBite);

            if (rod != null)
                yield return rod.PlayBite();

            float react = Random.Range(reactWindowMin, reactWindowMax);
            _canReact = true;

            while (react > 0f && !_isComplete && _remainTime > 0f)
            {
                if (!_canReact)
                    yield break;

                react -= Time.deltaTime;
                yield return null;
            }

            _canReact = false;

            if (_isComplete || _remainTime <= 0f)
                yield break;

            if (rod != null)
                yield return rod.PlayMissRecover();
        }
    }

    private IEnumerator CompleteCoroutine()
    {
        if (_isComplete) yield break;
        _isComplete = true;

        if (_flowRoutine != null)
        {
            StopCoroutine(_flowRoutine);
            _flowRoutine = null;
        }

        // AudioManager.Instance.PlayEffectSfx(AudioManager.EffectSfx.fishPull);

        if (rod != null)
            yield return rod.PlayHookUpAndRevealFish();

        // AudioManager.Instance.PlayEffectSfx(AudioManager.EffectSfx.fishSuccess);

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
        _canReact = false;

        if (_flowRoutine != null)
        {
            StopCoroutine(_flowRoutine);
            _flowRoutine = null;
        }

        // AudioManager.Instance.PlayEffectSfx(AudioManager.EffectSfx.fishFail);

        if (rod != null)
            rod.ResetVisual();

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
