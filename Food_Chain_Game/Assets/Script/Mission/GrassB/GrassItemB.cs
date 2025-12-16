using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassItemB : MonoBehaviour
{
    [SerializeField] private int level = 1;

    [SerializeField] private RectTransform topRect;
    [SerializeField] private GameObject topVisual;
    [SerializeField] private GameObject bottomVisual;

    private float dropDistance = 250f;
    private float dropDuration = 1f;

    private float minHorizontalDistance = 30f;
    private float maxHorizontalDistance = 80f;
    private float arcHeight = 100f;
    private float minRotation = -180f;
    private float maxRotation = 180f;

    private GrassB _mission;
    private bool _isCut;
    private Vector2 _topStartPos;

    public int Level => level;
    public bool IsCut => _isCut;

    private void Awake()
    {
        if (topRect != null)
            _topStartPos = topRect.anchoredPosition;
    }

    public void Init(GrassB mission)
    {
        _mission = mission;
        _isCut = false;

        if (topRect != null)
        {
            topRect.anchoredPosition = _topStartPos;
            topRect.localRotation = Quaternion.identity;
        }

        if (topVisual != null) topVisual.SetActive(true);
        if (bottomVisual != null) bottomVisual.SetActive(true);

        StopAllCoroutines();
    }

    public void TryCut(Vector2 screenPos, Camera cam, int allowedLevel)
    {
        if (_isCut) return;
        if (_mission == null) return;

        if (level != allowedLevel) return;

        if (topRect == null) return;

        bool hit = RectTransformUtility.RectangleContainsScreenPoint(
            topRect,
            screenPos,
            cam
        );

        if (!hit) return;

        Cut();
    }

    private void Cut()
    {
        if (_isCut) return;
        _isCut = true;

        if (topRect != null && topVisual != null)
        {
            StartCoroutine(DropTopCoroutine());
        }
        else
        {
            _mission.OnBladeCut(this);
        }
    }

    private IEnumerator DropTopCoroutine()
    {
        if (topRect == null || topVisual == null)
        {
            _mission.OnBladeCut(this);
            yield break;
        }

        Vector2 start = topRect.anchoredPosition;

        float dir = Random.value < 0.5f ? -1f : 1f;
        float horiz = Random.Range(minHorizontalDistance, maxHorizontalDistance) * dir;

        Vector2 end = start + new Vector2(horiz, -dropDistance);

        float totalRot = Random.Range(minRotation, maxRotation);
        Quaternion rotStart = topRect.localRotation;
        Quaternion rotEnd = rotStart * Quaternion.Euler(0f, 0f, totalRot);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dropDuration;
            float alpha = Mathf.Clamp01(t);

            float x = Mathf.Lerp(start.x, end.x, alpha);
            float y = Mathf.Lerp(start.y, end.y, alpha);

            y += arcHeight * Mathf.Sin(Mathf.PI * alpha);

            topRect.anchoredPosition = new Vector2(x, y);

            topRect.localRotation = Quaternion.Slerp(rotStart, rotEnd, alpha);

            yield return null;
        }

        topVisual.SetActive(false);

        _mission.OnBladeCut(this);
    }
}
