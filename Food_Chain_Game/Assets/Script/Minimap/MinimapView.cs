using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 미니맵 한 장(코너 미니맵, 전체 맵 오버레이 공용).
// 미리 찍어 둔 맵 그림 위에 UI 마커를 올리는 방식이라 시야(2D Light, 시야 마스크)와 얽히지 않는다.
// 로컬 UI이며 네트워크로 주고받는 것은 없다(이미 클라이언트가 알고 있는 정보만 그린다).
// 로컬 플레이어가 지금 상호작용할 수 있는 오브젝트를 마커로 표시한다(미션: 노란색, 탐색·위장·예측 같은 특수 오브젝트: 보라색).
// 역할에 따라 다르게 보이지만 자기 화면에만 그리므로 다른 플레이어에게 역할이 드러나지 않는다.
// 다른 플레이어는 지금 내 시야에 있을 때만 그 플레이어가 고른 색으로 표시한다.
// 얼룩말·타조(시야 공유 파트너)는 서로 항상 보이고, 파트너 시야에 있는 플레이어도 보인다
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
    [Tooltip("FollowPlayer일 때의 줌: 월드 1칸을 미니맵에서 몇 픽셀로 그릴지(fitToVision이 꺼져 있거나 시야를 못 찾을 때)")]
    [SerializeField] private float followPixelsPerUnit = 12f;
    [Tooltip("FollowPlayer일 때 줌을 내 시야 반경에 맞춘다(미니맵 가장자리 = 시야 끝)")]
    [SerializeField] private bool fitToVision;
    [Tooltip("fitToVision일 때 시야 반경에 곱하는 배율(1이면 시야 끝이 미니맵 가장자리에 닿는다)")]
    [SerializeField, Min(0.1f)] private float visionFitScale = 1f;
    [Tooltip("FitWholeMap일 때 가장자리에 남기는 여백(1이면 꽉 채움)")]
    [SerializeField, Range(0.5f, 1f)] private float fitPadding = 0.95f;

    [Header("Local Player Marker")]
    [SerializeField] private float localMarkerSize = 16f;
    [SerializeField] private Color localMarkerColor = Color.red;

    [Header("Object Markers")]
    [SerializeField] private bool showObjectMarkers = true;
    [SerializeField] private float objectMarkerSize = 12f;
    [Tooltip("상호작용할 수 있는 미션 오브젝트")]
    [SerializeField] private Color interactableMarkerColor = new Color(1f, 0.85f, 0.1f);
    [Tooltip("전갈 독 미션 오브젝트")]
    [SerializeField] private Color poisonMarkerColor = new Color(0.65f, 0.3f, 1f);
    [Tooltip("탐색, 위장, 승리 예측 같은 특수 오브젝트")]
    [SerializeField] private Color specialMarkerColor = new Color(0.65f, 0.3f, 1f);

    [Header("Player Markers")]
    [SerializeField] private bool showPlayerMarkers = true;
    [SerializeField] private float playerMarkerSize = 14f;

    private readonly Stack<Image> _freeMarkers = new();
    private readonly List<Image> _playerMarkers = new();
    private int _usedPlayerMarkers;
    private readonly List<Image> _objectMarkers = new();
    private int _usedObjectMarkers;
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
        UpdateObjectMarkers(local);
        UpdatePlayerMarkers(local);
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
            _pixelsPerUnit = Mathf.Max(0.01f, GetFollowPixelsPerUnit(viewSize));

        Vector2 contentSize = worldSize * _pixelsPerUnit;
        if (content.sizeDelta != contentSize)
            content.sizeDelta = contentSize;

        // 맵을 반대로 밀어서 보고 싶은 지점이 가운데에 오게 한다
        Vector2 focus = mode == ViewMode.FollowPlayer && local != null
            ? (Vector2)local.transform.position
            : config.WorldCenter;

        content.anchoredPosition = -WorldToContent(focus);
    }

    // 시야 반경(낮/밤, 유령 전환 때 함께 바뀐다)이 미니맵 반지름에 딱 맞도록 줌을 정한다
    private float GetFollowPixelsPerUnit(Vector2 viewSize)
    {
        if (!fitToVision) return followPixelsPerUnit;

        var vision = VisionUIController.Instance;
        if (vision == null || vision.fov == null || vision.fov.viewRadius <= 0.01f) return followPixelsPerUnit;

        float viewRadiusPixels = Mathf.Min(viewSize.x, viewSize.y) * 0.5f;
        return viewRadiusPixels / (vision.fov.viewRadius * visionFitScale);
    }

    private void UpdateObjectMarkers(GamePlayer local)
    {
        _usedObjectMarkers = 0;

        if (showObjectMarkers && local != null)
        {
            var missions = MissionRegistry.Instance != null ? MissionRegistry.Instance.All : null;
            if (missions != null)
            {
                for (int i = 0; i < missions.Count; i++)
                {
                    if (CanInteractMission(local, missions[i]))
                        ShowObjectMarker(missions[i], missions[i].MissionType == MissionType.Scorpion_Poison
                            ? poisonMarkerColor
                            : interactableMarkerColor);
                }
            }

            if (local.isAlive && local.RemainingInvestigationCount > 0)
                ShowObjectMarkers(InvestigationObject.All);
            if (local.isAlive && local.CanDisguise && !local.HasDisguised)
                ShowObjectMarkers(DisguiseObject.All);
            if (local.isAlive && local.CanPredict && !local.HasPredicted)
                ShowObjectMarkers(PredictObject.All);
        }

        // 이번 프레임에 쓰지 않은 마커는 숨긴다
        for (int i = _usedObjectMarkers; i < _objectMarkers.Count; i++)
        {
            if (_objectMarkers[i] != null && _objectMarkers[i].gameObject.activeSelf)
                _objectMarkers[i].gameObject.SetActive(false);
        }
    }

    private void UpdatePlayerMarkers(GamePlayer local)
    {
        _usedPlayerMarkers = 0;

        if (showPlayerMarkers && local != null)
        {
            GamePlayer partner = GetVisionPartner(local);
            var fov = VisionUIController.Instance != null ? VisionUIController.Instance.fov : null;

            var players = GamePlayer.AllPlayers;
            for (int i = 0; i < players.Count; i++)
            {
                GamePlayer p = players[i];
                if (p == null || p == local) continue;
                if (!IsPlayerVisibleOnMap(local, p, partner, fov)) continue;

                ShowPlayerMarker(p);
            }
        }

        for (int i = _usedPlayerMarkers; i < _playerMarkers.Count; i++)
        {
            if (_playerMarkers[i] != null && _playerMarkers[i].gameObject.activeSelf)
                _playerMarkers[i].gameObject.SetActive(false);
        }
    }

    private static bool IsPlayerVisibleOnMap(GamePlayer local, GamePlayer p, GamePlayer partner, FOVMaskController2D fov)
    {
        // 화면과 같은 규칙: 산 사람에게 유령은 보이지 않고, 유령에게는 모두 보인다(GamePlayer.ApplyVisibility)
        if (!p.isAlive && local.isAlive) return false;

        if (p.IsInLocalVision) return true;
        if (partner == null) return false;
        if (p == partner) return true;

        // 파트너 시야: 파트너도 같은 피식자라 낮/밤·근접 보정이 나와 같으므로 내 시야 반경을 그대로 쓴다
        if (fov == null) return false;
        return fov.IsVisibleFrom(partner.transform.position, fov.viewRadius, p.transform.position);
    }

    // 얼룩말·타조의 시야 공유 파트너. 파트너가 누구인지는 서버와 본인만 안다(PreySymbiosisAbility, Owner 동기화)
    private static GamePlayer GetVisionPartner(GamePlayer local)
    {
        if (!local.isAlive) return null;
        if (!local.TryGetComponent(out PreySymbiosisAbility symbiosis) || symbiosis.partnerNetId == 0) return null;

        if (!Mirror.NetworkClient.spawned.TryGetValue(symbiosis.partnerNetId, out var identity) || identity == null) return null;

        var partner = identity.GetComponent<GamePlayer>();
        return partner != null && partner.isAlive ? partner : null;
    }

    private void ShowPlayerMarker(GamePlayer p)
    {
        if (_usedPlayerMarkers >= _playerMarkers.Count)
        {
            Image created = RentMarker();
            if (created == null) return;
            _playerMarkers.Add(created);
        }

        Color color = p.gpColor;
        color.a = 1f;

        Image marker = _playerMarkers[_usedPlayerMarkers++];
        PlaceMarker(marker, p.transform.position, playerMarkerSize, color);
    }

    // 미션 오브젝트는 지금 내 미션으로 켜져 있고 아직 끝내지 않은 것만 표시한다(Scanner의 판정과 같다)
    private static bool CanInteractMission(GamePlayer local, MissionObject mission)
    {
        if (mission == null || !mission.isActiveAndEnabled) return false;
        return local.CanDoMissions && local.CanStartMissionType(mission.MissionType);
    }

    private void ShowObjectMarkers<T>(IReadOnlyList<T> objects) where T : Component
    {
        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i] != null && objects[i].gameObject.activeInHierarchy)
                ShowObjectMarker(objects[i], specialMarkerColor);
        }
    }

    private void ShowObjectMarker(Component target, Color color)
    {
        if (_usedObjectMarkers >= _objectMarkers.Count)
        {
            Image created = RentMarker();
            if (created == null) return;
            _objectMarkers.Add(created);
        }

        Image marker = _objectMarkers[_usedObjectMarkers++];
        PlaceMarker(marker, GetMarkerPosition(target), objectMarkerSize, color);
    }

    // 오브젝트의 기준점이 그림 가운데가 아닐 수 있으므로 콜라이더가 있으면 그 가운데를 쓴다
    private static Vector2 GetMarkerPosition(Component target)
    {
        if (target.TryGetComponent(out Collider2D col) && col.enabled)
            return col.bounds.center;
        return target.transform.position;
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
