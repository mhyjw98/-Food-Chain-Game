using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.PlayerLoop;

[RequireComponent(typeof(RectTransform))]
public class DragThrowItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private float throwPower = 150f;
    [SerializeField] private float maxThrowSpeed = 500f;
    [SerializeField] private float minThrowSpeed = 10f;

    [SerializeField] private float gravity = -500f;
    [SerializeField] private float damping = 1f;

    [SerializeField] private float landingMaxHeight = 400f;

    [SerializeField] private RectTransform boundsRect;
    public RectTransform _rect;
    private Canvas _canvas;

    public bool _isDragging;
    private bool _isFlying;
    private Vector2 _velocity;
    private Vector2 _lastPos;
    private float _landingY;
    private bool _hasLandingY;

    public System.Action<DragThrowItem> OnReleased;
    private void Awake()
    {
        if(_rect == null)
            _rect = GetComponent<RectTransform>();

        if(_canvas == null)
            _canvas = GetComponentInParent<Canvas>();

        if (boundsRect == null)
            boundsRect = _rect.parent as RectTransform;
    }
    private void Update()
    {
        if (!_isFlying)
            return;

        _velocity.y += gravity * Time.deltaTime;
        _velocity = Vector2.Lerp(_velocity, Vector2.zero, damping * Time.deltaTime);

        Vector2 pos = _rect.anchoredPosition;
        pos += _velocity * Time.deltaTime;

        Rect r = boundsRect.rect;
        float xMin = r.xMin;
        float xMax = r.xMax;
        float yMin = r.yMin;
        float yMax = r.yMax;

        if (pos.x < xMin) { pos.x = xMin; _velocity.x = 0; }
        if (pos.x > xMax) { pos.x = xMax; _velocity.x = 0; }

        if (pos.y > yMax)
        {
            pos.y = yMax;

            if (_velocity.y > 0)
                _velocity.y = 0;
        }

        float targetLandingY = _hasLandingY
            ? Mathf.Max(_landingY, yMin)
            : yMin;

        if (pos.y <= targetLandingY)
        {
            pos.y = targetLandingY;
            _velocity = Vector2.zero;
            _isFlying = false;
            _hasLandingY = false;
        }

        _rect.anchoredPosition = pos;
    }



    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _isFlying = false;
        _velocity = Vector2.zero;
        _lastPos = _rect.anchoredPosition;
        _hasLandingY = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            boundsRect,
            eventData.position,
            _canvas != null ? _canvas.worldCamera : null,
            out localPoint
        );

        Rect r = boundsRect.rect;
        localPoint.x = Mathf.Clamp(localPoint.x, r.xMin, r.xMax);
        localPoint.y = Mathf.Clamp(localPoint.y, r.yMin, r.yMax);

        _rect.anchoredPosition = localPoint;

        Vector2 newPos = _rect.anchoredPosition;
        _velocity = (newPos - _lastPos) / Mathf.Max(Time.deltaTime, 0.0001f);
        _lastPos = newPos;

    }


    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;

        float speed = _velocity.magnitude;

        if (speed < minThrowSpeed)
        {
            _isFlying = false;
            _hasLandingY = false;
            _velocity = Vector2.zero;

            ClampToBounds();
            return;
        }

        _isFlying = true;

        speed = Mathf.Min(speed * throwPower, maxThrowSpeed);
        _velocity = _velocity.normalized * speed;

        Rect r = boundsRect.rect;
        float yMin = r.yMin;
        float currentY = _rect.anchoredPosition.y;

        float maxHeight = Mathf.Min(landingMaxHeight, currentY - yMin);
        if (maxHeight <= 0f)
        {
            _landingY = yMin;
            _hasLandingY = true;
            return;
        }

        float randomHeight = Random.Range(0f, maxHeight);
        _landingY = yMin + randomHeight;
        _hasLandingY = true;

        OnReleased?.Invoke(this);
    }
    private void ClampToBounds()
    {
        Rect r = boundsRect.rect;
        float xMin = r.xMin;
        float xMax = r.xMax;
        float yMin = r.yMin;
        float yMax = r.yMax;

        Vector2 pos = _rect.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, xMin, xMax);
        pos.y = Mathf.Clamp(pos.y, yMin, yMax);

        _rect.anchoredPosition = pos;
    }
    public RectTransform Rect => _rect;
}
