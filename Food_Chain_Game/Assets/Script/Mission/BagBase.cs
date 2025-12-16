using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class BagBase : MonoBehaviour
{
    [SerializeField] private BagItemType[] acceptedTypes;

    public enum BagItemType
    {
        Seed, Fruit, Wood, Leaf, ShellFish, Fish, Meat, Trash, Stone, Crop, Insect, Grass
    }   
    protected virtual void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    //private void OnTriggerEnter2D(Collider2D other)
    //{
    //    Debug.Log("[BagBase] TriggerEnter »£√‚");
    //    var bagItem = other.GetComponent<IBagItem>();
    //    if (bagItem == null) return;

    //    bool accept = AcceptsType(acceptedTypes, bagItem.ItemType);

    //    if (accept)
    //    {
    //        OnItemAccepted(bagItem, other.gameObject);
    //    }
    //}

    bool AcceptsType(BagItemType[] bagAcceptTypes, BagItemType type)
    {
        for (int i = 0; i < bagAcceptTypes.Length; i++)
        {
            if (bagAcceptTypes[i].Equals(type))
                return true;
        }
        return false;
    }
    protected abstract void OnItemAccepted(IBagItem item, GameObject go);
}
