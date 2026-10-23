using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Vivox;
using UnityEngine;

// 게임 중 "누구의 목소리가 들리는지"를 듣는 쪽(내 클라이언트)에서 참가자별 음량으로 조절한다.
//  - 생존자: 정해진 거리 안에 있고 벽에 가리지 않은 생존자만 들린다(멀수록 작게 들린다).
//    거리는 캐릭터·낮밤과 상관없이 같아서, 내가 들리면 상대에게도 내가 들린다
//  - 유령: 유령끼리는 전부 들리고(유령 채널), 가까이 있는 생존자의 목소리도 들린다
// 내 플레이어에서만 동작한다(GamePlayer.OnStartLocalPlayer에서 켠다)
public class InGameVoiceProximity : MonoBehaviour
{
    [SerializeField] private GamePlayer localPlayer;
    [SerializeField] private float tickInterval = 0.10f;

    [Header("생존자")]
    [Tooltip("게임에서 가장 좁은 시야 반경(포식자의 밤 시야). 듣는 범위는 최소·최대 시야의 가운데 값이다")]
    [SerializeField] private float minVisionRadius = 6f;
    [Tooltip("게임에서 가장 넓은 시야 반경(낮 시야)")]
    [SerializeField] private float maxVisionRadius = 11f;
    [Tooltip("들리는 범위의 가장자리에서 소리가 서서히 작아지는 구간의 폭")]
    [SerializeField] private float edgeFadeWidth = 1.0f;
    [Tooltip("목소리를 막는 벽 레이어(시야를 막는 레이어와 같게 둔다)")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("거리감")]
    [Tooltip("이 거리 안에서는 줄어들지 않고 그대로 들린다")]
    [SerializeField] private float fullVolumeDistance = 2f;
    [Tooltip("들리는 범위의 끝(가장자리 구간 직전)에서 줄어드는 음량(dB, 음수). 0이면 거리감 없이 범위 안은 전부 같은 크기로 들린다")]
    [SerializeField] private float farAttenuationDb = -12f;

    [Header("유령")]
    [Tooltip("유령이 생존자의 목소리를 들을 수 있는 거리")]
    [SerializeField] private float ghostHearDistance = 3.5f;
    [SerializeField] private float ghostEdgeFadeWidth = 0.8f;

    [Header("음량 변화 속도(dB/초)")]
    [SerializeField] private float fadeInSpeed = 200f;
    [SerializeField] private float fadeOutSpeed = 300f;

    // 참가자별 현재 감쇠량(0 = 그대로, MinVolume = 무음)
    private readonly Dictionary<string, float> _attenuation = new();
    // 참가자별로 Vivox에 마지막으로 보낸 음량. 값이 같으면 다시 보내지 않는다.
    // 매 틱 보내면 채널을 옮기는 순간 처리 중이던 요청이 "Target Object Does Not Exist"로 실패해 에러가 찍혔다.
    // 같은 사람이 채널에 다시 들어오면 참가자 객체가 새로 생기므로 객체 단위로 기억한다
    private readonly Dictionary<VivoxParticipant, int> _appliedVolume = new();
    private readonly HashSet<VivoxParticipant> _seenParticipants = new();
    private readonly List<VivoxParticipant> _removeParticipantBuffer = new();
    private readonly Dictionary<string, GamePlayer> _playersByVoiceId = new();
    private readonly HashSet<string> _seen = new();
    private readonly List<string> _removeBuffer = new();

    private Coroutine _loop;

    private void Awake()
    {
        if (localPlayer == null) localPlayer = GetComponentInParent<GamePlayer>();

        // 다른 플레이어의 오브젝트에서는 돌지 않는다
        enabled = false;
    }

    private void OnEnable()
    {
        if (_loop == null) _loop = StartCoroutine(Loop());
    }

