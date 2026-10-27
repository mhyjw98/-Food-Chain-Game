using UnityEngine;

// 미니맵이 그릴 맵의 범위와 그림. 맵(씬)마다 하나씩 만든다.
// 맵 그림은 Tools > Minimap > Capture Map Image 로 씬에서 찍어 만든다
[CreateAssetMenu(menuName = "Minimap/Config", fileName = "MinimapConfig")]
public class MinimapConfig : ScriptableObject
{
    [Header("World Bounds")]
    [Tooltip("미니맵에 담을 맵의 왼쪽 아래 끝(월드 좌표)")]
    public Vector2 worldMin = new Vector2(-30f, -6f);
    [Tooltip("미니맵에 담을 맵의 오른쪽 위 끝(월드 좌표)")]
    public Vector2 worldMax = new Vector2(30f, 17f);

    [Header("Map Image")]
    [Tooltip("worldMin~worldMax 범위를 그대로 담은 맵 그림")]
    public Sprite mapSprite;

    [Header("Capture (Editor)")]
    [Tooltip("맵 그림을 찍을 때 월드 1칸을 몇 픽셀로 찍을지")]
    public int capturePixelsPerUnit = 32;
    [Tooltip("맵 그림에 찍을 레이어")]
    public LayerMask captureLayers = ~0;
    [Tooltip("맵 바깥(아무것도 없는 곳)의 색")]
    public Color captureBackground = Color.black;

    public Vector2 WorldSize => new Vector2(Mathf.Max(0.01f, worldMax.x - worldMin.x), Mathf.Max(0.01f, worldMax.y - worldMin.y));
    public Vector2 WorldCenter => (worldMin + worldMax) * 0.5f;
}
