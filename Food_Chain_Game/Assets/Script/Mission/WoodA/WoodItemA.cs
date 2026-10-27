using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class WoodItemA : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
{
    [SerializeField] private float dropDuration = 1f;
    [SerializeField] private float minRotation = -35f;
    [SerializeField] private float maxRotation = 35f;
    [SerializeField]
    private AnimationCurve dropCurve
        = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField] private float floorOffsetMin = 20f;
    [SerializeField] private float floorOffsetMax = 150f;
    [SerializeField] private float horizontalJitter = 20f;

    private RectTransform _rect;
    private RectTransform _playPanel;
    private WoodA _mission;
    private bool _cut;
    public void Init(WoodA mission, RectTransform playPanel)
    {
        _mission = mission;
        _playPanel = playPanel;

        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        _cut = false;
    }

    private void Awake()
    {
        if (_rect == null)
            _rect = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TryCut();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        TryCut();
    }
    public void OnDrag(PointerEventData eventData)
    {
        TryCut();
    }

    public void TryCutBySweep(Vector2 screenPos, Camera cam)
    {
        if (_cut) return;
        if (_rect == null) return;

        bool inside = RectTransformUtility.RectangleContainsScreenPoint(
            _rect,
            screenPos,
            cam
        );

        if (!inside) return;

        TryCut();
    }
    private void TryCut()
    {
        if (_cut) return;
        _cut = true;

        StartCoroutine(CutAndCollectCoroutine());
    }

    private IEnumerator CutAndCollectCoroutine()
    {
        Vector2 startPos = _rect.anchoredPosition;

        float floorY = startPos.y - 100f;

        if (_playPanel != null)
        {
            Rect r = _playPanel.rect;
            float baseFloor = r.yMin;

            float offset = Random.Range(floorOffsetMin, floorOffsetMax);
            floorY = baseFloor + offset;
        }

        Vector2 endPos = new Vector2(
            startPos.x + Random.Range(-horizontalJitter, horizontalJitter),
            floorY
        );

        Quaternion rotStart = _rect.localRotation;
        float targetZ = Random.Range(minRotation, maxRotation);
        Quaternion rotEnd = Quaternion.Euler(0f, 0f, targetZ);

        float t = 0f;
        float duration = Mathf.Max(0.01f, dropDuration);

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float a = Mathf.Clamp01(t);

            float eased = dropCurve != null
                ? dropCurve.Evaluate(a)
                : (1f - Mathf.Pow(1f - a, 2f));

            _rect.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
            _rect.localRotation = Quaternion.Slerp(rotStart, rotEnd, eased);

            yield return null;
        }

        _rect.anchoredPosition = endPos;
        _rect.localRotation = rotEnd;

        if (_mission != null)
            _mission.OnWoodCollected(this);
    }
}

