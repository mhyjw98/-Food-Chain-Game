using System.Collections;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Edgegap;

public class VoiceManager : MonoBehaviour
{
    public static VoiceManager Instance { get; private set; }
    public enum VoiceContext { None, Lobby, GameplayAlive, GameplayDead }

    public static event Action OnVoiceChannelJoined;

    public event Action OnDeviceListsChanged;
    public IReadOnlyList<VivoxInputDevice> CachedInputs => _cachedInputs;
    public IReadOnlyList<VivoxOutputDevice> CachedOutputs => _cachedOutputs;

    private readonly List<VivoxInputDevice> _cachedInputs = new();
    private readonly List<VivoxOutputDevice> _cachedOutputs = new();
    private bool _deviceEventsHooked;

    public const string ParticipantVolPrefPrefix = "Vivox_ParticipantVol_";
    public bool IsReady { get; private set; }
    public bool IsLoggedIn { get; private set; }

    public string CurrentChannel { get; private set; }
    public VoiceContext CurrentContext { get; private set; } = VoiceContext.None;

    public string ActiveTransmitChannel { get; private set; }
    public IReadOnlyCollection<string> JoinedChannels => _joinedChannels;
    private readonly HashSet<string> _joinedChannels = new();

    readonly SemaphoreSlim _initLock = new(1, 1);
    readonly SemaphoreSlim _switchLock = new(1, 1);
    CancellationTokenSource _cts;

