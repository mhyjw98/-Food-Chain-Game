using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ZoneType { Forest, Field, River, Sky }
public class MapData : MonoBehaviour
{
    public ZoneType zoneType;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out GamePlayer player))
        {
            if (player.isLocalPlayer)
            {
                player.CmdChangeZone(zoneType);
            }
        }
    }
}
