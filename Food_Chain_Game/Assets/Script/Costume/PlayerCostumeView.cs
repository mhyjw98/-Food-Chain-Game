using UnityEngine;

// 플레이어(로비 RoomPlayer, 게임 GamePlayer)에 의상 그림을 입힌다.
// 몸 그림은 기존 몸 SpriteRenderer의 그림만 바꾸므로 색(플레이어 색), 숨김(시야·유령), 반투명(유령)은 기존 코드가 그대로 처리한다.
// 덮는 그림은 자식 렌더러로 그리고, 몸 렌더러의 켜짐·투명도·레이어를 매 프레임 따라간다
public class PlayerCostumeView : MonoBehaviour
{
    [Tooltip("의상 몸 그림을 바꿀 렌더러. 비우면 이 오브젝트의 SpriteRenderer")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [Tooltip("덮는 그림용 렌더러. 비우면 실행할 때 자식으로 만든다")]
    [SerializeField] private SpriteRenderer overlayRenderer;
    [Tooltip("덮는 그림을 몸보다 몇 단계 위에 그릴지")]
    [SerializeField] private int overlaySortingOffset = 1;

    private Sprite _defaultBodySprite;
    private CostumeData _costume;
    private bool _initialized;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        if (_initialized) return;
        _initialized = true;

        if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
        if (bodyRenderer != null) _defaultBodySprite = bodyRenderer.sprite;

        if (overlayRenderer == null && bodyRenderer != null)
        {
            var go = new GameObject("Costume Overlay");
            go.transform.SetParent(bodyRenderer.transform, false);
            overlayRenderer = go.AddComponent<SpriteRenderer>();
        }

        if (overlayRenderer != null && bodyRenderer != null)
        {
            overlayRenderer.sharedMaterial = bodyRenderer.sharedMaterial;
            overlayRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
            overlayRenderer.sortingOrder = bodyRenderer.sortingOrder + overlaySortingOffset;
            overlayRenderer.maskInteraction = bodyRenderer.maskInteraction;
            overlayRenderer.enabled = false;
        }
    }

    public void SetCostume(int index)
    {
        Init();
        _costume = CostumeCatalog.Get(index);

        if (bodyRenderer != null)
        {
            Sprite body = _costume != null && _costume.bodySprite != null ? _costume.bodySprite : _defaultBodySprite;
            if (bodyRenderer.sprite != body) bodyRenderer.sprite = body;
        }

        if (overlayRenderer != null)
            overlayRenderer.sprite = _costume != null ? _costume.overlaySprite : null;

        SyncOverlay();
    }

    private void LateUpdate()
    {
        SyncOverlay();
    }

    // 몸이 숨겨지면(시야 밖, 로비 밖) 함께 숨고, 유령처럼 반투명해지면 함께 반투명해진다
    private void SyncOverlay()
    {
        if (overlayRenderer == null || bodyRenderer == null) return;

        bool show = overlayRenderer.sprite != null && bodyRenderer.enabled && bodyRenderer.gameObject.activeInHierarchy;
        if (overlayRenderer.enabled != show) overlayRenderer.enabled = show;
        if (!show) return;

        Color body = bodyRenderer.color;
        Color wanted = _costume != null && _costume.tintOverlay ? body : new Color(1f, 1f, 1f, body.a);
        if (overlayRenderer.color != wanted) overlayRenderer.color = wanted;

        int layer = bodyRenderer.gameObject.layer;
        if (overlayRenderer.gameObject.layer != layer) overlayRenderer.gameObject.layer = layer;
    }
}
