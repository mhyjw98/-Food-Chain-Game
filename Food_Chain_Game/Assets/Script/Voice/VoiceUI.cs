using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Vivox;
using UnityEngine;

public class VoiceUI : MonoBehaviour
{
    [SerializeField] Transform root;
    [SerializeField] LobbyVoiceParticipantRow rowPrefab;
    [SerializeField] TextMeshProUGUI descriptText;

    readonly Dictionary<string, LobbyVoiceParticipantRow> _rowsByPlayerId = new();

    Coroutine _refreshLoop;
    bool _vivoxHooked;

    void OnEnable()
    {
        VoiceManager.OnVoiceChannelJoined += RefreshAll;
        StartCoroutine(CoWaitVivoxAndHook());

        if (_refreshLoop != null) StopCoroutine(_refreshLoop);
        _refreshLoop = StartCoroutine(CoRefreshLoop());
    }

    void OnDisable()
    {
        VoiceManager.OnVoiceChannelJoined -= RefreshAll;
        UnhookVivoxEvents();
        ClearRows();

        if (_refreshLoop != null) StopCoroutine(_refreshLoop);
        _refreshLoop = null;
    }
    IEnumerator CoRefreshLoop()
    {
        var wait = new WaitForSeconds(0.25f);
        while (true)
        {
            RefreshAll();
            yield return wait;
        }
    }

    IEnumerator CoWaitVivoxAndHook()
    {
        while (VoiceManager.Instance == null || !VoiceManager.Instance.IsLoggedIn || VivoxService.Instance == null)
            yield return null;

        HookVivoxEvents();
        RefreshAll();
    }

    void HookVivoxEvents()
    {
        if (_vivoxHooked) return;

        VivoxService.Instance.ParticipantAddedToChannel += OnParticipantAdded;
        VivoxService.Instance.ParticipantRemovedFromChannel += OnParticipantRemoved;

        _vivoxHooked = true;
    }

    void UnhookVivoxEvents()
    {
        if (!_vivoxHooked) return;
        if (VivoxService.Instance == null) return;

        VivoxService.Instance.ParticipantAddedToChannel -= OnParticipantAdded;
        VivoxService.Instance.ParticipantRemovedFromChannel -= OnParticipantRemoved;

        _vivoxHooked = false;
    }

    void OnParticipantAdded(VivoxParticipant p)
    {
        if (!IsCurrentLobbyChannel(p)) return;
        UpsertRow(p);
    }

    void OnParticipantRemoved(VivoxParticipant p)
    {
        if (!IsCurrentLobbyChannel(p)) return;
        RemoveRow(p.PlayerId);
    }

    bool IsCurrentLobbyChannel(VivoxParticipant p)
    {
        if (p == null || VoiceManager.Instance == null) return false;
        var ch = GetUiTargetChannel();
        if (string.IsNullOrEmpty(ch)) return false;

        return p.ChannelName == ch;
    }

    string GetUiTargetChannel()
    {
        if (VoiceManager.Instance == null || VivoxService.Instance == null)
            return null;

        var ch = VoiceManager.Instance.CurrentChannel;
        if (!string.IsNullOrEmpty(ch) && VivoxService.Instance.ActiveChannels.ContainsKey(ch))
            return ch;

        foreach (var key in VivoxService.Instance.ActiveChannels.Keys)
        {
            if (key.StartsWith("lobby_")) return key;
        }

        return VivoxService.Instance.ActiveChannels.Keys.FirstOrDefault();
    }
    public void ResetAllParticipantVolumesToDefault()
    {
        if (VivoxService.Instance == null || VoiceManager.Instance == null) return;

        var ch = GetUiTargetChannel();  
        if (string.IsNullOrEmpty(ch)) return;

        if (!VivoxService.Instance.ActiveChannels.TryGetValue(ch, out var participants))
            return;

        foreach (var p in participants)
        {
            if (p == null || string.IsNullOrEmpty(p.PlayerId)) continue;

            PlayerPrefs.SetInt(VoiceManager.ParticipantVolPrefPrefix + p.PlayerId, 70);

            int vivox = Mathf.Clamp(70 - 50, -50, 50);
            p.SetLocalVolume(vivox);

            if (_rowsByPlayerId.TryGetValue(p.PlayerId, out var row) && row != null)
                row.Bind(p);
        }

        PlayerPrefs.Save();
    }
    public void RefreshAll()
    {
        if (root == null || rowPrefab == null) return;
        if (VivoxService.Instance == null || VoiceManager.Instance == null) return;

        var ch = GetUiTargetChannel();
        if (string.IsNullOrEmpty(ch)) return;

        if (!VivoxService.Instance.ActiveChannels.TryGetValue(ch, out var participants))
            return;

        var aliveIds = new HashSet<string>();

        foreach (var p in participants)
        {
            if (p == null || string.IsNullOrEmpty(p.PlayerId)) continue;
            if (p.IsSelf) continue;

            aliveIds.Add(p.PlayerId);
            UpsertRow(p);
        }

        var toRemove = new List<string>();
        foreach (var kv in _rowsByPlayerId)
        {
            if (!aliveIds.Contains(kv.Key))
                toRemove.Add(kv.Key);
        }
        foreach (var id in toRemove)
            RemoveRow(id);

        if (descriptText != null && !string.IsNullOrEmpty(descriptText.text))
            descriptText.text = string.Empty;
    }

    void UpsertRow(VivoxParticipant p)
    {
        if (p == null || string.IsNullOrEmpty(p.PlayerId)) return;

        if (p.IsSelf) return;

        if (_rowsByPlayerId.TryGetValue(p.PlayerId, out var existing) && existing != null)
        {
            existing.Bind(p);
            return;
        }

        var row = Instantiate(rowPrefab, root);
        row.Bind(p);
        _rowsByPlayerId[p.PlayerId] = row;
    }

    void RemoveRow(string playerId)
    {
        if (string.IsNullOrEmpty(playerId)) return;
        if (!_rowsByPlayerId.TryGetValue(playerId, out var row)) return;

        _rowsByPlayerId.Remove(playerId);
        if (row != null) Destroy(row.gameObject);
    }

    void ClearRows()
    {
        foreach (var kv in _rowsByPlayerId)
        {
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        }
        _rowsByPlayerId.Clear();
    }

    public void ResetAllPlayerToDefault()
    {
        foreach (var kv in _rowsByPlayerId)
        {
            if (kv.Value != null)
                kv.Value.ApplyDefaultVolume();
        }
    }
}
