using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Vivox;
using UnityEngine;

public class InGameVoiceProximity : MonoBehaviour
{
    [SerializeField] private GamePlayer localPlayer;
    [SerializeField] private float tickInterval = 0.10f;

    [SerializeField] private float edgeFadeWidth = 1.0f;
    [SerializeField] private LayerMask obstacleMask;

    [SerializeField] private float ghostHearDistance = 3.5f;
    [SerializeField] private float ghostEdgeFadeWidth = 0.8f;

    [SerializeField] private float fadeInSpeed = 200f;
    [SerializeField] private float fadeOutSpeed = 300f;

    private readonly Dictionary<(string channel, string playerId), float> _volCache = new();

    private Coroutine _loop;

    private void Reset()
    {
        localPlayer = GetComponent<GamePlayer>();
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
        _volCache.Clear();
    }

    private IEnumerator Loop()
    {
        var wait = new WaitForSeconds(tickInterval);

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
        if (VoiceManager.Instance == null || !VoiceManager.Instance.IsLoggedIn) return;
        if (VivoxService.Instance == null) return;

        var ctx = VoiceManager.Instance.CurrentContext;
        if (ctx != VoiceManager.VoiceContext.GameplayAlive &&
            ctx != VoiceManager.VoiceContext.GameplayDead)
            return;

        string roomCode = RoomSessionData.CurrentRoomCode;
        if (string.IsNullOrEmpty(roomCode)) return;

        string aliveCh = $"alive_{roomCode}";
        string deadCh = $"dead_{roomCode}";

        if (ctx == VoiceManager.VoiceContext.GameplayAlive) ApplyAliveHearing(aliveCh);      
        else ApplyGhostHearing(deadCh, aliveCh);             
    }

    private void ApplyAliveHearing(string aliveChannel)
    {
        if (!VivoxService.Instance.ActiveChannels.TryGetValue(aliveChannel, out var participants))
            return;

        bool isNight = GameMamager.Instance != null && GameMamager.Instance.IsNightPhase;
        float myVision = GetVisionForAnimal(localPlayer, isNight);

        var seen = new HashSet<(string ch, string pid)>();

        foreach (var p in participants)
        {
            if (p == null || string.IsNullOrEmpty(p.PlayerId) || p.IsSelf) continue;

            var key = (aliveChannel, p.PlayerId);
            seen.Add(key);

            if (!VoiceManager.TryParseVivoxDisplayName(p.DisplayName, out _, out uint netId))
            {
                ApplyParticipantVolume(aliveChannel, p, target: -50, dt: tickInterval);
                continue;
            }

            var targetPlayer = FindGamePlayerByNetId(netId);
            if (targetPlayer == null)
            {
                ApplyParticipantVolume(aliveChannel, p, target: -50, dt: tickInterval);
                continue;
            }

            bool visible = IsVisibleByMe(targetPlayer, myVision);
            if (!visible)
            {
                ApplyParticipantVolume(aliveChannel, p, target: -50, dt: tickInterval);
                continue;
            }

            float dist = Vector2.Distance(localPlayer.transform.position, targetPlayer.transform.position);
            float targetVol = ComputeEdgeFadeVolume(dist, myVision, edgeFadeWidth); // 0 ~ -50

            ApplyParticipantVolume(aliveChannel, p, targetVol, tickInterval);
        }

        CleanupCache(seen);
    }

