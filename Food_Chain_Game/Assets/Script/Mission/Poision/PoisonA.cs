using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 전갈 독 수집: 색이 다른 병을 눌러 가운데 잔에 붓는다.
// 오른쪽 위 가이드 순서대로 모두 부으면 성공. 순서가 틀려도 병을 전부 부은 뒤에 실패로 판정한다
public class PoisonA : BaseMission
{
    [Header("UI References")]
    [SerializeField] private Button[] bottleButtons;     // 병(누르는 버튼)
    [SerializeField] private Image[] bottleLiquids;      // 병 안의 액체(Filled). bottleButtons와 같은 순서
    [SerializeField] private Image[] cupLayers;          // 잔 안의 층(Filled, 아래부터). 부은 순서대로 채운다
    [SerializeField] private Image[] guideSlots;         // 가이드 순서 표시(왼쪽부터)
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Liquid")]
    [SerializeField] private Color[] liquidColors =      // 병 색. bottleButtons와 같은 순서
    {
        new Color(0.93f, 0.11f, 0.14f),
        new Color(0.25f, 0.29f, 0.80f),
        new Color(0.13f, 0.69f, 0.30f),
    };
    [SerializeField] private float pourSeconds = 0.6f;

    [Header("Timer")]
    [SerializeField] private float timeLimit = 20f;
    [SerializeField] private float resultHoldSeconds = 2f;

    private readonly List<int> _targetOrder = new List<int>();
    private readonly List<int> _pouredOrder = new List<int>();
    private float _remain;
    private bool _done;
    private bool _pouring;

    private int BottleCount => bottleButtons != null ? bottleButtons.Length : 0;

    private void Awake()
    {
        if (bottleButtons == null) return;

        for (int i = 0; i < bottleButtons.Length; i++)
        {
            int idx = i;
            if (bottleButtons[i] != null)
            {
                bottleButtons[i].onClick.RemoveAllListeners();
                bottleButtons[i].onClick.AddListener(() => OnBottleClicked(idx));
            }
        }
    }

    public override void Begin()
    {
        base.Begin();

        _done = false;
        _pouring = false;
        _remain = timeLimit;
        _pouredOrder.Clear();

        ShuffleTargetOrder();
        ResetVisuals();

        if (statusText != null) statusText.text = "";
        UpdateProgressUI();
        UpdateTimerUI();
    }

    private void ShuffleTargetOrder()
    {
        _targetOrder.Clear();
        for (int i = 0; i < BottleCount; i++)
            _targetOrder.Add(i);

        for (int i = _targetOrder.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_targetOrder[i], _targetOrder[j]) = (_targetOrder[j], _targetOrder[i]);
        }
    }

    private void ResetVisuals()
    {
        for (int i = 0; i < BottleCount; i++)
        {
            if (bottleButtons[i] != null)
                bottleButtons[i].interactable = true;

            Image liquid = GetAt(bottleLiquids, i);
            if (liquid != null)
            {
                liquid.color = GetColor(i);
                liquid.fillAmount = 1f;
            }
        }

        if (cupLayers != null)
        {
            for (int i = 0; i < cupLayers.Length; i++)
                if (cupLayers[i] != null)
                    cupLayers[i].fillAmount = 0f;
        }

        if (guideSlots != null)
        {
            for (int i = 0; i < guideSlots.Length; i++)
            {
                if (guideSlots[i] == null) continue;
                bool has = i < _targetOrder.Count;
                guideSlots[i].gameObject.SetActive(has);
                if (has) guideSlots[i].color = GetColor(_targetOrder[i]);
            }
        }
    }

    private void Update()
    {
        if (_done) return;

        _remain -= Time.deltaTime;
        if (_remain < 0f) _remain = 0f;

        UpdateTimerUI();

        if (_remain <= 0f)
        {
            StartCoroutine(FailCoroutine());
        }
    }

    private void OnBottleClicked(int index)
    {
        if (_done || _pouring) return;
        if (index < 0 || index >= BottleCount) return;
        if (_pouredOrder.Contains(index)) return;

        StartCoroutine(PourCoroutine(index));
    }

    private IEnumerator PourCoroutine(int index)
    {
        _pouring = true;

        int layerIndex = _pouredOrder.Count;
        _pouredOrder.Add(index);

        if (bottleButtons[index] != null)
            bottleButtons[index].interactable = false;

        UpdateProgressUI();

        Image liquid = GetAt(bottleLiquids, index);
        Image layer = GetAt(cupLayers, layerIndex);
        if (layer != null) layer.color = GetColor(index);

        float t = 0f;
        while (t < pourSeconds)
        {
            if (_done) yield break;   // 붓는 도중 시간 초과

            t += Time.deltaTime;
            float k = pourSeconds > 0f ? Mathf.Clamp01(t / pourSeconds) : 1f;
            if (liquid != null) liquid.fillAmount = 1f - k;
            if (layer != null) layer.fillAmount = k;
            yield return null;
        }

        if (liquid != null) liquid.fillAmount = 0f;
        if (layer != null) layer.fillAmount = 1f;

        _pouring = false;
        if (_done) yield break;

        if (_pouredOrder.Count >= BottleCount)
        {
            if (IsOrderCorrect())
                StartCoroutine(CompleteCoroutine());
            else
                StartCoroutine(FailCoroutine());
        }
    }

    private bool IsOrderCorrect()
    {
        if (_pouredOrder.Count != _targetOrder.Count) return false;

        for (int i = 0; i < _targetOrder.Count; i++)
            if (_pouredOrder[i] != _targetOrder[i]) return false;

        return true;
    }

    private void UpdateProgressUI()
    {
        if (progressText != null)
            progressText.text = $"{_pouredOrder.Count}/{BottleCount}";
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
            timerText.text = _remain.ToString("0.0");
    }

    private Color GetColor(int bottleIndex)
    {
        return liquidColors != null && bottleIndex >= 0 && bottleIndex < liquidColors.Length
            ? liquidColors[bottleIndex]
            : Color.white;
    }

    private static Image GetAt(Image[] array, int index)
    {
        return array != null && index >= 0 && index < array.Length ? array[index] : null;
    }

    private IEnumerator CompleteCoroutine()
    {
        if (_done) yield break;
        _done = true;

        if (statusText != null)
        {
            statusText.text = "성 공";
            statusText.rectTransform.SetAsLastSibling();
        }

        yield return new WaitForSeconds(resultHoldSeconds);
        Complete();
    }

    private IEnumerator FailCoroutine()
    {
        if (_done) yield break;
        _done = true;

        if (statusText != null)
        {
            statusText.text = "실 패";
            statusText.rectTransform.SetAsLastSibling();
        }

        yield return new WaitForSeconds(resultHoldSeconds);
        Fail();
    }
}
