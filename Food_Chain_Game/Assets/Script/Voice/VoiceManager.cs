using Mirror;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using TMPro;

// 음성 채팅(Vivox)의 로그인, 채널 전환, 마이크 상태를 한 곳에서 관리한다.
//  - 로비: lobby_방코드 (거리와 상관없이 전원이 들림)
//  - 게임 생존자: alive_방코드 (누가 들리는지는 InGameVoiceProximity가 듣는 쪽에서 음량으로 조절)
//  - 게임 유령: dead_방코드에 말하고, alive_방코드는 듣기만 한다
public class VoiceManager : MonoBehaviour
{
    public static VoiceManager Instance { get; private set; }
    public enum VoiceContext { None, Lobby, GameplayAlive, GameplayDead }
    public enum MicMode { Open, PushToTalk }

    public static event Action OnVoiceChannelJoined;

    public event Action OnDeviceListsChanged;
    // 마이크 음소거/방식/송신 여부가 바뀌었을 때
    public event Action OnMicStateChanged;
    // 내 음성 ID가 정해졌을 때(로그인 완료)
    public event Action OnLocalVoiceIdReady;

    public IReadOnlyList<VivoxInputDevice> CachedInputs => _cachedInputs;
    public IReadOnlyList<VivoxOutputDevice> CachedOutputs => _cachedOutputs;

    private readonly List<VivoxInputDevice> _cachedInputs = new();
    private readonly List<VivoxOutputDevice> _cachedOutputs = new();
    private bool _serviceEventsHooked;

    public const string ParticipantVolPrefPrefix = "Vivox_ParticipantVol_";
    public const string ParticipantMutePrefPrefix = "Vivox_ParticipantMute_";
    public const int DefaultParticipantVolume = 70;
    // Vivox 참가자 음량 범위(dB). MinVolume은 사실상 무음
    public const int MinVolume = -50;
    public const int MaxBoostVolume = 10;
    public const int MaxVoiceIdLength = 64;

    private const string PREF_MIC_MODE = "Voice_MicMode";
    private const string PREF_MIC_MUTED = "Voice_MicMuted";
    private const string LobbyPrefix = "lobby_";
    private const string AlivePrefix = "alive_";
    private const string DeadPrefix = "dead_";

    [Header("Mic")]
    [Tooltip("눌러서 말하기 키를 뗀 뒤 마이크가 꺼질 때까지의 시간(초). 말끝이 잘리지 않게 한다")]
    [SerializeField] private float pushToTalkReleaseDelay = 0.2f;
    [Tooltip("죽은 순간부터 말을 못 하는 시간(초). 유령 채널로 옮겨진 뒤에도 이 시간이 지나야 유령끼리 말할 수 있다. 사망 연출(GamePlayUI.deathEffectSeconds)과 같게 둔다")]
    [SerializeField] private float deathSilenceSeconds = 2f;

    [Header("Speaking")]
    [Tooltip("누가 말하고 있는지 다시 확인하는 간격(초)")]
    [SerializeField] private float speakingRefreshInterval = 0.1f;

    [Header("Reconnect")]
    [Tooltip("음성 연결이 요청한 상태와 같은지 확인하는 간격(초)")]
    [SerializeField] private float reconnectCheckInterval = 5f;
    [Tooltip("다시 연결하는 데 실패했을 때 다음 시도까지 기다리는 시간(초). 실패할 때마다 두 배로 늘어난다")]
    [SerializeField] private float reconnectInitialDelay = 2f;
    [Tooltip("다시 연결하는 간격의 상한(초)")]
    [SerializeField] private float reconnectMaxDelay = 30f;

    [Header("Quit")]
    [Tooltip("게임을 끌 때 음성 로그아웃을 기다려 주는 최대 시간(초)")]
    [SerializeField] private float quitLogoutTimeout = 1.5f;

    public bool IsReady { get; private set; }
    public bool IsLoggedIn { get; private set; }

    // 다른 플레이어가 나를 음성 참가자 목록에서 찾을 때 쓰는 ID. 로그인 전에는 비어 있다
    public string LocalVoiceId { get; private set; }

    public string CurrentChannel { get; private set; }
    public VoiceContext CurrentContext { get; private set; } = VoiceContext.None;

    public string ActiveTransmitChannel { get; private set; }
    public IReadOnlyCollection<string> JoinedChannels => _joinedChannels;
    private readonly HashSet<string> _joinedChannels = new();

