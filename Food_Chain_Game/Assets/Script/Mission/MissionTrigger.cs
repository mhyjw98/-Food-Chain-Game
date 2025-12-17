using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionTrigger : MonoBehaviour
{
    private MissionObject parent;

    private void Awake()
    {
        parent = GetComponentInParent<MissionObject>();
        if (parent == null)
            Debug.LogError("MissionObjectTrigger: MissionObject not found in parent");
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<GamePlayer>();
        if (player == null) return;

        parent.OnPlayerEnter(player);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponentInParent<GamePlayer>();
        if (player == null) return;

        parent.OnPlayerExit(player);
    }
}
