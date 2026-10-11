using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 미니맵 한 장(코너 미니맵, 전체 맵 오버레이 공용).
// 미리 찍어 둔 맵 그림 위에 UI 마커를 올리는 방식이라 시야(2D Light, 시야 마스크)와 얽히지 않는다.
// 로컬 UI이며 네트워크로 주고받는 것은 없다(이미 클라이언트가 알고 있는 정보만 그린다)
public class MinimapView : MonoBehaviour
{
    public enum ViewMode
    {
        FollowPlayer,   // 로컬 플레이어를 가운데에 두고 맵이 움직인다
        FitWholeMap,    // 맵 전체가 한 화면에 들어온다
    }

    public enum Corner { None, BottomRight, BottomLeft, TopRight, TopLeft }

    [Header("Refs")]
    [SerializeField] private MinimapConfig config;
    [Tooltip("미니맵이 보이는 영역(이 밖은 잘린다)")]
    [SerializeField] private RectTransform viewport;
    [Tooltip("맵 전체. 맵 그림과 마커의 부모")]
    [SerializeField] private RectTransform content;
    [SerializeField] private Image mapImage;
    [SerializeField] private RectTransform markerRoot;
    [Tooltip("마커를 찍어 낼 원본(꺼 둔 Image)")]
    [SerializeField] private Image markerTemplate;

    [Header("Layout")]
    [Tooltip("붙일 화면 모서리. None이면 위치와 크기를 건드리지 않는다")]
    [SerializeField] private Corner corner = Corner.BottomRight;
    [SerializeField] private Vector2 size = new Vector2(300f, 300f);
    [Tooltip("모서리에서 띄우는 거리")]
    [SerializeField] private Vector2 margin = new Vector2(50f, 50f);

    [Header("View")]
    [SerializeField] private ViewMode mode = ViewMode.FollowPlayer;
    [Tooltip("FollowPlayer일 때의 줌: 월드 1칸을 미니맵에서 몇 픽셀로 그릴지")]
    [SerializeField] private float followPixelsPerUnit = 12f;
    [Tooltip("FitWholeMap일 때 가장자리에 남기는 여백(1이면 꽉 채움)")]
    [SerializeField, Range(0.5f, 1f)] private float fitPadding = 0.95f;

    [Header("Local Player Marker")]
    [SerializeField] private float localMarkerSize = 16f;
    [SerializeField] private Color localMarkerColor = Color.red;

    private readonly Stack<Image> _freeMarkers = new();
    private Image _localMarker;
    private float _pixelsPerUnit;

    private void Awake()
    {
        // 화면이 없는 실행(전용 서버)에서는 미니맵을 쓰지 않는다
        if (Application.isBatchMode)
        {
            gameObject.SetActive(false);
            return;
        }

        if (markerTemplate != null) markerTemplate.gameObject.SetActive(false);

        ApplyLayout();
        ApplyMapImage();
    }

    private void OnValidate()
    {
        ApplyLayout();
    }

    private void LateUpdate()
    {
        if (config == null || viewport == null || content == null) return;

        GamePlayer local = GamePlayer.LocalPlayer;

        UpdateContent(local);
        UpdateLocalMarker(local);
    }

    // 모서리·크기 설정을 이 오브젝트의 위치에 반영한다
    private void ApplyLayout()
    {
        if (corner == Corner.None) return;

        var rect = transform as RectTransform;
        if (rect == null) return;

        bool right = corner == Corner.BottomRight || corner == Corner.TopRight;
        bool top = corner == Corner.TopRight || corner == Corner.TopLeft;

        Vector2 anchor = new Vector2(right ? 1f : 0f, top ? 1f : 0f);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(right ? -margin.x : margin.x, top ? -margin.y : margin.y);
    }

    private void ApplyMapImage()
    {
        if (mapImage == null || config == null) return;

        mapImage.sprite = config.mapSprite;
        mapImage.enabled = config.mapSprite != null;
    }

    private void UpdateContent(GamePlayer local)
    {
        Vector2 worldSize = config.WorldSize;
        Vector2 viewSize = viewport.rect.size;

        if (mode == ViewMode.FitWholeMap)
            _pixelsPerUnit = Mathf.Min(viewSize.x / worldSize.x, viewSize.y / worldSize.y) * fitPadding;
        else
            _pixelsPerUnit = Mathf.Max(0.01f, followPixelsPerUnit);

        Vector2 contentSize = worldSize * _pixelsPerUnit;
        if (content.sizeDelta != contentSize)
            content.sizeDelta = contentSize;

        // 맵을 반대로 밀어서 보고 싶은 지점이 가운데에 오게 한다
        Vector2 focus = mode == ViewMode.FollowPlayer && local != null
            ? (Vector2)local.transform.position
            : config.WorldCenter;

        content.anchoredPosition = -WorldToContent(focus);
    }

    private void UpdateLocalMarker(GamePlayer local)
    {
        if (local == null)
        {
            if (_localMarker != null && _localMarker.gameObject.activeSelf)
                _localMarker.gameObject.SetActive(false);
            return;
        }

        if (_localMarker == null) _localMarker = RentMarker();
        if (_localMarker == null) return;

        PlaceMarker(_localMarker, local.transform.position, localMarkerSize, localMarkerColor);
        _localMarker.transform.SetAsLastSibling();
    }

    // 월드 좌표 → 맵(content) 가운데 기준 좌표
    private Vector2 WorldToContent(Vector2 world)
    {
        return (world - config.WorldCenter) * _pixelsPerUnit;
    }

    private void PlaceMarker(Image marker, Vector2 world, float markerSize, Color color)
    {
        var rect = marker.rectTransform;
        rect.anchoredPosition = WorldToContent(world);

        // 줌과 상관없이 화면에서 같은 크기로 보이게 한다
        Vector2 wanted = new Vector2(markerSize, markerSize);
        if (rect.sizeDelta != wanted) rect.sizeDelta = wanted;
        if (marker.color != color) marker.color = color;

        if (!marker.gameObject.activeSelf) marker.gameObject.SetActive(true);
    }

    // 마커는 만들었다 지우지 않고 돌려 쓴다
    private Image RentMarker()
    {
        if (_freeMarkers.Count > 0) return _freeMarkers.Pop();
        if (markerTemplate == null || markerRoot == null) return null;

        Image marker = Instantiate(markerTemplate, markerRoot);
        var rect = marker.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        return marker;
    }

    private void ReturnMarker(Image marker)
    {
        if (marker == null) return;

        marker.gameObject.SetActive(false);
        _freeMarkers.Push(marker);
    }
}
