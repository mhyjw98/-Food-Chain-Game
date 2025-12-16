using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShovelFollow : MonoBehaviour
{
    public enum Mode { Dig, Cover }

    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform boundsRect;
    [SerializeField] private RectTransform rect;
    [SerializeField] private Vector2 restAnchoredPos;

    private bool _follow = true;
    private Mode _mode;

    private void Awake()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();
    }

    public void SetFollow(bool follow)
    {
        _follow = follow;
    }

    public void SetMode(Mode mode)
    {
        _mode = mode;
    }

    public void GoToRestPosition()
    {
        _follow = false;
        rect.anchoredPosition = restAnchoredPos;
    }

    private void Update()
    {
        if (!_follow) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            boundsRect,
            Input.mousePosition,
            canvas != null ? canvas.worldCamera : null,
            out localPoint
        );

        Rect r = boundsRect.rect;
        localPoint.x = Mathf.Clamp(localPoint.x, r.xMin, r.xMax);
        localPoint.y = Mathf.Clamp(localPoint.y, r.yMin, r.yMax);

        rect.anchoredPosition = localPoint;
    }
}
