using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static BagBase;


public class SeedItem : MonoBehaviour, IBagItem
{
    [SerializeField] private BagItemType itemType;
    [SerializeField] private DragThrowItem _drag;

    private SeedA _mission;
    private SeedBag _bag;
    
    public BagItemType ItemType => itemType;
    public bool IsSeed => itemType == BagItemType.Seed;

    public RectTransform Rect => _drag._rect;
    private bool _cleared;

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
    public void Init(SeedA mission, BagItemType type)
    {
        _mission = mission;
        _bag = mission != null ? mission.Bag : null;
        itemType = type;
    }
}
