using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragItem : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform boundsRect;

    private RectTransform _rect;
    private Vector2 _startPos;
    private Transform _startParent;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _startPos = _rect.anchoredPosition;
        _startParent = _rect.parent;

        _rect.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;

        RectTransform targetRect = boundsRect != null ? boundsRect : (RectTransform)_rect.parent;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect,
            eventData.position,
            canvas != null ? canvas.worldCamera : null,
            out localPoint
        );

        _rect.anchoredPosition = localPoint;

        if (boundsRect != null)
        {
            Rect r = boundsRect.rect;
            Vector2 pos = _rect.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x, r.xMin, r.xMax);
            pos.y = Mathf.Clamp(pos.y, r.yMin, r.yMax);
            _rect.anchoredPosition = pos;
        }
    }

    public RectTransform Rect => _rect;
}
