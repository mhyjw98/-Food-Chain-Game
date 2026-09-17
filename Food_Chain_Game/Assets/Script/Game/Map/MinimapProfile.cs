using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MiniMap/Profile")]
public class MinimapProfile : ScriptableObject
{
    public Vector2 worldMin;
    public Vector2 worldMax;

    public Vector2 mapSize;
}