    private string _loginDisplayName;
    public string LocalVivoxPlayerId { get; private set; }
    const string PREF_BOOT_NAME = "Vivox_BootDisplayName";
    void Awake()
    {
        if (Instance != null) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _cts = new CancellationTokenSource();
        _ = BootInitAndLoginAsync();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        UnhookDeviceEvents();
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
    async Task BootInitAndLoginAsync()
    {
        var ct = _cts?.Token ?? CancellationToken.None;

        await EnsureInitializedAsync(ct);

        _loginDisplayName = GetOrCreateBootDisplayName();

        await EnsureLoggedInAsync(_loginDisplayName, ct);

        HookDeviceEventsOnce();
        RefreshDeviceCache();
        OnDeviceListsChanged?.Invoke();
    }
    string GetOrCreateBootDisplayName()
    {
        var saved = PlayerPrefs.GetString(PREF_BOOT_NAME, "");
        if (!string.IsNullOrEmpty(saved))
            return saved;

        var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
        var name = $"Player_{suffix}";
        PlayerPrefs.SetString(PREF_BOOT_NAME, name);
        PlayerPrefs.Save();
        return name;
    }
    async Task EnsureLoggedInAsync(string displayName, CancellationToken ct)
    {
        displayName = string.IsNullOrWhiteSpace(displayName) ? "Player" : displayName;

        await _initLock.WaitAsync(ct);
        try
        {
            ct.ThrowIfCancellationRequested();

            if (!IsLoggedIn)
            {
                await VivoxService.Instance.LoginAsync(new LoginOptions
                {
                    DisplayName = displayName,
                    EnableTTS = false
                });

                IsLoggedIn = true;

                //LocalVivoxPlayerId = VivoxService.Instance.PlayerId;
                Debug.Log($"[VoiceManager] Logged in. DisplayName={displayName}, PlayerId={LocalVivoxPlayerId}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[VoiceManager] EnsureLoggedInAsync failed: {e}");
        }
        finally
        {
            _initLock.Release();
        }
    }
    public static string BuildVivoxDisplayName(string nickname, uint netId)
    {
        nickname = string.IsNullOrWhiteSpace(nickname) ? "Player" : nickname.Trim();
        return $"{nickname}|{netId}";
    }

    public static bool TryParseVivoxDisplayName(string displayName, out string nickname, out uint netId)
    {
        nickname = string.Empty;
        netId = 0;

        if (string.IsNullOrWhiteSpace(displayName)) return false;

        int idx = displayName.LastIndexOf('|');
        if (idx <= 0 || idx >= displayName.Length - 1) return false;

        nickname = displayName.Substring(0, idx);
        string idPart = displayName.Substring(idx + 1);
        return uint.TryParse(idPart, out netId);
    }

    public static string GetNicknameOnly(string displayName)
    {
        if (TryParseVivoxDisplayName(displayName, out var nick, out _)) return nick;
        return string.IsNullOrWhiteSpace(displayName) ? "Player" : displayName;
    }
    public void JoinLobby(string roomCode, string displayName)
        => _ = JoinLobbyAsync(roomCode, displayName);
    public void EnterGameplay(string roomCode, string displayName, bool isAlive) 
        => _ = EnterGameplayAsync(roomCode, displayName, isAlive);
    public void SetGameplayLifeState(string roomCode, string displayName, bool isAlive) 
        => _ = EnterGameplayAsync(roomCode, displayName, isAlive);
    public void LeaveAllChannels()
    => _ = LeaveAllChannelsAsync();
    public void LeaveAndLogout()
        => _ = LeaveAndLogoutAsync();

    public async Task WarmupDevicesOnBootAsync()
    {
        var ct = _cts?.Token ?? CancellationToken.None;

        await EnsureInitializedOnlyAsync(ct);

        HookDeviceEventsOnce();
        RefreshDeviceCache();
        OnDeviceListsChanged?.Invoke();
    }
    async Task EnsureInitializedOnlyAsync(CancellationToken ct)
    {
        await _initLock.WaitAsync(ct);
        try
        {
            if (!IsReady)
            {
                await UnityServices.InitializeAsync();
                IsReady = true;
            }

            ct.ThrowIfCancellationRequested();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            ct.ThrowIfCancellationRequested();

            await VivoxService.Instance.InitializeAsync();
        }
        finally
        {
            _initLock.Release();
        }
    }
    async Task EnsureInitializedAsync(CancellationToken ct)
    {
        await _initLock.WaitAsync(ct);
        try
        {
            if (!IsReady)
            {
                await UnityServices.InitializeAsync();
                IsReady = true;
            }

            ct.ThrowIfCancellationRequested();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            ct.ThrowIfCancellationRequested();

            await VivoxService.Instance.InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"[VoiceManager] EnsureInitializedAsync failed: {e}");
        }
        finally
        {
            _initLock.Release();
        }
    }
    async Task EnsureReadyAsync(string displayName, CancellationToken ct)
    {
        displayName = string.IsNullOrWhiteSpace(displayName) ? "Player" : displayName;

        await EnsureInitializedAsync(ct);

        await _initLock.WaitAsync(ct);
        try
        {
            ct.ThrowIfCancellationRequested();

            if (!IsLoggedIn)
            {
                await VivoxService.Instance.LoginAsync(new LoginOptions
                {
                    DisplayName = displayName,
                    EnableTTS = false
                });

                IsLoggedIn = true;
                _loginDisplayName = displayName;
            }
            else
            {
                if (!string.IsNullOrEmpty(displayName) && _loginDisplayName != displayName)
                {
                    Debug.Log($"[VoiceManager] DisplayName differs (ignored while logged in). current='{_loginDisplayName}', requested='{displayName}'");
                }
            }
            if (!IsReady)
            {
                await UnityServices.InitializeAsync();
                IsReady = true;
            }

            //ct.ThrowIfCancellationRequested();

            //if (!AuthenticationService.Instance.IsSignedIn)
            //    await AuthenticationService.Instance.SignInAnonymouslyAsync();

            //ct.ThrowIfCancellationRequested();

            //await VivoxService.Instance.InitializeAsync();

            //ct.ThrowIfCancellationRequested();           

            HookDeviceEventsOnce();
            RefreshDeviceCache();
        }
        catch (Exception e)
        {
            Debug.LogError($"[VoiceManager] EnsureReadyAsync failed: {e}");
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task JoinLobbyAsync(string roomCode, string displayName)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            Debug.LogWarning("[VoiceManager] JoinLobbyAsync aborted: roomCode is empty");
            return;
        }

        var ct = _cts?.Token ?? CancellationToken.None;

        await EnsureReadyAsync(displayName, ct);
        if (!IsLoggedIn) return;

        string channelName = $"lobby_{roomCode}";

        await _switchLock.WaitAsync(ct);
        try
        {
            await LeaveAllJoinedChannelsAsync(ct);

            await JoinGroupChannelAsync(channelName, makeActive: true, ct);

            CurrentContext = VoiceContext.Lobby;
            CurrentChannel = channelName;
            ActiveTransmitChannel = channelName;

            OnVoiceChannelJoined?.Invoke();
        }
        finally
        {
            _switchLock.Release();
        }
    }
    public async Task EnterGameplayAsync(string roomCode, string displayName, bool isAlive)
    {
        if (string.IsNullOrWhiteSpace(roomCode)) return;

        var ct = _cts?.Token ?? CancellationToken.None;

        await EnsureReadyAsync(displayName, ct);
        if (!IsLoggedIn) return;

        string aliveCh = $"alive_{roomCode}";
        string deadCh = $"dead_{roomCode}";

        await _switchLock.WaitAsync(ct);
        try
        {
            await LeaveChannelIfJoinedAsync($"lobby_{roomCode}", ct);

            if (isAlive)
            {
                // Alive: alive 채널만 (송신/수신)
                await LeaveChannelIfJoinedAsync(deadCh, ct);

                await JoinGroupChannelAsync(aliveCh, makeActive: true, ct);

                CurrentContext = VoiceContext.GameplayAlive;
                CurrentChannel = aliveCh;
                ActiveTransmitChannel = aliveCh;
            }
            else
            {
                await JoinGroupChannelAsync(deadCh, makeActive: true, ct);
                await JoinGroupChannelAsync(aliveCh, makeActive: false, ct);

                CurrentContext = VoiceContext.GameplayDead;
                CurrentChannel = deadCh;
                ActiveTransmitChannel = deadCh;
            }

            OnVoiceChannelJoined?.Invoke();
        }
        finally
        {
            _switchLock.Release();
        }
    }

    //async Task SwitchToChannelAsync(string channelName, VoiceContext ctx, bool joinPositional, Channel3DProperties props = default)
    //{
    //    var ct = _cts?.Token ?? CancellationToken.None;

    //    await _switchLock.WaitAsync(ct);

    //    try
    //    {
    //        if (VivoxService.Instance != null &&
    //            VivoxService.Instance.ActiveChannels.TryGetValue(channelName, out var existing))
    //        {
    //            CurrentChannel = channelName;
    //            CurrentContext = ctx;
    //            OnVoiceChannelJoined?.Invoke();
    //            return;
    //        }

    //        string prev = CurrentChannel;
    //        if (!string.IsNullOrEmpty(prev))
    //        {
    //            CurrentChannel = null;
    //            CurrentContext = VoiceContext.None;

    //            await SafeLeaveChannelAsync(prev);

    //            await WaitChannelFullyRemovedAsync(prev, ct);
    //        }
    //        if (joinPositional)
    //        {
    //            await VivoxService.Instance.JoinPositionalChannelAsync(
    //                channelName,
    //                ChatCapability.AudioOnly,
    //                props,
    //                new ChannelOptions { MakeActiveChannelUponJoining = true }
    //            );
    //        }
    //        else
    //        {
    //            await VivoxService.Instance.JoinGroupChannelAsync(
    //                channelName,
    //                ChatCapability.AudioOnly,
    //                new ChannelOptions { MakeActiveChannelUponJoining = true }
    //            );
    //        }

    //        CurrentChannel = channelName;
    //        CurrentContext = ctx;
    //        OnVoiceChannelJoined?.Invoke();
    //    }
    //    finally
    //    {
    //        _switchLock.Release();
    //    }
    //}
    //async Task WaitChannelFullyRemovedAsync(string channelName, CancellationToken ct)
    //{
    //    const int maxMs = 1500;
    //    int waited = 0;

    //    while (waited < maxMs)
    //    {
    //        ct.ThrowIfCancellationRequested();

    //        if (VivoxService.Instance == null) return;
    //        if (!VivoxService.Instance.ActiveChannels.ContainsKey(channelName))
    //            return;

    //        await Task.Delay(50, ct);
    //        waited += 50;
    //    }

    //    Debug.LogWarning($"[VoiceManager] Channel '{channelName}' still exists after leave wait.");
    //}
    public async Task LeaveAllChannelsAsync()
    {
        try
        {
            var ct = _cts?.Token ?? CancellationToken.None;

            await _switchLock.WaitAsync(ct);
            try
            {
                await LeaveAllJoinedChannelsAsync(ct);

                CurrentContext = VoiceContext.None;
                CurrentChannel = null;
                ActiveTransmitChannel = null;

                Debug.Log("[VoiceManager] LeaveAllChannels completed");
            }
            finally
            {
                _switchLock.Release();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[VoiceManager] LeaveAllChannelsAsync failed: {e}");
        }
    }
    public async Task LeaveAndLogoutAsync()
    {
        try
        {
            var ct = _cts?.Token ?? CancellationToken.None;

            await _switchLock.WaitAsync(ct);
            try
            {
                await LeaveAllJoinedChannelsAsync(ct);

                if (IsLoggedIn)
                {
                    await VivoxService.Instance.LogoutAsync();
                    IsLoggedIn = false;
                }

                CurrentContext = VoiceContext.None;
                CurrentChannel = null;
                ActiveTransmitChannel = null;

                Debug.Log("[VoiceManager] LeaveAndLogout completed");
            }
            finally
            {
                _switchLock.Release();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[VivoxVoiceManager] LeaveAndLogoutAsync failed: {e}");
        }
    }

    //async Task SafeLeaveChannelAsync(string channelName)
    //{
    //    try
    //    {
    //        await VivoxService.Instance.LeaveChannelAsync(channelName);
    //        Debug.Log($"[VoiceManager] Left channel: {channelName}");
    //    }
    //    catch (Exception e)
    //    {
    //        Debug.LogWarning($"[VoiceManager] LeaveChannelAsync({channelName}) warning: {e.Message}");
    //    }
    //}

    private async Task JoinGroupChannelAsync(string channelName, bool makeActive, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(channelName)) return;
        if (VivoxService.Instance == null) return;

        if (VivoxService.Instance.ActiveChannels.ContainsKey(channelName))
        {
            _joinedChannels.Add(channelName);
            if (makeActive) ActiveTransmitChannel = channelName;
            return;
        }

        await VivoxService.Instance.JoinGroupChannelAsync(
            channelName,
            ChatCapability.AudioOnly,
            new ChannelOptions { MakeActiveChannelUponJoining = makeActive }
        );

        _joinedChannels.Add(channelName);
        if (makeActive) ActiveTransmitChannel = channelName;
    }
    private async Task LeaveChannelIfJoinedAsync(string channelName, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(channelName)) return;
        if (VivoxService.Instance == null) return;

        if (!_joinedChannels.Contains(channelName) && !VivoxService.Instance.ActiveChannels.ContainsKey(channelName))
            return;

        try
        {
            await VivoxService.Instance.LeaveChannelAsync(channelName);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[VoiceManager] LeaveChannelAsync({channelName}) warning: {e.Message}");
        }

        _joinedChannels.Remove(channelName);
        if (CurrentChannel == channelName) CurrentChannel = null;
    }
    private async Task LeaveAllJoinedChannelsAsync(CancellationToken ct)
    {
        if (VivoxService.Instance == null)
        {
            _joinedChannels.Clear();
            CurrentChannel = null;
            ActiveTransmitChannel = null;
            return;
        }

        var all = _joinedChannels.ToList();

        foreach (var ch in all)        
            await LeaveChannelIfJoinedAsync(ch, ct);
        

        _joinedChannels.Clear();
        CurrentChannel = null;
        ActiveTransmitChannel = null;
    }
    void HookDeviceEventsOnce()
    {
        if (_deviceEventsHooked) return;
        if (VivoxService.Instance == null) return;

        VivoxService.Instance.AvailableInputDevicesChanged += OnAvailableDevicesChanged;
        VivoxService.Instance.AvailableOutputDevicesChanged += OnAvailableDevicesChanged;
        _deviceEventsHooked = true;
    }

    void UnhookDeviceEvents()
    {
        if (!_deviceEventsHooked) return;
        if (VivoxService.Instance != null)
        {
            VivoxService.Instance.AvailableInputDevicesChanged -= OnAvailableDevicesChanged;
            VivoxService.Instance.AvailableOutputDevicesChanged -= OnAvailableDevicesChanged;
        }
        _deviceEventsHooked = false;
    }

    void OnAvailableDevicesChanged()
    {
        RefreshDeviceCache();
        OnDeviceListsChanged?.Invoke();
    }

    void RefreshDeviceCache()
    {
        if (VivoxService.Instance == null) return;

        _cachedInputs.Clear();
        _cachedOutputs.Clear();

        _cachedInputs.AddRange(VivoxService.Instance.AvailableInputDevices);
        _cachedOutputs.AddRange(VivoxService.Instance.AvailableOutputDevices);
    }

    public void RefreshDevicesForUI()
    {
        if (VivoxService.Instance == null)
        {
            Debug.LogWarning("[VoiceManager] RefreshDevicesForUI skipped: Vivox not initialized yet.");
            return;
        }

        RefreshDeviceCache();
        OnDeviceListsChanged?.Invoke();

        Debug.Log($"[VoiceManager] Devices refreshed. inputs={_cachedInputs.Count}, outputs={_cachedOutputs.Count}");
    }
}
