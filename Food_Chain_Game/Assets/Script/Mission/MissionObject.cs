using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionObject : MonoBehaviour
{
    [SerializeField] private MissionType missionType;
    [SerializeField] private SpriteRenderer[] renderers;

    [SerializeField] private Collider2D triggerCollider;

    private Color[] _defaultColors;
    private GamePlayer _localPlayerInRange;

    public MissionType MissionType => missionType;
    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>();

        _defaultColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            _defaultColors[i] = renderers[i] != null ? renderers[i].color : Color.white;

        if (MissionRegistry.Instance != null)
            MissionRegistry.Instance.Register(this);
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
    private void Update()
    {
        if (_localPlayerInRange == null) return;

        if (Input.GetKeyDown(KeySetting.keys[KeyAction.INTERACT]))
        {
            TryInteract();
        }
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
        if (!_localPlayerInRange.CanStartMissionType(missionType)) return;

        _localPlayerInRange.TryStartMission(missionType);
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
