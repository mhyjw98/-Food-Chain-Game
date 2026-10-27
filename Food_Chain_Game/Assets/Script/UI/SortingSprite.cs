using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SortingSprite : MonoBehaviour
{
    public enum ESortingType
    {
        Static, Update
    }
    [SerializeField] private ESortingType type;
    private SpriteSorter sorter;
    private SpriteRenderer sprite;

    // 씬마다 SpriteSorter는 하나라서, 오브젝트마다 씬 전체를 검색하지 않도록 같이 쓴다
    private static SpriteSorter _sharedSorter;

    void Start()
    {
        if (_sharedSorter == null)
            _sharedSorter = FindFirstObjectByType<SpriteSorter>();

        sorter = _sharedSorter;
        sprite = GetComponent<SpriteRenderer>();

        ApplySortingOrder();
    }
    void Update()
    {
        if(type == ESortingType.Update)
        {
            ApplySortingOrder();
        }
    }

    private static int _lastSearchFrame = -1;

    // SpriteSorter가 없는 씬에서는 정렬하지 않는다.
    // 씬이 바뀌면 이전 씬의 SpriteSorter는 파괴되므로(로비↔게임을 오가는 RoomPlayer 등) 다시 찾는다
    private void ApplySortingOrder()
    {
        if (sprite == null) return;

        if (sorter == null)
        {
            if (_sharedSorter == null && _lastSearchFrame != Time.frameCount)
            {
                _lastSearchFrame = Time.frameCount;
                _sharedSorter = FindFirstObjectByType<SpriteSorter>();
            }

            sorter = _sharedSorter;
            if (sorter == null) return;
        }

        sprite.sortingOrder = sorter.GetSortingOrder(gameObject);
    }
}