    public MicMode Mode { get; private set; } = MicMode.Open;
    public bool IsMicMuted { get; private set; }
    // 음성 키는 설정 창의 키 설정에서 바꾼다
    public KeyCode PushToTalkKey => KeySetting.GetKey(KeyAction.VOICE_TALK);
    public KeyCode MuteToggleKey => KeySetting.GetKey(KeyAction.VOICE_MUTE);
    // 지금 내 목소리가 나가고 있는 상태인지(마이크가 열려 있는지)
    public bool IsMicOpen => !IsMicMuted && !_micTestActive && !_transmitBlocked && !IsDeathSilenced && (Mode == MicMode.Open || _pttActive);
    // 죽은 직후라 아직 말할 수 없는 상태인지
    public bool IsDeathSilenced => _deathSilenceUntil > 0f && Time.unscaledTime < _deathSilenceUntil;
    // 가장 마지막으로 요청된 음성 상태(전환이 아직 끝나지 않았을 수 있다)
    public VoiceContext DesiredContext => _desiredContext;

    readonly SemaphoreSlim _initLock = new(1, 1);
    CancellationTokenSource _cts;

    private bool _vivoxInitialized;

    // 채널 전환 요청: 가장 마지막 요청만 적용한다(요청 순서가 뒤섞여도 결과가 같도록)
    private string _desiredRoomCode;
    private VoiceContext _desiredContext = VoiceContext.None;
    private int _desiredVersion;
    private bool _applying;

    // 다시 연결: 요청한 상태와 실제 상태가 다르면(끊김, 입장 실패) 간격을 늘려 가며 계속 시도한다
    private float _nextReconnectCheckTime;
    private float _reconnectDelay;
    private int _reconnectAttempts;

    // 지금 말하고 있는 플레이어의 음성 ID(내가 들어가 있는 채널 기준)
    private readonly HashSet<string> _speakingVoiceIds = new();
    private float _nextSpeakingRefresh;

    private bool _quitting;
    private bool _quitLogoutDone;
    private Task _logoutTask;

    private bool _pttActive;
    private float _pttReleaseTime;
    private bool _micTestActive;
    // 사망이 확정된 순간부터 유령 채널로 옮겨질 때까지 송신을 막는다(그사이 생존자 채널로 말이 새지 않게)
    private bool _transmitBlocked;
    // 죽은 직후 말을 막는 시간이 끝나는 시각(unscaledTime). 0이면 막지 않음
    private float _deathSilenceUntil;
    private bool? _appliedInputMuted;

    // 같은 PC에서 여러 개를 실행했을 때 인스턴스마다 다른 계정으로 로그인하기 위한 번호표
    private static Mutex _instanceSlotMutex;
    private static int _instanceSlot = -1;
    private const int MaxInstanceSlots = 32;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Mode = PlayerPrefs.GetInt(PREF_MIC_MODE, 0) == 1 ? MicMode.PushToTalk : MicMode.Open;
        IsMicMuted = PlayerPrefs.GetInt(PREF_MIC_MUTED, 0) == 1;

        Application.wantsToQuit += OnWantsToQuit;

