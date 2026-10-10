using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoodBag : BagBase
{
    [SerializeField] private RectTransform bagRect;
    [SerializeField] private WoodB mission;
    [SerializeField] private Canvas canvas;

    private void Awake()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
    }
    protected override void OnItemAccepted(IBagItem item, GameObject go)
    {
        var woodItem = go.GetComponent<WoodItemB>();
        if (woodItem != null && mission != null)
        {
            mission.OnWoodPutInBag(woodItem);
        }

        Destroy(go);
    }

    public bool TryCatchItem(WoodItemB item)
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

        mission.OnWoodPutInBag(item);

        return true;
    }
}
