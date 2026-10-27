using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MeatBItem : MonoBehaviour, IPointerClickHandler
{
    [Header("Refs")]
    [SerializeField] private RectTransform rect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image image;

    [Header("FX")]
    [SerializeField] private float floatUpDistance = 60f;
    [SerializeField] private float fadeDuration = 0.45f;
    [SerializeField] private float floatDuration = 0.45f;
    [SerializeField] private bool disableRaycastOnClear = true;

    private MeatB _mission;
    private bool _clearing;

    public RectTransform Rect
    {
        get
        {
            if (rect == null) rect = GetComponent<RectTransform>();
            return rect;
        }
    }

    public void Init(MeatB mission)
    {
        _mission = mission;

        if (rect == null)
            rect = GetComponent<RectTransform>();

        if (image == null)
            image = GetComponent<Image>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _clearing = false;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_mission == null) return;
        if (_mission.IsMissionFinished) return;
        if (_clearing) return;

        _clearing = true;

        if (disableRaycastOnClear && canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        StartCoroutine(ClearFxRoutine());
    }

    private IEnumerator ClearFxRoutine()
    {
        Vector2 start = rect.anchoredPosition;
        Vector2 end = start + Vector2.up * floatUpDistance;

        float t = 0f;
        float duration = Mathf.Max(0.01f, Mathf.Max(floatDuration, fadeDuration));

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float u = Mathf.Clamp01(t);

            float moveU = Mathf.Clamp01(Time.deltaTime <= 0f ? u : (u * (duration / Mathf.Max(0.01f, floatDuration))));
            rect.anchoredPosition = Vector2.Lerp(start, end, u);

            if (canvasGroup != null)
            {
                float fadeU = Mathf.Clamp01(u * (duration / Mathf.Max(0.01f, fadeDuration)));
                canvasGroup.alpha = 1f - fadeU;
            }

            yield return null;
        }

        if (_mission != null)
            _mission.OnPieceCleared(this);

        Destroy(gameObject);
    }
}
