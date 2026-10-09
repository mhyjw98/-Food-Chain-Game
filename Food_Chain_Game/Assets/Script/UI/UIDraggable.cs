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
    [Tooltip("창 위(스크롤 영역 밖)에서 휠을 굴렸을 때 한 칸당 스크롤되는 비율")]
    [SerializeField] private float wheelScrollStep = 0.15f;

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
        if (scrollRect == null || Input.mouseScrollDelta.y == 0) return;

        // 마우스가 이 창 위에 있을 때만 스크롤한다(예전에는 화면 어디서 휠을 굴려도 채팅이 움직였다).
        // 스크롤 영역 위에서는 ScrollRect가 직접 처리하므로 여기서 또 움직이지 않는다
        Camera cam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? _canvas.worldCamera : null;
        Vector2 mouse = Input.mousePosition;

        if (!RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, mouse, cam)) return;
        if (scrollArea != null && RectTransformUtility.RectangleContainsScreenPoint(scrollArea, mouse, cam)) return;

        float scrollDelta = Input.mouseScrollDelta.y * wheelScrollStep;
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + scrollDelta);
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