    private void OnDisable()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
        _attenuation.Clear();
        _appliedVolume.Clear();
    }

    private IEnumerator Loop()
    {
        var wait = new WaitForSecondsRealtime(tickInterval);

        while (true)
        {
            try
            {
                Tick();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[InGameVoiceProximity] Tick exception: {e.Message}");
            }

            yield return wait;
        }
    }

    private void Tick()
    {
        if (localPlayer == null || !localPlayer.isLocalPlayer) return;

        var voice = VoiceManager.Instance;
        if (voice == null || !voice.IsLoggedIn || VivoxService.Instance == null) return;

        var context = voice.CurrentContext;
        if (context != VoiceManager.VoiceContext.GameplayAlive &&
            context != VoiceManager.VoiceContext.GameplayDead)
            return;

        // 채널을 옮기는 중(사망, 결과 화면)에는 건드리지 않는다. 떠나는 채널의 참가자에 음량을 적용하면 Vivox 에러가 난다
        if (voice.DesiredContext != context) return;

        // 생존자 채널만 여기서 조절한다. 유령 채널은 VoiceManager가 개별 음량 그대로 둔다
        string aliveChannel = null;
        foreach (var name in VivoxService.Instance.ActiveChannels.Keys)
        {
            if (VoiceManager.IsProximityChannel(name))
            {
                aliveChannel = name;
                break;
            }
        }
        if (aliveChannel == null) return;
        if (!VivoxService.Instance.ActiveChannels.TryGetValue(aliveChannel, out var participants)) return;

        RebuildPlayerMap();

        bool iAmGhost = !localPlayer.isAlive || context == VoiceManager.VoiceContext.GameplayDead;
        Vector2 myPosition = localPlayer.transform.position;
        float hearRange = iAmGhost ? ghostHearDistance : (minVisionRadius + maxVisionRadius) * 0.5f;
        float fadeWidth = iAmGhost ? ghostEdgeFadeWidth : edgeFadeWidth;

        _seen.Clear();
        _seenParticipants.Clear();

        foreach (var participant in participants)
        {
            if (participant == null || participant.IsSelf || string.IsNullOrEmpty(participant.PlayerId)) continue;

            _seen.Add(participant.PlayerId);
            _seenParticipants.Add(participant);

            float target = VoiceManager.MinVolume;

            // 누구인지 알 수 없는 참가자, 죽은 참가자(유령은 생존자 채널에 말하지 않는다)는 들리지 않게 한다.
            // 이 경우는 서서히 줄이지 않고 바로 끈다(사망이 확정된 뒤의 말이 새지 않게)
            bool canSpeakHere = _playersByVoiceId.TryGetValue(participant.PlayerId, out var speaker) && speaker != null && speaker.isAlive;
            if (!canSpeakHere)
                _attenuation[participant.PlayerId] = VoiceManager.MinVolume;

            if (canSpeakHere)
            {
                Vector2 speakerPosition = speaker.transform.position;
                float distance = Vector2.Distance(myPosition, speakerPosition);

                // 유령은 벽을 통과하므로 벽에 가려도 들린다
                if (distance < hearRange && (iAmGhost || !IsBlockedByWall(myPosition, speakerPosition)))
                    target = ComputeDistanceAttenuation(distance, hearRange, fadeWidth);
            }

            ApplyParticipantVolume(participant, target);
        }

        // 채널에서 나간 참가자의 기록 정리
        _removeBuffer.Clear();
        foreach (var id in _attenuation.Keys)
            if (!_seen.Contains(id)) _removeBuffer.Add(id);
        foreach (var id in _removeBuffer)
            _attenuation.Remove(id);

        _removeParticipantBuffer.Clear();
        foreach (var p in _appliedVolume.Keys)
            if (!_seenParticipants.Contains(p)) _removeParticipantBuffer.Add(p);
        foreach (var p in _removeParticipantBuffer)
            _appliedVolume.Remove(p);
    }

    private void RebuildPlayerMap()
    {
        _playersByVoiceId.Clear();

        var players = GamePlayer.AllPlayers;
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (player == null || string.IsNullOrEmpty(player.voiceId)) continue;

            _playersByVoiceId[player.voiceId] = player;
        }
    }

    private bool IsBlockedByWall(Vector2 from, Vector2 to)
    {
        if (obstacleMask.value == 0) return false;

        return Physics2D.Linecast(from, to, obstacleMask).collider != null;
    }

    // 가까우면 0(그대로), 멀어질수록 farAttenuationDb까지 줄고, 가장자리 구간에서 MinVolume(무음)까지 서서히 줄어든다
    private float ComputeDistanceAttenuation(float distance, float range, float edgeWidth)
    {
        if (distance >= range) return VoiceManager.MinVolume;

        float fadeStart = Mathf.Max(0f, range - Mathf.Max(0f, edgeWidth));
        float nearDistance = Mathf.Min(fullVolumeDistance, fadeStart);
        float farDb = fadeStart > nearDistance ? Mathf.Min(0f, farAttenuationDb) : 0f;

        if (distance <= fadeStart)
            return Mathf.Lerp(0f, farDb, Mathf.InverseLerp(nearDistance, fadeStart, distance));

        return Mathf.Lerp(farDb, VoiceManager.MinVolume, Mathf.InverseLerp(fadeStart, range, distance));
    }

    private void ApplyParticipantVolume(VivoxParticipant participant, float target)
    {
        string id = participant.PlayerId;

        if (!_attenuation.TryGetValue(id, out float current))
            current = VoiceManager.MinVolume;

        float speed = target > current ? fadeInSpeed : fadeOutSpeed;
        float next = Mathf.MoveTowards(current, target, speed * tickInterval);
        _attenuation[id] = next;

        // 내가 그 사람에게 정해 둔 개별 음량에 거리 감쇠를 더한다
        float volume = VoiceManager.GetParticipantBaseVolume(id) + next;
        int rounded = Mathf.RoundToInt(Mathf.Clamp(volume, VoiceManager.MinVolume, VoiceManager.MaxBoostVolume));

        if (_appliedVolume.TryGetValue(participant, out int applied) && applied == rounded) return;

        _appliedVolume[participant] = rounded;
        participant.SetLocalVolume(rounded);
    }
}
