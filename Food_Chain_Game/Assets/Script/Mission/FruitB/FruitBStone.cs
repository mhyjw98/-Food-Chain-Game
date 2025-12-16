using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitBStone : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform moveAreaRect;
    [SerializeField] private RectTransform boundsRect;
    [SerializeField] private RectTransform rect;
    [SerializeField] private DragThrowItem _drag;

    [Header("Settings")]
    [SerializeField] private float lifeTimeAfterThrow = 3f;

    private FruitB _mission;
    private bool _thrown;
    private float _lifeTimer;

    public RectTransform Rect => rect;


    public void Init(FruitB mission, Canvas canvas, RectTransform moveAreaRect, RectTransform boundsRect, float lifeTimeAfterThrow)
    {
        _mission = mission;
        this.canvas = canvas != null ? canvas : GetComponentInParent<Canvas>();
        this.moveAreaRect = moveAreaRect;
        this.boundsRect = boundsRect;
        this.lifeTimeAfterThrow = lifeTimeAfterThrow;

        if (rect == null)
            rect = GetComponent<RectTransform>();

        if (_drag == null)
            _drag = GetComponent<DragThrowItem>();

        _thrown = false;
        _lifeTimer = 0f;

        if (_drag != null)
            _drag.OnReleased = OnReleasedFromDrag;

        FollowCursorInBounds();
    }

    private void Update()
    {
        if (_mission == null || rect == null)
            return;

        if (!_thrown)
        {
            FollowCursorInBounds();
        }
        else
        {
            _lifeTimer += Time.deltaTime;

            _mission.TryHitFruits(rect);

            if (_lifeTimer >= lifeTimeAfterThrow)
            {
                _mission.OnStoneExpired(this);
                Destroy(gameObject);
            }
        }
    }

    private void FollowCursorInBounds()
    {
        if (canvas == null || moveAreaRect == null || boundsRect == null)
            return;

        Vector2 localInMove;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            moveAreaRect,
            Input.mousePosition,
            canvas != null ? canvas.worldCamera : null,
            out localInMove
        );

        Rect r = moveAreaRect.rect;
        localInMove.x = Mathf.Clamp(localInMove.x, r.xMin, r.xMax);
        localInMove.y = Mathf.Clamp(localInMove.y, r.yMin, r.yMax);

        Vector3 worldPos = moveAreaRect.TransformPoint(localInMove);

        Vector2 localInBounds;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            boundsRect,
            RectTransformUtility.WorldToScreenPoint(
                canvas != null ? canvas.worldCamera : null,
                worldPos
            ),
            canvas != null ? canvas.worldCamera : null,
            out localInBounds
        );

        rect.anchoredPosition = localInBounds;
    }

    private void OnReleasedFromDrag(DragThrowItem drag)
    {       
        _thrown = true;
        _lifeTimer = 0f;
    }
}
