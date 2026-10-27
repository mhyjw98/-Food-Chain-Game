using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionObject : MonoBehaviour
{
    [SerializeField] private MissionType missionType;
    [SerializeField] private SpriteRenderer[] renderers;

    [SerializeField] private Collider2D triggerCollider;

    [Tooltip("서버가 미션 시작/완료를 허용하는 최대 거리. 자식 트리거(클릭 가능 영역)를 덮도록 넉넉하게 설정")]
    [SerializeField] private float interactRange = 4f;

    [Tooltip("서버가 완료를 인정하는 최소 소요 시간(초). 시작 승인 후 이보다 빨리 온 완료 요청은 거부된다. 정상 플레이의 최단 클리어 시간보다 작게 설정")]
    [SerializeField] private float minDurationSeconds = 2f;
    public float MinDurationSeconds => minDurationSeconds;

    private Color[] _defaultColors;
    private GamePlayer _localPlayerInRange;
    private Collider2D _bodyCollider;

    public MissionType MissionType => missionType;

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

        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>();

        _defaultColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            _defaultColors[i] = renderers[i] != null ? renderers[i].color : Color.white;

        SetEnabled(false);
        StartCoroutine(RegisterWhenRegistryReady());
    }

    private IEnumerator RegisterWhenRegistryReady()
    {
        int guard = 0;
        while (MissionRegistry.Instance == null && guard++ < 300)
            yield return null;

        if (MissionRegistry.Instance != null)
            MissionRegistry.Instance.Register(this);
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
        if (UIManager.Instance != null && !UIManager.Instance.CanInteract()) return;
        if (!_localPlayerInRange.CanDoMissions) return;
        if (!_localPlayerInRange.CanStartMissionType(missionType)) return;

        _localPlayerInRange.TryStartMission(missionType);
    }

    public void OnPlayerEnter(GamePlayer player)
    {
        if (!player.isLocalPlayer) return;

        _localPlayerInRange = player;
        SetHighlight(player.CanStartMissionType(missionType));
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
