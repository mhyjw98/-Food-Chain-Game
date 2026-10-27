using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitBag : BagBase
{
    [SerializeField] private RectTransform bagRect;
    [SerializeField] private FruitA mission;
    [SerializeField] private Canvas canvas;

    private void Awake()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
    }
    protected override void OnItemAccepted(IBagItem item, GameObject go)
    {
        var fruitItem = go.GetComponent<FruitItemA>();
        if (fruitItem != null && mission != null)
        {
            mission.OnFruitPutInBag(fruitItem);
        }

        Destroy(go);
    }

    public bool TryCatchItem(FruitItemA item)
    {
        if (item == null || bagRect == null) return false;

        var itemRect = item.GetComponent<RectTransform>();
        if (itemRect == null) return false;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
            canvas != null ? canvas.worldCamera : null,
            itemRect.position
        );

        bool inside = RectTransformUtility.RectangleContainsScreenPoint(
            bagRect,
            screenPos,
            canvas != null ? canvas.worldCamera : null
        );

        if (!inside)
            return false;

        mission.OnFruitPutInBag(item);

        return true;
    }
}
