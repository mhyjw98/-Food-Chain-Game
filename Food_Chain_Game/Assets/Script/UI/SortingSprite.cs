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

    void Start()
    {
        sorter = FindObjectOfType<SpriteSorter>();
        sprite = GetComponent<SpriteRenderer>();

        sprite.sortingOrder = sorter.GetSortingOrder(gameObject);
    }
    void Update()
    {
        if(type == ESortingType.Update)
        {
            sprite.sortingOrder = sorter.GetSortingOrder(gameObject);
        }
    }
}
