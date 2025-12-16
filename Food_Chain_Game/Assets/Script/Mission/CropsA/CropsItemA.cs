using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CropsItemA : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform boundsRect;

    private RectTransform _rect;
    private Vector2 _startPos;
    private CropsA _mission;
    private int _typeId;

    public int TypeId => _typeId;

    private void Awake()
    {
        if (_rect == null)
            _rect = GetComponent<RectTransform>();
    }

    public void Init(CropsA mission, int typeId, Sprite sprite)
    {
        _mission = mission;
        _typeId = typeId;

        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        _startPos = _rect.anchoredPosition;

        if (icon != null)
            icon.sprite = sprite;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (boundsRect == null && _rect != null)
            boundsRect = _rect.parent as RectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_rect == null) return;
        _startPos = _rect.anchoredPosition;
        _rect.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_rect == null || boundsRect == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            boundsRect,
            eventData.position,
            canvas != null ? canvas.worldCamera : null,
            out localPoint
        );

        _rect.anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_rect == null || _mission == null) return;

        Camera cam = canvas != null ? canvas.worldCamera : null;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, _rect.position);

        bool inBag = _mission.IsInBagArea(screenPos);

        if (inBag)
        {
            _mission.OnCropPutInBag(this);
            Destroy(gameObject);
        }
        else
        {
            _rect.anchoredPosition = _startPos;
        }
    }
}
