using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 설정 창의 플레이어 음성 목록.
// 지금 같은 음성 채널에 있는 사람만이 아니라 방의 모든 플레이어를 보여 준다(유령/생존자, 로비/게임과 무관하게 음량·음소거를 정할 수 있다).
// RoomPlayer는 게임 중에도 유지되므로 "방에 있는 플레이어" 목록으로 쓴다
public class VoiceUI : MonoBehaviour
{
    [SerializeField] Transform root;
    [SerializeField] LobbyVoiceParticipantRow rowPrefab;
    [SerializeField] TextMeshProUGUI descriptText;

    [Tooltip("목록의 플레이어 구성을 다시 맞추는 간격(초)")]
    [SerializeField] float listRefreshInterval = 0.5f;
    [Tooltip("사망/말하는 중 표시를 갱신하는 간격(초)")]
    [SerializeField] float stateRefreshInterval = 0.1f;

    readonly Dictionary<string, LobbyVoiceParticipantRow> _rowsByVoiceId = new();
    readonly HashSet<string> _present = new();
    readonly List<string> _removeBuffer = new();

    float _nextListRefresh;
    float _nextStateRefresh;

    void OnEnable()
    {
        VoiceManager.OnVoiceChannelJoined += RefreshAll;
    }

    void OnDisable()
    {
        VoiceManager.OnVoiceChannelJoined -= RefreshAll;
        ClearRows();
    }

    void Update()
    {
        // 목록이 화면에 보일 때만 갱신한다
        if (root == null || !root.gameObject.activeInHierarchy) return;

        if (Time.unscaledTime >= _nextListRefresh)
        {
            _nextListRefresh = Time.unscaledTime + listRefreshInterval;
            RefreshAll();
        }

        if (Time.unscaledTime >= _nextStateRefresh)
        {
            _nextStateRefresh = Time.unscaledTime + stateRefreshInterval;
            RefreshStates();
        }
    }

    public void RefreshAll()
    {
        if (root == null || rowPrefab == null) return;

        // 화면에 보이지 않는 동안에는 만들지 않는다(열릴 때 Update에서 바로 채운다)
        if (!root.gameObject.activeInHierarchy) return;

        _present.Clear();

        string localVoiceId = VoiceManager.Instance != null ? VoiceManager.Instance.LocalVoiceId : null;

        foreach (var roomPlayer in FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None))
        {
            // 음성 ID가 아직 없는 플레이어(음성 로그인 전)는 설정할 대상이 없어 건너뛴다
            if (roomPlayer == null || string.IsNullOrEmpty(roomPlayer.voiceId)) continue;

            // 나 자신은 목록에 넣지 않는다. 게임 중에는 내 캐릭터가 GamePlayer로 바뀌어
            // 내 RoomPlayer의 isLocalPlayer가 꺼지므로 음성 ID와 소유 여부로도 가린다
            if (roomPlayer.isLocalPlayer || roomPlayer.isOwned || roomPlayer.voiceId == localVoiceId) continue;

            string voiceId = roomPlayer.voiceId;
            _present.Add(voiceId);

            if (!_rowsByVoiceId.TryGetValue(voiceId, out var row) || row == null)
            {
                row = Instantiate(rowPrefab, root);
                _rowsByVoiceId[voiceId] = row;
            }

            row.Bind(voiceId, roomPlayer.nickname);
        }

        _removeBuffer.Clear();
        foreach (var id in _rowsByVoiceId.Keys)
            if (!_present.Contains(id)) _removeBuffer.Add(id);
        foreach (var id in _removeBuffer)
            RemoveRow(id);

        if (descriptText != null && !string.IsNullOrEmpty(descriptText.text))
            descriptText.text = string.Empty;

        RefreshStates();
    }

    void RefreshStates()
    {
        var voice = VoiceManager.Instance;

        foreach (var pair in _rowsByVoiceId)
        {
            if (pair.Value == null) continue;

            bool dead = GamePlayer.IsKnownDeadToLocal(VoiceManager.FindGamePlayer(pair.Key));
            bool speaking = voice != null && voice.IsSpeakingVisible(pair.Key);
            pair.Value.SetState(dead, speaking);
        }
    }

    void RemoveRow(string voiceId)
    {
        if (!_rowsByVoiceId.TryGetValue(voiceId, out var row)) return;

        _rowsByVoiceId.Remove(voiceId);
        if (row != null) Destroy(row.gameObject);
    }

    void ClearRows()
    {
        foreach (var pair in _rowsByVoiceId)
        {
            if (pair.Value != null) Destroy(pair.Value.gameObject);
        }
        _rowsByVoiceId.Clear();
    }

    // "기본값 적용" 버튼
    public void ResetAllPlayerToDefault()
    {
        foreach (var pair in _rowsByVoiceId)
        {
            if (pair.Value != null)
                pair.Value.ApplyDefaultVolume();
        }
    }

    public void ResetAllParticipantVolumesToDefault() => ResetAllPlayerToDefault();
}
