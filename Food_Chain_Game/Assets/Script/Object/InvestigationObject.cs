using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvestigationObject : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] renderers;

    [SerializeField] private Collider2D triggerCollider;

    [Tooltip("서버가 탐색을 허용하는 최대 거리. Scanner의 scanRadius보다 조금 크게 잡아 지연 오차를 허용")]
    [SerializeField] private float interactRange = 2f;

    private static readonly List<InvestigationObject> _instances = new();
    public static IReadOnlyList<InvestigationObject> All => _instances;

    private Color[] _defaultColors;
    private GamePlayer _localPlayerInRange;
    private Collider2D _bodyCollider;

    public static bool IsAnyInRange(Vector2 position)
    {
        for (int i = 0; i < _instances.Count; i++)
        {
            if (_instances[i] != null && _instances[i].IsInRange(position))
                return true;
        }
        return false;
    }

    public bool IsInRange(Vector2 position)
    {
        Vector2 closest = (_bodyCollider != null && _bodyCollider.enabled)
            ? _bodyCollider.ClosestPoint(position)
            : (Vector2)transform.position;

        return (closest - position).sqrMagnitude <= interactRange * interactRange;
    }

    private void Awake()
    {
        _bodyCollider = GetComponent<Collider2D>();
        _instances.Add(this);

        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>();

        _defaultColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            _defaultColors[i] = renderers[i] != null ? renderers[i].color : Color.white;

        SetEnabled(false);
    }

    private void OnDestroy()
    {
        _instances.Remove(this);
    }

    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;

        if (triggerCollider != null) triggerCollider.enabled = enabled;

        if (!enabled) SetHighlight(false);
    }
    private void OnMouseDown()
    {
        if (_localPlayerInRange == null) return;
        TryInteract();
    }

    private void TryInteract()
    {
        if (_localPlayerInRange == null) return;

        _localPlayerInRange.TryStartInvestigation();
    }

    public void OnPlayerEnter(GamePlayer player)
    {
        if (!player.isLocalPlayer) return;

        _localPlayerInRange = player;
        SetHighlight(true);
    }

    public void OnPlayerExit(GamePlayer player)
    {
        if (!player.isLocalPlayer) return;

        if (_localPlayerInRange == player)
        {
            _localPlayerInRange = null;
            SetHighlight(false);
        }
    }

    private void SetHighlight(bool on)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].color = on ? Color.green : _defaultColors[i];
        }
    }
}
