using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GrassItem : MonoBehaviour, IEndDragHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform boundsRect;
    [SerializeField] private DragThrowItem _drag;

    private RectTransform _rect;
    private GrassA _mission;
    private GrassBag _bag;
    private int _typeId;
    private bool _cleared;
    public int TypeId => _typeId;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
        if (boundsRect == null)
            boundsRect = _rect.parent as RectTransform;
    }
    private void Update()
    {
        if (_cleared) return;
        if (_bag == null) return;

        if (_drag != null && _drag._isDragging)
            return;

        if (_bag.TryCatchItem(this))
        {
            _cleared = true;
        }
    }
    public void Init(GrassA mission, int typeId, Sprite sprite)
    {
        _mission = mission;
        _bag = mission != null ? mission.bag : null;
        _typeId = typeId;
        if (icon != null)
            icon.sprite = sprite;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_mission == null || _cleared) return;

        Vector2 screenPos = eventData.position;

        bool inBag = _mission.IsInBagArea(screenPos);

        if (inBag)
        {
            // Update의 가방 판정(TryCatchItem)이 같은 프레임에 한 번 더 처리하지 않도록 먼저 표시
            _cleared = true;
            _mission.OnGrassPutInBag(this);

            Destroy(gameObject);
        }
    }
}
