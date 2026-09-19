using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PoisonA : BaseMission
{
    [Header("UI References")]
    [SerializeField] private Button[] ingredientButtons;
    [SerializeField] private Image bottleImage;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Timer")]
    [SerializeField] private float timeLimit = 15f;
    [SerializeField] private float resultHoldSeconds = 2f;

    private bool[] _picked;
    private int _pickedCount;
    private float _remain;
    private bool _done;

    private void Awake()
    {
        if (ingredientButtons == null) return;

        for (int i = 0; i < ingredientButtons.Length; i++)
        {
            int idx = i;
            if (ingredientButtons[i] != null)
            {
                ingredientButtons[i].onClick.RemoveAllListeners();
                ingredientButtons[i].onClick.AddListener(() => OnIngredientClicked(idx));
            }
        }
    }

    public override void Begin()
    {
        base.Begin();

        _done = false;
        _remain = timeLimit;

        int n = ingredientButtons != null ? ingredientButtons.Length : 0;
        _picked = new bool[n];
        _pickedCount = 0;

        if (statusText != null) statusText.text = "";
        UpdateProgressUI();
        UpdateTimerUI();

        if (ingredientButtons != null)
        {
            for (int i = 0; i < ingredientButtons.Length; i++)
                if (ingredientButtons[i] != null)
                    ingredientButtons[i].interactable = true;
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

    private void OnIngredientClicked(int index)
    {
        if (_done) return;
        if (_picked == null) return;
        if (index < 0 || index >= _picked.Length) return;
        if (_picked[index]) return;

        _picked[index] = true;
        _pickedCount++;

        // Disable this ingredient button (functional "consumed")
        if (ingredientButtons != null && index < ingredientButtons.Length && ingredientButtons[index] != null)
            ingredientButtons[index].interactable = false;

        // Future: play animation / move sprite into bottle here
        // For now: purely functional state.

        UpdateProgressUI();

        if (_pickedCount >= _picked.Length && _picked.Length > 0)
        {
            StartCoroutine(CompleteCoroutine());
        }
    }

    private void UpdateProgressUI()
    {
        if (progressText != null)
        {
            int total = _picked != null ? _picked.Length : 0;
            progressText.text = $"{_pickedCount}/{total}";
        }

        // Optional: update bottle fill visual later
        // if (bottleImage != null) ...
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
            timerText.text = _remain.ToString("0.0");
    }

    private IEnumerator CompleteCoroutine()
    {
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