    private bool IsVisibleByMe(GamePlayer target, float vision)
    {
        if (target == null) return false;

        Vector2 a = localPlayer.transform.position;
        Vector2 b = target.transform.position;

        float dist = Vector2.Distance(a, b);
        if (dist > vision) return false;

        if (obstacleMask.value == 0) return true;

        var hit = Physics2D.Linecast(a, b, obstacleMask);
        return hit.collider == null;
    }
    private void ApplyGhostHearing(string deadChannel, string aliveChannel)
    {
        var seen = new HashSet<(string ch, string pid)>();

        if (VivoxService.Instance.ActiveChannels.TryGetValue(deadChannel, out var deadParticipants))
        {
            foreach (var p in deadParticipants)
            {
                if (p == null || string.IsNullOrEmpty(p.PlayerId) || p.IsSelf) continue;

                var key = (deadChannel, p.PlayerId);
                seen.Add(key);

                ApplyParticipantVolume(deadChannel, p, target: 0, dt: tickInterval);
            }
        }

        if (VivoxService.Instance.ActiveChannels.TryGetValue(aliveChannel, out var aliveParticipants))
        {
            foreach (var p in aliveParticipants)
            {
                if (p == null || string.IsNullOrEmpty(p.PlayerId) || p.IsSelf) continue;

                var key = (aliveChannel, p.PlayerId);
                seen.Add(key);

                if (!VoiceManager.TryParseVivoxDisplayName(p.DisplayName, out _, out uint netId))
                {
                    ApplyParticipantVolume(aliveChannel, p, target: -50, dt: tickInterval);
                    continue;
                }

                var targetPlayer = FindGamePlayerByNetId(netId);
                if (targetPlayer == null)
                {
                    ApplyParticipantVolume(aliveChannel, p, target: -50, dt: tickInterval);
                    continue;
                }

                float dist = Vector2.Distance(localPlayer.transform.position, targetPlayer.transform.position);
                float targetVol = ComputeEdgeFadeVolume(dist, ghostHearDistance, ghostEdgeFadeWidth);

                ApplyParticipantVolume(aliveChannel, p, targetVol, tickInterval);
            }
        }

        CleanupCache(seen);
    }
    private float ComputeEdgeFadeVolume(float dist, float range, float edgeWidth)
    {
        if (dist >= range) return -50f;

        if (edgeWidth <= 0.001f) return 0f;

        float startFade = Mathf.Max(0f, range - edgeWidth);
        if (dist <= startFade) return 0f;

        float t = Mathf.InverseLerp(startFade, range, dist); // 0..1
        return Mathf.Lerp(0f, -50f, t);
    }

    private void ApplyParticipantVolume(string channelName, VivoxParticipant p, float target, float dt)
    {
        var cacheKey = (channelName, p.PlayerId);

        if (!_volCache.TryGetValue(cacheKey, out float cur))
            cur = -50f;

        float speed = (target > cur) ? fadeInSpeed : fadeOutSpeed;
        float next = Mathf.MoveTowards(cur, target, speed * dt);

        _volCache[cacheKey] = next;

        // Vivox 로컬 볼륨 범위: 보통 -50 ~ +10 정도로 운용
        p.SetLocalVolume(Mathf.RoundToInt(Mathf.Clamp(next, -50f, 10f)));
    }

    private void CleanupCache(HashSet<(string ch, string pid)> seen)
    {
        var keys = _volCache.Keys.ToList();
        foreach (var k in keys)
            if (!seen.Contains(k))
                _volCache.Remove(k);
    }

    private float GetVisionForAnimal(GamePlayer player, bool isNight)
    {
        if (!isNight) return 11f;
        if (player.isPredator) return 6f;
        if (player.animalType == AnimalType.Squirrel) return 11f;
        return 8f;
    }

    private GamePlayer FindGamePlayerByNetId(uint netId)
    {
        if (NetworkClient.spawned.TryGetValue(netId, out var identity))
            return identity != null ? identity.GetComponent<GamePlayer>() : null;

        return null;
    }
    //private float ComputeTargetVolume(float dist, float vision)
    //{
    //    if (dist >= vision) return -50f;

    //    if (edgeFadeWidth <= 0.001f) return 0f;

    //    float startFade = Mathf.Max(0f, vision - edgeFadeWidth);
    //    if (dist <= startFade) return 0f;

    //    float t = Mathf.InverseLerp(startFade, vision, dist); // 0..1
    //    return Mathf.Lerp(0f, -50f, t);
    //}

    //private void ApplyParticipantVolume(VivoxParticipant p, float target, float dt)
    //{
    //    string playerId = p.PlayerId;

    //    if (!_volByPlayerId.TryGetValue(playerId, out float cur))
    //        cur = -50f;

    //    float speed = (target > cur) ? fadeInSpeed : fadeOutSpeed;
    //    float next = Mathf.MoveTowards(cur, target, speed * dt);

    //    _volByPlayerId[playerId] = next;

    //    p.SetLocalVolume(Mathf.RoundToInt(Mathf.Clamp(next, -50f, 10f)));
    //}

    //private float GetVisionForAnimal(GamePlayer player, bool isNight)
    //{
    //    if (!isNight) return 11f;
    //    if (player.isPredator) return 6f;
    //    if (player.animalType == AnimalType.Squirrel) return 11f;
    //    else return 8f;
    //}

    //private GamePlayer FindGamePlayerByNetId(uint netId)
    //{
    //    if (NetworkClient.spawned.TryGetValue(netId, out var identity))
    //        return identity != null ? identity.GetComponent<GamePlayer>() : null;
    //    return null;
    //}
}
