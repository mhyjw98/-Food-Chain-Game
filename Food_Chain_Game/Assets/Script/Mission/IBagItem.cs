using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BagBase;

public interface IBagItem
{
    BagItemType ItemType { get; }
}