        _cts = new CancellationTokenSource();
        _ = BootInitAndLoginAsync();
    }

    void OnDestroy()
    {
        if (Instance != this) return;

        Instance = null;
        Application.wantsToQuit -= OnWantsToQuit;
        UnhookServiceEvents();
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    void Update()
    {
        UpdateReconnect();
        UpdateSpeaking();
        UpdateDeathSilence();

        if (!IsLoggedIn) return;

        bool typing = IsTypingText();

        if (!typing && Input.GetKeyDown(MuteToggleKey))
            ToggleMicMuted();

        if (Mode != MicMode.PushToTalk) return;

        if (!typing && Input.GetKey(PushToTalkKey))
            _pttReleaseTime = Time.unscaledTime + pushToTalkReleaseDelay;

        bool active = Time.unscaledTime < _pttReleaseTime;
        if (active != _pttActive)
        {
            _pttActive = active;
            ApplyMicState();
        }
    }

    // 죽은 직후 말을 막는 시간이 끝나면 마이크 상태를 다시 맞춘다(채널 이동이 먼저 끝났어도 이때 풀린다)
    private void UpdateDeathSilence()
    {
        if (_deathSilenceUntil <= 0f || Time.unscaledTime < _deathSilenceUntil) return;

        _deathSilenceUntil = 0f;
        ApplyMicState();
    }

    // 채팅·닉네임 등 글자를 입력하는 중에는 음성 키가 눌린 것으로 치지 않는다
    private static bool IsTypingText()
    {
        if (ChatInputFocus.IsFocused || SettingManager.isKeySetting) return true;

        var eventSystem = EventSystem.current;
        if (eventSystem == null) return false;

        var selected = eventSystem.currentSelectedGameObject;
        if (selected == null) return false;

        var input = selected.GetComponent<TMP_InputField>();
        return input != null && input.isFocused;
    }

    #region 마이크

    public void SetMicMuted(bool muted)
    {
        if (IsMicMuted == muted) return;

        IsMicMuted = muted;

        PlayerPrefs.SetInt(PREF_MIC_MUTED, muted ? 1 : 0);
        PlayerPrefs.Save();

        ApplyMicState();
    }

    public void ToggleMicMuted() => SetMicMuted(!IsMicMuted);

    public void SetMicMode(MicMode mode)
    {
        if (Mode == mode) return;

        Mode = mode;
        _pttActive = false;
        _pttReleaseTime = 0f;

        PlayerPrefs.SetInt(PREF_MIC_MODE, mode == MicMode.PushToTalk ? 1 : 0);
        PlayerPrefs.Save();

        ApplyMicState();
    }

    public void ToggleMicMode()
        => SetMicMode(Mode == MicMode.Open ? MicMode.PushToTalk : MicMode.Open);

    // 설정 창의 마이크 테스트 중에는 내 목소리(와 스피커로 되돌아 나오는 소리)가 다른 플레이어에게 나가지 않게 한다
    public void SetMicTestActive(bool active)
    {
        if (_micTestActive == active) return;

        _micTestActive = active;
        ApplyMicState();
    }

    private void ApplyMicState()
    {
        bool shouldMute = !IsMicOpen;

        if (IsLoggedIn && VivoxService.Instance != null && _appliedInputMuted != shouldMute)
        {
            try
            {
                if (shouldMute) VivoxService.Instance.MuteInputDevice();
                else VivoxService.Instance.UnmuteInputDevice();

                _appliedInputMuted = shouldMute;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VoiceManager] 마이크 상태 적용 실패: {e.Message}");
            }
        }

        OnMicStateChanged?.Invoke();
    }

    #endregion

    #region 음성 ID / 참가자

    // 서버가 클라이언트에게 받은 음성 ID를 검사할 때 쓴다
    public static bool IsValidVoiceId(string voiceId)
    {
        if (string.IsNullOrEmpty(voiceId) || voiceId.Length > MaxVoiceIdLength) return false;

        foreach (char c in voiceId)
        {
            bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '_';
            if (!ok) return false;
        }
        return true;
    }

    // 음성 참가자 ID로 닉네임을 찾는다(플레이어 오브젝트에 동기화된 음성 ID 기준). 없으면 null
    public static string ResolveNickname(string voiceId)
    {
        if (string.IsNullOrEmpty(voiceId)) return null;

        foreach (var identity in NetworkClient.spawned.Values)
        {
            if (identity == null) continue;

            if (identity.TryGetComponent(out GamePlayer gamePlayer))
            {
                if (gamePlayer.voiceId == voiceId) return gamePlayer.nickname;
            }
            else if (identity.TryGetComponent(out RoomPlayer roomPlayer))
            {
                if (roomPlayer.voiceId == voiceId) return roomPlayer.nickname;
            }
        }
        return null;
    }

    // 설정에서 정한 0~100 음량을 Vivox 음량(dB)으로 바꾼다. 70이 기준(0dB)
    public static int ToVivoxVolume(int volume01)
    {
        volume01 = Mathf.Clamp(volume01, 0, 100);

        if (volume01 <= DefaultParticipantVolume)
            return Mathf.RoundToInt(Mathf.Lerp(MinVolume, 0, volume01 / (float)DefaultParticipantVolume));

        float t = (volume01 - DefaultParticipantVolume) / (float)(100 - DefaultParticipantVolume);
        return Mathf.RoundToInt(Mathf.Lerp(0, MaxBoostVolume, t));
    }

    // 내가 이 참가자에게 설정해 둔 개별 음량(dB)
    public static int GetParticipantBaseVolume(string voiceId)
    {
        return ToVivoxVolume(PlayerPrefs.GetInt(ParticipantVolPrefPrefix + voiceId, DefaultParticipantVolume));
    }

    // 게임 중 생존자 채널의 음량은 InGameVoiceProximity가 거리·시야에 따라 정한다.
    // 그 외(로비, 유령 채널)는 개별 음량을 그대로 쓴다
    public static bool IsProximityChannel(string channelName)
    {
        return !string.IsNullOrEmpty(channelName) && channelName.StartsWith(AlivePrefix);
    }

    // 개별 음량을 저장하고 바로 적용한다. 게임 중 생존자 채널은 InGameVoiceProximity가 저장값을 읽어 거리 감쇠와 함께 적용한다
    public void SetParticipantVolume(string voiceId, int volume01)
    {
        if (string.IsNullOrEmpty(voiceId)) return;

        volume01 = Mathf.Clamp(volume01, 0, 100);
        PlayerPrefs.SetInt(ParticipantVolPrefPrefix + voiceId, volume01);

        ForEachParticipant(voiceId, participant =>
        {
            if (!IsProximityChannel(participant.ChannelName))
                participant.SetLocalVolume(ToVivoxVolume(volume01));
        });
    }

    public static GamePlayer FindGamePlayer(string voiceId)
    {
        if (string.IsNullOrEmpty(voiceId)) return null;

        var players = GamePlayer.AllPlayers;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null && players[i].voiceId == voiceId) return players[i];
        }
        return null;
    }

    // 지금 말하고 있는지(표시해도 되는지는 따지지 않는다)
    public bool IsSpeaking(string voiceId)
    {
        return !string.IsNullOrEmpty(voiceId) && _speakingVoiceIds.Contains(voiceId);
    }

    // "말하는 중" 표시(목록의 초록 테두리, 머리 위 아이콘)를 보여 줘도 되는지.
    //  - 로비·결과 화면: 조건 없음
    //  - 게임 중: 내 시야 안에 있는 플레이어만(멀리 있는 사람이 말하는 것까지 보이면 위치·대화 상대가 드러난다).
    //    유령의 말은 유령에게만 보인다
    public bool IsSpeakingVisible(string voiceId)
    {
        if (!IsSpeaking(voiceId)) return false;

        if (CurrentContext != VoiceContext.GameplayAlive && CurrentContext != VoiceContext.GameplayDead)
            return true;

        var speaker = FindGamePlayer(voiceId);
        if (speaker == null) return false;
        if (speaker.isLocalPlayer) return true;

        var local = GamePlayer.LocalPlayer;
        if (local == null) return false;

        if (!speaker.isAlive) return !local.isAlive;

        return speaker.IsInLocalVision;
    }

    private void UpdateSpeaking()
    {
        if (Time.unscaledTime < _nextSpeakingRefresh) return;
        _nextSpeakingRefresh = Time.unscaledTime + speakingRefreshInterval;

        _speakingVoiceIds.Clear();
        if (!IsLoggedIn) return;

        try
        {
            if (VivoxService.Instance == null) return;

            foreach (var channel in VivoxService.Instance.ActiveChannels)
            {
                foreach (var participant in channel.Value)
                {
                    if (participant == null || !participant.SpeechDetected) continue;

                    if (participant.IsSelf)
                    {
                        // 마이크가 닫혀 있으면(음소거, 눌러서 말하기 대기) 말하는 것으로 치지 않는다
                        if (IsMicOpen && !string.IsNullOrEmpty(LocalVoiceId))
                            _speakingVoiceIds.Add(LocalVoiceId);
                    }
                    else if (!string.IsNullOrEmpty(participant.PlayerId))
                    {
                        _speakingVoiceIds.Add(participant.PlayerId);
                    }
                }
            }
        }
        catch (Exception)
        {
            // 채널을 옮기는 중에는 목록이 바뀔 수 있다. 다음 확인 때 다시 읽는다
        }
    }

    // 내가 이 참가자를 음소거해 두었는지(저장값). 채널이 바뀌어도 같은 사람에게 계속 적용된다
    public static bool IsParticipantMutedByMe(string voiceId)
    {
        return !string.IsNullOrEmpty(voiceId) && PlayerPrefs.GetInt(ParticipantMutePrefPrefix + voiceId, 0) == 1;
    }

    // 개별 음소거를 저장하고, 지금 들어가 있는 모든 채널의 그 참가자에게 바로 적용한다
    public void SetParticipantMuted(string voiceId, bool muted)
    {
        if (string.IsNullOrEmpty(voiceId)) return;

        PlayerPrefs.SetInt(ParticipantMutePrefPrefix + voiceId, muted ? 1 : 0);
        PlayerPrefs.Save();

        ForEachParticipant(voiceId, ApplyParticipantMute);
    }

    // 사망이 확정된 플레이어의 목소리를 생존자 채널에서 즉시 끈다(그 플레이어의 채널 이동이 끝나기를 기다리지 않는다)
    public void SilenceInAliveChannelNow(string voiceId)
    {
        if (string.IsNullOrEmpty(voiceId)) return;

        ForEachParticipant(voiceId, participant =>
        {
            if (IsProximityChannel(participant.ChannelName))
                participant.SetLocalVolume(MinVolume);
        });
    }

    private void ForEachParticipant(string voiceId, Action<VivoxParticipant> action)
    {
        if (!IsLoggedIn || VivoxService.Instance == null) return;

        try
        {
            foreach (var channel in VivoxService.Instance.ActiveChannels)
            {
                foreach (var participant in channel.Value)
                {
                    if (participant == null || participant.IsSelf || participant.PlayerId != voiceId) continue;
                    action(participant);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[VoiceManager] 참가자 설정 적용 실패: {e.Message}");
        }
    }

    private static void ApplyParticipantMute(VivoxParticipant participant)
    {
        if (IsParticipantMutedByMe(participant.PlayerId)) participant.MutePlayerLocally();
        else participant.UnmutePlayerLocally();
    }

    private void OnParticipantAdded(VivoxParticipant participant)
    {
        if (participant == null || participant.IsSelf || string.IsNullOrEmpty(participant.PlayerId)) return;

        try
        {
            // 음소거는 채널 종류와 상관없이 저장값을 그대로 적용한다
            if (IsParticipantMutedByMe(participant.PlayerId))
                participant.MutePlayerLocally();

            // 생존자 채널은 무음에서 시작해 들려도 되는 사람만 InGameVoiceProximity가 키운다
            if (IsProximityChannel(participant.ChannelName))
                participant.SetLocalVolume(MinVolume);
            else
                participant.SetLocalVolume(GetParticipantBaseVolume(participant.PlayerId));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[VoiceManager] 참가자 음량 초기화 실패: {e.Message}");
        }
    }

    #endregion

    #region 초기화 / 로그인

    async Task BootInitAndLoginAsync()
    {
        try
        {
            var ct = _cts?.Token ?? CancellationToken.None;

            await EnsureLoggedInAsync(ct);

            RefreshDeviceCache();
            OnDeviceListsChanged?.Invoke();
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Debug.LogError($"[VoiceManager] 시작 시 음성 초기화 실패: {e}");
        }
    }

    // 같은 PC에서 여러 개를 실행하면 익명 로그인 정보가 공유돼 전부 같은 계정이 된다.
    // 실행 순서대로 번호표를 잡아, 두 번째 인스턴스부터는 다른 프로필(=다른 계정)을 쓴다
    private static string GetAuthProfile()
    {
        if (_instanceSlot < 0)
        {
            _instanceSlot = 0;
            for (int i = 0; i < MaxInstanceSlots; i++)
            {
                try
                {
                    var mutex = new Mutex(false, $"FoodChainGame_VoiceInstance_{i}", out bool createdNew);
                    if (createdNew)
                    {
                        _instanceSlotMutex = mutex;
                        _instanceSlot = i;
                        break;
                    }
                    mutex.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[VoiceManager] 인스턴스 번호 확인 실패: {e.Message}");
                    break;
                }
            }
        }

        return _instanceSlot == 0 ? "default" : $"instance{_instanceSlot}";
    }

    async Task EnsureLoggedInAsync(CancellationToken ct)
    {
        await _initLock.WaitAsync(ct);
        try
        {
            string profile = GetAuthProfile();

            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync(new InitializationOptions().SetProfile(profile));

            ct.ThrowIfCancellationRequested();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                if (AuthenticationService.Instance.Profile != profile)
                    AuthenticationService.Instance.SwitchProfile(profile);

                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            ct.ThrowIfCancellationRequested();

            if (!_vivoxInitialized)
            {
                await VivoxService.Instance.InitializeAsync();
                _vivoxInitialized = true;
            }

            IsReady = true;
            HookServiceEventsOnce();

            ct.ThrowIfCancellationRequested();

            if (!VivoxService.Instance.IsLoggedIn)
            {
                // 표시 이름은 로그인할 때 한 번만 정해져서 닉네임을 담을 수 없다.
                // 누가 누구인지는 플레이어 오브젝트에 동기화한 음성 ID로 찾는다(ResolveNickname)
                await VivoxService.Instance.LoginAsync(new LoginOptions
                {
                    DisplayName = "Player",
                    EnableTTS = false
                });
            }

            bool wasLoggedIn = IsLoggedIn;
            IsLoggedIn = true;

            string voiceId = VivoxService.Instance.SignedInPlayerId;
            if (string.IsNullOrEmpty(voiceId))
                voiceId = AuthenticationService.Instance.PlayerId;

            bool idChanged = LocalVoiceId != voiceId;
            LocalVoiceId = voiceId;

            if (!wasLoggedIn)
            {
                _appliedInputMuted = null;
                ApplyMicState();
                Debug.Log($"[VoiceManager] 로그인 완료. profile={profile}, voiceId={LocalVoiceId}");
            }

            if (idChanged && !string.IsNullOrEmpty(LocalVoiceId))
                OnLocalVoiceIdReady?.Invoke();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception e)
        {
            LogVoiceFailure("음성 로그인 실패", e);
        }
        finally
        {
            _initLock.Release();
        }
    }

    private void OnVivoxLoggedOut()
    {
        IsLoggedIn = false;
        _appliedInputMuted = null;
        _joinedChannels.Clear();
        CurrentChannel = null;
        ActiveTransmitChannel = null;
        CurrentContext = VoiceContext.None;

        // 방에 있는 중에 끊긴 것이면 바로 다시 연결을 시도한다(UpdateReconnect)
        _nextReconnectCheckTime = 0f;
    }

    // 다시 연결을 시도하는 동안에는 같은 실패가 반복되므로, 첫 실패만 에러로 남기고 이후는 경고 한 줄로 줄인다
    private void LogVoiceFailure(string what, Exception e)
    {
        if (_reconnectAttempts <= 1) Debug.LogError($"[VoiceManager] {what}: {e}");
        else Debug.LogWarning($"[VoiceManager] {what}(다시 연결 시도 {_reconnectAttempts}회째): {e.Message}");
    }

    #endregion

    #region 채널 전환

    // 로비 음성: 거리와 상관없이 방의 모든 플레이어가 서로 들린다
    public void JoinLobby(string roomCode) => RequestContext(roomCode, VoiceContext.Lobby);

    // 결과 화면: 생존/사망 구분 없이 방의 모든 플레이어가 서로 들린다(로비 채널로 모인다)
    public void EnterResult(string roomCode) => RequestContext(roomCode, VoiceContext.Lobby);

    // 게임 음성: 생존자는 생존자 채널, 유령은 유령 채널에 말한다
    public void EnterGameplay(string roomCode, bool isAlive)
        => RequestContext(roomCode, isAlive ? VoiceContext.GameplayAlive : VoiceContext.GameplayDead);

    public void LeaveAllChannels() => RequestContext(null, VoiceContext.None);

    private void RequestContext(string roomCode, VoiceContext context)
    {
        if (context != VoiceContext.None && string.IsNullOrWhiteSpace(roomCode))
        {
            Debug.LogWarning($"[VoiceManager] 방 코드가 없어 음성 채널 전환({context})을 건너뜁니다.");
            return;
        }

        // 살아 있다가 죽은 경우: 죽은 순간 바로 말을 못 하게 한다.
        // 유령 채널로 옮겨질 때까지(생존자 채널로 새지 않게) + 죽은 뒤 deathSilenceSeconds 동안(사망 연출 중) 둘 다 끝나야 다시 말할 수 있다
        if (context == VoiceContext.GameplayDead && _desiredContext == VoiceContext.GameplayAlive && !_transmitBlocked)
        {
            _transmitBlocked = true;
            _deathSilenceUntil = Time.unscaledTime + Mathf.Max(0f, deathSilenceSeconds);
            ApplyMicState();
        }
        else if (context != VoiceContext.GameplayDead && _deathSilenceUntil > 0f)
        {
            // 결과 화면·로비로 넘어가면 바로 전원 대화로 돌아간다
            _deathSilenceUntil = 0f;
            ApplyMicState();
        }

        _desiredRoomCode = roomCode;
        _desiredContext = context;
        _desiredVersion++;

        if (!_applying)
            _ = ApplyDesiredContextLoopAsync();
    }

    // 전환 도중에 새 요청이 오면, 진행 중인 전환이 끝난 뒤 마지막 요청으로 다시 맞춘다
    private async Task ApplyDesiredContextLoopAsync()
    {
        _applying = true;
        try
        {
            int appliedVersion = -1;
            while (appliedVersion != _desiredVersion)
            {
                appliedVersion = _desiredVersion;
                var ct = _cts?.Token ?? CancellationToken.None;
                await ApplyContextAsync(_desiredRoomCode, _desiredContext, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            LogVoiceFailure("음성 채널 전환 실패", e);
        }
        finally
        {
            _applying = false;
        }
    }

    // 요청한 음성 상태(로비/게임 채널)와 실제 상태가 다르면 다시 맞춘다.
    // 음성 서버와의 연결이 끊기거나 채널 입장에 실패해도, 방에 있는 동안에는 계속 다시 시도한다
    private void UpdateReconnect()
    {
        if (_quitting || _applying || _desiredContext == VoiceContext.None) return;
        if (Time.unscaledTime < _nextReconnectCheckTime) return;

        if (IsDesiredStateActive())
        {
            _reconnectDelay = 0f;
            _reconnectAttempts = 0;
            _nextReconnectCheckTime = Time.unscaledTime + reconnectCheckInterval;
            return;
        }

        _reconnectAttempts++;
        _reconnectDelay = _reconnectDelay <= 0f
            ? reconnectInitialDelay
            : Mathf.Min(_reconnectDelay * 2f, reconnectMaxDelay);
        _nextReconnectCheckTime = Time.unscaledTime + _reconnectDelay;

        Debug.LogWarning($"[VoiceManager] 음성 채널에 연결돼 있지 않아 다시 연결합니다. (시도 {_reconnectAttempts}회째, 실패하면 {_reconnectDelay:0}초 뒤 재시도)");
        _ = ApplyDesiredContextLoopAsync();
    }

    private bool IsDesiredStateActive()
    {
        if (!IsLoggedIn || CurrentContext != _desiredContext) return false;

        try
        {
            if (VivoxService.Instance == null || !VivoxService.Instance.IsLoggedIn) return false;

            var wanted = GetWantedChannels(_desiredRoomCode, _desiredContext);
            if (wanted.Count == 0 || CurrentChannel != wanted[0]) return false;

            foreach (var channelName in wanted)
            {
                if (!VivoxService.Instance.ActiveChannels.ContainsKey(channelName)) return false;
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // 말하는 채널을 맨 앞에 둔다
    private static List<string> GetWantedChannels(string roomCode, VoiceContext context)
    {
        var wanted = new List<string>();
        switch (context)
        {
            case VoiceContext.Lobby:
                wanted.Add(LobbyPrefix + roomCode);
                break;
            case VoiceContext.GameplayAlive:
                wanted.Add(AlivePrefix + roomCode);
                break;
            case VoiceContext.GameplayDead:
                wanted.Add(DeadPrefix + roomCode);
                wanted.Add(AlivePrefix + roomCode);
                break;
        }
        return wanted;
    }

    private async Task ApplyContextAsync(string roomCode, VoiceContext context, CancellationToken ct)
    {
        if (context == VoiceContext.None)
        {
            await LeaveChannelsExceptAsync(null);
            SetCurrent(VoiceContext.None, null);
            _transmitBlocked = false;
            return;
        }

        await EnsureLoggedInAsync(ct);
        if (!IsLoggedIn || VivoxService.Instance == null) return;

        var wanted = GetWantedChannels(roomCode, context);
        string transmitChannel = wanted[0];

        await LeaveChannelsExceptAsync(wanted);

        for (int i = 0; i < wanted.Count; i++)
            await JoinGroupChannelAsync(wanted[i], makeActive: i == 0);

        // 유령은 생존자 채널을 듣기만 해야 하므로 말하는 채널을 하나로 고정한다
        if (VivoxService.Instance.ActiveChannels.ContainsKey(transmitChannel))
            await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.Single, transmitChannel);

        SetCurrent(context, transmitChannel);
        ApplyInitialParticipantVolumes();

        // 말하는 채널이 정해졌으므로(유령은 유령 채널로만 나간다) 사망 직후의 송신 차단을 푼다
        _transmitBlocked = false;

        _appliedInputMuted = null;
        ApplyMicState();

        OnVoiceChannelJoined?.Invoke();
    }

    private void SetCurrent(VoiceContext context, string transmitChannel)
    {
        CurrentContext = context;
        CurrentChannel = transmitChannel;
        ActiveTransmitChannel = transmitChannel;
    }

    // 이미 들어와 있던 참가자들의 음량을 채널 종류에 맞게 맞춘다(새로 들어오는 참가자는 OnParticipantAdded)
    private void ApplyInitialParticipantVolumes()
    {
        if (VivoxService.Instance == null) return;

        foreach (var channel in VivoxService.Instance.ActiveChannels)
        {
            foreach (var participant in channel.Value)
            {
                if (participant == null || participant.IsSelf || string.IsNullOrEmpty(participant.PlayerId)) continue;

                ApplyParticipantMute(participant);

                // 생존자 채널의 음량은 InGameVoiceProximity가 계속 조절하므로 건드리지 않는다
                if (!IsProximityChannel(channel.Key))
                    participant.SetLocalVolume(GetParticipantBaseVolume(participant.PlayerId));
            }
        }
    }

    private async Task JoinGroupChannelAsync(string channelName, bool makeActive)
    {
        if (string.IsNullOrEmpty(channelName) || VivoxService.Instance == null) return;

        if (!VivoxService.Instance.ActiveChannels.ContainsKey(channelName))
        {
            await VivoxService.Instance.JoinGroupChannelAsync(
                channelName,
                ChatCapability.AudioOnly,
                new ChannelOptions { MakeActiveChannelUponJoining = makeActive }
            );
        }

        _joinedChannels.Add(channelName);
    }

    // keep에 없는 채널에서 모두 나간다. keep이 null이면 전부 나간다
    private async Task LeaveChannelsExceptAsync(List<string> keep)
    {
        if (!IsLoggedIn || VivoxService.Instance == null)
        {
            _joinedChannels.Clear();
            return;
        }

        var toLeave = new HashSet<string>(_joinedChannels);
        foreach (var name in VivoxService.Instance.ActiveChannels.Keys)
            toLeave.Add(name);

        if (keep != null)
            toLeave.ExceptWith(keep);

        foreach (var channelName in toLeave)
        {
            if (VivoxService.Instance.ActiveChannels.ContainsKey(channelName))
            {
                try
                {
                    await VivoxService.Instance.LeaveChannelAsync(channelName);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[VoiceManager] 채널 나가기 실패({channelName}): {e.Message}");
                }
            }

            _joinedChannels.Remove(channelName);
        }
    }

    public void LeaveAndLogout()
        => _ = LeaveAndLogoutAsync();

    // 여러 곳(종료 버튼, 창 닫기)에서 겹쳐 불려도 로그아웃은 한 번만 진행한다
    public Task LeaveAndLogoutAsync()
    {
        if (_logoutTask == null || _logoutTask.IsCompleted)
            _logoutTask = LeaveAndLogoutCoreAsync();

        return _logoutTask;
    }

    private async Task LeaveAndLogoutCoreAsync()
    {
        try
        {
            // 진행 중인 채널 전환이 로그아웃 뒤에 다시 들어가지 않도록 요청도 비운다
            _desiredRoomCode = null;
            _desiredContext = VoiceContext.None;
            _desiredVersion++;

            await LeaveChannelsExceptAsync(null);
            SetCurrent(VoiceContext.None, null);

            if (IsLoggedIn && VivoxService.Instance != null)
                await VivoxService.Instance.LogoutAsync();

            IsLoggedIn = false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[VoiceManager] 로그아웃 실패: {e}");
        }
    }

    // 창을 닫아 게임을 끌 때: 음성 로그아웃이 끝날 때까지 종료를 잠깐 미룬다.
    // (그냥 꺼지면 음성 서버가 알아챌 때까지 다른 플레이어의 목록에 한동안 남는다)
    private bool OnWantsToQuit()
    {
        if (_quitLogoutDone || !IsLoggedIn) return true;

        if (!_quitting)
        {
            _quitting = true;
            _ = LogoutThenQuitAsync();
        }
        return false;
    }

    private async Task LogoutThenQuitAsync()
    {
        try
        {
            int timeoutMs = Mathf.Max(0, Mathf.RoundToInt(quitLogoutTimeout * 1000f));
            await Task.WhenAny(LeaveAndLogoutAsync(), Task.Delay(timeoutMs));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[VoiceManager] 종료 중 음성 로그아웃 실패: {e.Message}");
        }

        _quitLogoutDone = true;
        Application.Quit();
    }

    // 종료를 미룰 수 없는 경우(에디터의 플레이 종료 등)에도 로그아웃 요청은 보낸다
    void OnApplicationQuit()
    {
        _quitting = true;

        if (!_quitLogoutDone && IsLoggedIn)
            LeaveAndLogout();
    }

    #endregion

    #region 장치

    void HookServiceEventsOnce()
    {
        if (_serviceEventsHooked) return;
        if (VivoxService.Instance == null) return;

        VivoxService.Instance.AvailableInputDevicesChanged += OnAvailableDevicesChanged;
        VivoxService.Instance.AvailableOutputDevicesChanged += OnAvailableDevicesChanged;
        VivoxService.Instance.ParticipantAddedToChannel += OnParticipantAdded;
        VivoxService.Instance.LoggedOut += OnVivoxLoggedOut;
        _serviceEventsHooked = true;
    }

    void UnhookServiceEvents()
    {
        if (!_serviceEventsHooked) return;

        try
        {
            if (VivoxService.Instance != null)
            {
                VivoxService.Instance.AvailableInputDevicesChanged -= OnAvailableDevicesChanged;
                VivoxService.Instance.AvailableOutputDevicesChanged -= OnAvailableDevicesChanged;
                VivoxService.Instance.ParticipantAddedToChannel -= OnParticipantAdded;
                VivoxService.Instance.LoggedOut -= OnVivoxLoggedOut;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[VoiceManager] 이벤트 해제 실패: {e.Message}");
        }
        _serviceEventsHooked = false;
    }

    void OnAvailableDevicesChanged()
    {
        RefreshDeviceCache();
        OnDeviceListsChanged?.Invoke();
    }

    void RefreshDeviceCache()
    {
        if (!_vivoxInitialized || VivoxService.Instance == null) return;

        _cachedInputs.Clear();
        _cachedOutputs.Clear();

        _cachedInputs.AddRange(VivoxService.Instance.AvailableInputDevices);
        _cachedOutputs.AddRange(VivoxService.Instance.AvailableOutputDevices);
    }

    public void RefreshDevicesForUI()
    {
        if (!_vivoxInitialized || VivoxService.Instance == null)
        {
            Debug.LogWarning("[VoiceManager] RefreshDevicesForUI skipped: Vivox not initialized yet.");
            return;
        }

        RefreshDeviceCache();
        OnDeviceListsChanged?.Invoke();
    }

    #endregion
}
