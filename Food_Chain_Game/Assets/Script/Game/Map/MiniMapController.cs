using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniMapController : MonoBehaviour
{
    [Header("References")]
    public MinimapProfile profile;

    public Transform player;

    public RectTransform mapContainer;

    [Header("Settings")]
    public float zoom = 1f;

    void Update()
    {
        UpdateMapPosition();
    }

    void UpdateMapPosition()
    {
        Vector2 mapPos = WorldToMapPosition(player.position);

        mapContainer.anchoredPosition = -mapPos * zoom;
    }

    Vector2 WorldToMapPosition(Vector3 worldPos)
    {
        float normalizedX = Mathf.InverseLerp(
            profile.worldMin.x,
            profile.worldMax.x,
            worldPos.x
        );

        float normalizedY = Mathf.InverseLerp(
            profile.worldMin.y,
            profile.worldMax.y,
            worldPos.z
        );

        float mapX =
            (normalizedX - 0.5f) * profile.mapSize.x;

        float mapY =
            (normalizedY - 0.5f) * profile.mapSize.y;

        return new Vector2(mapX, mapY);
    }
}
