using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDraggable : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    public bool isLock = false;
    public TextMeshProUGUI lockText;

    public ScrollRect scrollRect;
    public RectTransform scrollArea;

    Vector2 _moveBegin;
    Vector2 _startingPoint;
    RectTransform _rectTransform;
    Canvas _canvas;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
    }

    void Update()
    {
        if (Input.mouseScrollDelta.y != 0)
        {
            float scrollDelta = Input.mouseScrollDelta.y * 0.15f;
            scrollRect.verticalNormalizedPosition += scrollDelta;
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLock) return;

        _moveBegin = eventData.position;
        _startingPoint = transform.position;
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (isLock) return;

        Vector2 moveOffset = eventData.position - _moveBegin;
        Vector2 targetWorldPos = _startingPoint + moveOffset;

        // 화면 사이즈 얻기
        Vector2 canvasSize = _canvas.pixelRect.size;

        // UI의 월드 크기 계산
        Vector2 uiSize = _rectTransform.rect.size * _rectTransform.lossyScale;

        // Clamp 범위 계산 (Pivot 기준 고려)
        float minX = uiSize.x * _rectTransform.pivot.x;
        float maxX = canvasSize.x - uiSize.x * (1f - _rectTransform.pivot.x);
        float minY = uiSize.y * _rectTransform.pivot.y;
        float maxY = canvasSize.y - uiSize.y * (1f - _rectTransform.pivot.y);

        // 위치 Clamp
        Vector2 clampedScreenPos = new Vector2(
            Mathf.Clamp(targetWorldPos.x, minX, maxX),
            Mathf.Clamp(targetWorldPos.y, minY, maxY)
        );

        _rectTransform.position = clampedScreenPos;
    }

    public void LockDrag()
    {
        if (isLock)
        {
            // 잠금이 풀린 이미지로 변환 추가
            lockText.text = "L";
            isLock = false;
        }
        else
        {
            // 잠금된 이미지로 변환 추가
            lockText.text = "UL";
            isLock = true;
        }        
    }
}