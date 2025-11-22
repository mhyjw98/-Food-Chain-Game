using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpeechBubble : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public CanvasGroup canvasGroup;

    public float showDuration = 3f;
    public float fadeTime = 0.25f;

    private Coroutine showRoutine;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        HideImmediate();
    }

    public void Show(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (messageText != null)
            messageText.text = message;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (showRoutine != null)
            StopCoroutine(showRoutine);

        showRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        yield return new WaitForSeconds(showDuration);

        if (canvasGroup != null)
        {
            float t = 0f;
            float startAlpha = canvasGroup.alpha;

            while (t < fadeTime)
            {
                t += Time.deltaTime;
                float normalized = t / fadeTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, normalized);
                yield return null;
            }

            canvasGroup.alpha = 0f;
        }

        showRoutine = null;
    }

    public void HideImmediate()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }
}
