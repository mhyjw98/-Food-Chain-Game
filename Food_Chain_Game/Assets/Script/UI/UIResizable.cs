using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIResizable : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    public RectTransform targetRect;
    public Vector2 minSize = new Vector2(400, 250);
    public Vector2 maxSize = new Vector2(1000, 700);

    private Vector2 _startSize;
    private Vector2 _startMousePos;
    public void OnBeginDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect,
            eventData.position,
            eventData.pressEventCamera,
            out _startMousePos
        );

        _startSize = targetRect.sizeDelta;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 currentMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect,
            eventData.position,
            eventData.pressEventCamera,
            out currentMousePos
        );

        Vector2 sizeDiff = currentMousePos - _startMousePos;
        Vector2 newSize = _startSize + new Vector2(sizeDiff.x, sizeDiff.y);

        newSize.x = Mathf.Clamp(newSize.x, minSize.x, maxSize.x);
        newSize.y = Mathf.Clamp(newSize.y, minSize.y, maxSize.y);

        targetRect.sizeDelta = newSize;
    }
}
