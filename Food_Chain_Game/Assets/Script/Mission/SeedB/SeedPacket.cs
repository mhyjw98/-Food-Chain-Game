using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

public class SeedPacket : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform spawnParent;
    [SerializeField] private RectTransform[] validArea;
    [SerializeField] private PlantSeed seedPrefab;
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject openedVisual;

    private RectTransform _rect;
    private bool _opened = false;
    private int _spawnedCount = 0;

    private PlantSeed _currentSeed;
    private void Awake()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
        _rect = GetComponent<RectTransform>();
        SetOpened(false);
    }

    public void SetOpened(bool opened)
    {
        _opened = opened;
        if (closedVisual != null) closedVisual.SetActive(!opened);
        if (openedVisual != null) openedVisual.SetActive(opened);
    }
    public void NotifySeedRemoved()
    {
        if (_spawnedCount > 0)
            _spawnedCount--;
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!_opened) return;
        if (seedPrefab == null || spawnParent == null) return;

        _currentSeed = Instantiate(seedPrefab, spawnParent);
        var seedRect = _currentSeed.GetComponent<RectTransform>();

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            spawnParent,
            eventData.position,
            canvas != null ? canvas.worldCamera : null,
            out localPoint
        );

        seedRect.anchoredPosition = localPoint;
        seedRect.localScale = Vector3.one;

        _spawnedCount++;       
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (_currentSeed == null) return;
        if (spawnParent == null) return;

        var seedRect = _currentSeed.GetComponent<RectTransform>();
        if (seedRect == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            spawnParent,
            eventData.position,
            canvas != null ? canvas.worldCamera : null,
            out localPoint
        );

        seedRect.anchoredPosition = localPoint;
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (_currentSeed == null || validArea.Length == 0) return;

        var seedRect = _currentSeed.GetComponent<RectTransform>();

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
            canvas != null ? canvas.worldCamera : null,
            seedRect.position
        );

        foreach (var area in validArea)
        {
            if (area == null) continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(area, screenPos, canvas.worldCamera))
            {
                var cellRect = area.GetComponent<RectTransform>();
                if (cellRect != null)
                {
                    seedRect.SetParent(cellRect, worldPositionStays: false);
                    seedRect.anchoredPosition = Vector2.zero;
                }

                area.GetComponent<SoilCell>().AcceptSeed(_currentSeed);
                _currentSeed = null;
                return;
            }
        }              

        Destroy(_currentSeed.gameObject);

        NotifySeedRemoved();

        _currentSeed = null;
        return;
    }
      
}
