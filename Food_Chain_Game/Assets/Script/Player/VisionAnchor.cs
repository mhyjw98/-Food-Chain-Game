using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisionAnchor : MonoBehaviour
{
    public static readonly List<VisionAnchor> All = new();

    public Transform head;
    public PlayerUIVisibility uiVisibility;

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);
}
