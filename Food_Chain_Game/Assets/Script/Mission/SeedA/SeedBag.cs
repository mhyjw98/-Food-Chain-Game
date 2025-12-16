using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeedBag : BagBase
{
    [SerializeField] private RectTransform bagRect;
    [SerializeField] private SeedA mission;
    [SerializeField] private Canvas canvas;

    private void Awake()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
    }
    protected override void OnItemAccepted(IBagItem item, GameObject go)
    {
        var seedItem = go.GetComponent<SeedItem>();
        if (seedItem != null && mission != null)
        {
            mission.OnSeedCollected(seedItem);
        }

        Destroy(go);
    }

    public bool TryCatchItem(SeedItem item)
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

        if (!item.IsSeed)
            return false;

        if (mission != null)
            mission.OnSeedCollected(item);

        Destroy(item.gameObject);
        return true;
    }
}
