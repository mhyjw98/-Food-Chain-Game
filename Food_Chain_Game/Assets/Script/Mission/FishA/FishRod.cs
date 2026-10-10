using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FishRod : MonoBehaviour
{
    [Header("Rod")]
    [SerializeField] private RectTransform rodRoot;
    [SerializeField] private RectTransform rodTip;

    [Header("Line")]
    [SerializeField] private RectTransform lineContainer;
    [SerializeField] private RectTransform waterEndPoint;
    [SerializeField] private int segmentCount = 30;
    [SerializeField] private float lineThickness = 6f;
    [SerializeField] private bool autoCreateSegments = true;

    [Header("Fish")]
    [SerializeField] private RectTransform fishRect;
    [SerializeField] private CanvasGroup fishCg;

    [Header("Bite Motion")]
    [SerializeField] private float bendAngle = 18f;
    [SerializeField] private float bendTime = 0.12f;

    [Header("Hook Up Motion")]
    [SerializeField] private float hookUpAngle = -45f;
    [SerializeField] private float hookUpTime = 0.10f;
    [SerializeField] private Vector2 rodLiftOffset = new Vector2(0f, 35f);

    [Header("Fish Reveal")]
    [SerializeField] private float fishRevealTime = 0.12f;
    [SerializeField] private Vector2 fishRevealOffset = new Vector2(0f, 70f);

    [Header("Recover")]
    [SerializeField] private float recoverTime = 0.18f;

    [Header("Line Curve")]
    [SerializeField] private float idleSag = 90f;
    [SerializeField] private float biteSag = 130f;
    [SerializeField] private float hookSag = 35f;
    [SerializeField] private float controlForward = 140f;

    private RectTransform[] _lineSegments;

    private Quaternion _rodStartRot;
    private Vector2 _rodStartPos;

    private Vector2 _fishStartPos;
    private Vector2 _endPos;
    private float _sag;

    private bool _fishVisible;

    void Awake()
    {
        if (rodRoot != null)
        {
            _rodStartRot = rodRoot.localRotation;
            _rodStartPos = rodRoot.anchoredPosition;
        }

        if (fishRect != null)
        {
            if (fishCg == null)
            {
                fishCg = fishRect.GetComponent<CanvasGroup>();
                if (fishCg == null)
                    fishCg = fishRect.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (autoCreateSegments)
            CreateSegmentsIfNeeded();

        CacheStartPositions();
        ResetVisual();
    }

    private void LateUpdate()
    {
        UpdateLineCurve(_endPos, _sag);
    }

    private void CacheStartPositions()
    {
        if (lineContainer == null) return;

        if (waterEndPoint != null)
            _fishStartPos = WorldToAnchored(lineContainer, waterEndPoint.position);
        else if (fishRect != null)
            _fishStartPos = fishRect.anchoredPosition;
        else
            _fishStartPos = Vector2.zero;

        _endPos = _fishStartPos;
        _sag = idleSag;
        _fishVisible = false;
    }

    private void CreateSegmentsIfNeeded()
    {
        if (lineContainer == null) return;

        if (_lineSegments != null && _lineSegments.Length == segmentCount)
            return;

        for (int i = lineContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(lineContainer.GetChild(i).gameObject);
        }

        _lineSegments = new RectTransform[segmentCount];

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject go = new GameObject($"LineSeg_{i:00}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(lineContainer, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;

            Image img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = false;

            rt.sizeDelta = new Vector2(10f, lineThickness);

            _lineSegments[i] = rt;
        }
    }

    public void ResetVisual()
    {
        if (rodRoot != null)
        {
            rodRoot.localRotation = _rodStartRot;
            rodRoot.anchoredPosition = _rodStartPos;
        }

        _endPos = _fishStartPos;
        _sag = idleSag;
        _fishVisible = false;

        if (fishRect != null)
        {
            fishRect.anchoredPosition = _endPos;
            fishRect.gameObject.SetActive(false);
        }
        if (fishCg != null)
            fishCg.alpha = 0f;

        UpdateLineCurve(_endPos, _sag);
    }

    public IEnumerator PlayBite()
    {
        float t = 0f;
        float dur = Mathf.Max(0.01f, bendTime);

        Quaternion fromRot = rodRoot != null ? rodRoot.localRotation : Quaternion.identity;
        Quaternion toRot = _rodStartRot * Quaternion.Euler(0f, 0f, bendAngle);

        Vector2 endPos = _fishStartPos;

        while (t < dur)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);

            if (rodRoot != null)
                rodRoot.localRotation = Quaternion.Slerp(fromRot, toRot, u);

            _endPos = endPos;
            _sag = Mathf.Lerp(idleSag, biteSag, u);

            yield return null;
        }

        _endPos = endPos;
        _sag = biteSag;
    }

    public IEnumerator PlayMissRecover()
    {
        float t = 0f;
        float dur = Mathf.Max(0.01f, recoverTime);

        Quaternion fromRot = rodRoot != null ? rodRoot.localRotation : Quaternion.identity;
        Vector2 fromPos = rodRoot != null ? rodRoot.anchoredPosition : Vector2.zero;

        while (t < dur)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);

            if (rodRoot != null)
            {
                rodRoot.localRotation = Quaternion.Slerp(fromRot, _rodStartRot, u);
                rodRoot.anchoredPosition = Vector2.Lerp(fromPos, _rodStartPos, u);
            }

            _endPos = _fishStartPos;
            _sag = Mathf.Lerp(biteSag, idleSag, u);

            yield return null;
        }

        ResetVisual();
    }
    public IEnumerator PlayHookUpAndRevealFish()
    {
        float t = 0f;
        float dur = Mathf.Max(0.01f, hookUpTime);

        Quaternion fromRot = rodRoot != null ? rodRoot.localRotation : Quaternion.identity;
        Vector2 fromPos = rodRoot != null ? rodRoot.anchoredPosition : Vector2.zero;

        Quaternion toRot = _rodStartRot * Quaternion.Euler(0f, 0f, hookUpAngle);
        Vector2 toPos = _rodStartPos + rodLiftOffset;

        Vector2 fishFrom = _fishStartPos;
        Vector2 fishTo = _fishStartPos + fishRevealOffset;

        _fishVisible = true;

        if (fishRect != null && fishCg != null)
        {
            fishRect.gameObject.SetActive(true);
            fishRect.anchoredPosition = fishFrom;
            fishCg.alpha = 0f;
        }

        while (t < dur)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);

            if (rodRoot != null)
            {
                rodRoot.localRotation = Quaternion.Slerp(fromRot, toRot, u);
                rodRoot.anchoredPosition = Vector2.Lerp(fromPos, toPos, u);
            }

            Vector2 fishPos = Vector2.Lerp(fishFrom, fishTo, u);

            _endPos = fishPos;
            _sag = Mathf.Lerp(biteSag, hookSag, u);

            if (fishRect != null)
                fishRect.anchoredPosition = fishPos;

            yield return null;
        }

        float tf = 0f;
        float durFish = Mathf.Max(0.01f, fishRevealTime);

        while (tf < durFish)
        {
            tf += Time.deltaTime;
            float u = Mathf.Clamp01(tf / durFish);

            if (fishCg != null)
                fishCg.alpha = Mathf.Lerp(0f, 1f, u);

            yield return null;
        }

        if (fishCg != null)
            fishCg.alpha = 1f;

        _endPos = fishTo;
        _sag = hookSag;
    }

    private void UpdateLineCurve(Vector2 endPosAnchored, float sagAmount)
    {
        if (rodTip == null) return;
        if (lineContainer == null) return;
        if (_lineSegments == null || _lineSegments.Length == 0) return;

        Vector2 p0 = WorldToAnchored(lineContainer, rodTip.position);
        Vector2 p3 = endPosAnchored;

        Vector2 dir = p3 - p0;
        float len = Mathf.Max(1f, dir.magnitude);
        Vector2 dirN = dir / len;

        Vector2 c1 = p0 + dirN * controlForward;
        Vector2 c2 = p3 - dirN * (controlForward * 0.35f);

        c1 += Vector2.down * sagAmount * 0.35f;
        c2 += Vector2.down * sagAmount;

        float step = 1f / (_lineSegments.Length + 1f);
        float segLen = len / (_lineSegments.Length + 1f);

        Vector2 prev = p0;

        for (int i = 0; i < _lineSegments.Length; i++)
        {
            float t = (i + 1) * step;
            Vector2 pos = Bezier(p0, c1, c2, p3, t);

            Vector2 d = pos - prev;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;

            RectTransform seg = _lineSegments[i];
            seg.anchoredPosition = pos;
            seg.localRotation = Quaternion.Euler(0f, 0f, ang);
            seg.sizeDelta = new Vector2(segLen, lineThickness);

            prev = pos;
        }

        if (_fishVisible && fishRect != null && fishRect.gameObject.activeSelf)
            fishRect.anchoredPosition = p3;
    }

    private static Vector2 Bezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;

        Vector2 p = (uu * u) * p0;
        p += 3f * (uu * t) * p1;
        p += 3f * (u * tt) * p2;
        p += (tt * t) * p3;
        return p;
    }

    private static Vector2 WorldToAnchored(RectTransform target, Vector3 worldPos)
    {
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            target,
            RectTransformUtility.WorldToScreenPoint(null, worldPos),
            null,
            out local
        );
        return local;
    }
}
