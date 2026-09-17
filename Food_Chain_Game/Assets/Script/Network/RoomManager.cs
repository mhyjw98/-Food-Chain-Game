using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;


public class RoomManager : NetworkRoomManager
{
    private bool _isCleaningUp = false;

    public RoomHost roomHost;
    public GameObject chatManagerPrefab;

    public GameObject gpPrefab; 
    private List<string> GetCharacterPool(int playerCount)
    {
        List<string> baseCharacters = new()
    {
        "Ostrich", "Zebra", "Wolf", "Badger", "Scorpion", "Crow", "Skunk", "Squirrel", "Crocodile", "Fox"
    };

        List<string> additionalCharacters = new()
    {
        "Hawk", "Hyena",  "Plover",   
    };

        if (playerCount < 10)
        {
            int clampedCount = Mathf.Clamp(playerCount, 0, baseCharacters.Count);
            return baseCharacters.GetRange(0, clampedCount);
        }
        else
        {
            int extraCount = Mathf.Clamp(playerCount - 7, 0, additionalCharacters.Count);
            baseCharacters.AddRange(additionalCharacters.GetRange(0, extraCount));
            return baseCharacters;
        }
    }
    public int maxPlayerCount = 1;
    private readonly HashSet<NetworkConnectionToClient> joinedConnections = new();
    public List<RoomPlayer> roomPlayers = new();

    private bool _userRequestedQuit = false;
    bool _joiningGameplayVoice;
    Coroutine _voiceJoinCo;
    Coroutine _voiceJoinGameplayCo;
    void OnEnable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);        

        var roomPlayer = conn.identity.GetComponent<RoomPlayer>();
        roomPlayer.gameObject.transform.position = SpawnManager.Instance.GetAvailableSpawnPosition();

        roomPlayers.Add(roomPlayer);
        Debug.Log($"[OnServerAddPlayer] roomPlayers 등록 roomPlayers count : {roomPlayers.Count}");

        string code = RoomSessionData.CurrentRoomCode;        

        uint playerId = conn.identity.netId;

        if (!joinedConnections.Contains(conn))
        {
            if (!string.IsNullOrEmpty(code))
                roomHost.ComeAndGoing(code, 1);
            joinedConnections.Add(conn);
            Debug.Log($"[Room] 입장 처리 완료: {conn.connectionId}");
        }

        if (joinedConnections.Count == maxPlayerCount)
        {
            Debug.Log("모든 인원 입장 완료! 캐릭터 배정 시작");
            ServerAssignCharacters();
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        RoomPlayer roomPlayer = null;
        string nickname = null;
        bool wasHost = false;

        if (conn != null && conn.identity != null)
            roomPlayer = conn.identity.GetComponent<RoomPlayer>();

        if (roomPlayer == null)
        {
            foreach (var go in roomPlayers.ToArray())
            {
                if (go == null) continue;
                var rp = go.GetComponent<RoomPlayer>();
                if (rp != null && rp.connectionToClient == conn)
                {
                    roomPlayer = rp;
                    break;
                }
            }
        }
        if (roomPlayer != null)
        {
            nickname = roomPlayer.nickname;
            wasHost = roomPlayer.isHost;
            int oldIdx = roomPlayer.colorIndex;
            roomPlayer.colorIndex = -1;

            if (roomPlayers.Contains(roomPlayer))
                roomPlayers.Remove(roomPlayer);

            if (joinedConnections.Contains(roomPlayer.connectionToClient))
                joinedConnections.Remove(roomPlayer.connectionToClient);
        }

        if (!string.IsNullOrEmpty(nickname))
        {
            var chatManager = FindObjectOfType<ChatManager>();
            if (chatManager != null)            
                chatManager.AddSystemMessage("System", $"{nickname}님이 퇴장하셨습니다.");                        
        }

        if (wasHost)
        {
            ReassignHostAfterDisconnect();
        }

        string code = RoomSessionData.CurrentRoomCode;

        if (!string.IsNullOrEmpty(code))
            roomHost.ComeAndGoing(code, -1);       

        RoomSessionData.Reset();

        base.OnServerDisconnect(conn);
    }

    public override void OnClientDisconnect()
    {

        base.OnClientDisconnect();

        StartCoroutine(ReturnToTitleCoroutine());
    }

    private IEnumerator ReturnToTitleCoroutine()
    {
        yield return null;

        if (_userRequestedQuit)
        {
            Debug.Log("[RoomManager] ReturnToTitleCoroutine: 사용자 요청으로 인한 종료");
            _userRequestedQuit = false;
            CleanupAndLoadTitle(showError: false, errorMessage: null);
            yield break;
        }

        CleanupAndLoadTitle(showError: true, errorMessage: "서버와의 연결이 끊어졌습니다.");
    }

    public void CleanupAndLoadTitle(bool showError, string errorMessage)
    {
        if (_isCleaningUp) return;
        _isCleaningUp = true;

        VoiceManager.Instance?.LeaveAllChannels();

        if (NetworkServer.active && NetworkClient.isConnected)
        {
            Debug.Log("[RoomManager] Cleanup: StopHost()");
            StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            Debug.Log("[RoomManager] Cleanup: StopClient()");
            StopClient();
        }
        else if (NetworkServer.active)
        {
            Debug.Log("[RoomManager] Cleanup: StopServer()");
            StopServer();
        }

        RoomSessionData.Reset();

        if (showError && NetworkErrorManager.Instance != null && !string.IsNullOrEmpty(errorMessage))
        {
            NetworkErrorManager.Instance.SetError(NetworkErrorReason.ConnectionLost, errorMessage);
        }

        if (SceneManager.GetActiveScene().name != "Title")
        {
            SceneManager.LoadScene("Title");
        }
    }
    public override GameObject OnRoomServerCreateGamePlayer(NetworkConnectionToClient conn, GameObject roomPlayerObj)
    {
        Debug.Log("[OnRoomServerCreateGamePlayer] 호출됨");
        GameObject gamePlayerObj = Instantiate(playerPrefab);

        GamePlayer gamePlayer = gamePlayerObj.GetComponent<GamePlayer>();
        RoomPlayer roomPlayer = roomPlayerObj.GetComponent<RoomPlayer>();

        if (roomPlayer != null)
        {
            gamePlayer.characterName = roomPlayer.assignedCharacter;
            gamePlayer.nickname = roomPlayer.nickname;
            gamePlayer.gpColor = roomPlayer.rpColor;

            if (CharacterZoneData.CharacterHomeZone.TryGetValue(roomPlayer.assignedCharacter, out ZoneType zone))
            {
                gamePlayer.homeZone = zone;
            }
            gamePlayerObj.transform.position = SpawnManager.Instance.GetRandomPositionInZone();
            gamePlayer.SetAnimalType(roomPlayer.assignedCharacter);
        }

        return gamePlayerObj;
    }

    public override void OnClientError(TransportError error, string reason)
    {
        base.OnClientError(error, reason);

        if (_userRequestedQuit)
        {
            Debug.Log("[RoomManager] OnClientError: 사용자 요청으로 인한 종료");
            _userRequestedQuit = false;
            CleanupAndLoadTitle(showError: false, errorMessage: null);
            return;
        }
        
        CleanupAndLoadTitle(showError: true, errorMessage: $"네트워크 오류가 발생했습니다.\n({reason})"
    );
    }

    public override void OnStopHost()
    {
        base.OnStopHost();

        if (_userRequestedQuit)
        {
            Debug.Log("[RoomManager] OnStopHost: 사용자 요청으로 호스트 종료 → 에러 팝업 생략");
            _userRequestedQuit = false;

            CleanupAndLoadTitle(showError: false, errorMessage: null);
            return;
        }
        else
            CleanupAndLoadTitle(showError: true, errorMessage: "서버와의 연결이 끊어졌습니다.");
    }
    private void ServerAssignCharacters()
    {
        Debug.Log("캐릭터 배정 로직 실행");

        var pool = GetCharacterPool(maxPlayerCount);
        HashSet<string> assigned = new();

        foreach (var playerObj in roomPlayers)
        {
            NetworkConnectionToClient conn = playerObj.GetComponent<NetworkIdentity>().connectionToClient;

            string assignedCharacter;
            do
            {
                assignedCharacter = pool[UnityEngine.Random.Range(0, pool.Count)];
            } while (assigned.Contains(assignedCharacter));

            assigned.Add(assignedCharacter);

            RoomPlayer roomPlayer = playerObj.GetComponent<RoomPlayer>();
            if (roomPlayer != null)
            {
                roomPlayer.SetCharacter(assignedCharacter);
                Debug.Log($"플레이어 {conn.connectionId} 캐릭터 배정: {assignedCharacter}");
            }
        }
    }   
    public void StartGame()
    {
        if (joinedConnections.Count < maxPlayerCount)
        {
            Debug.LogWarning($"[RoomManager] 현재 인원 {joinedConnections.Count}/{maxPlayerCount} → 인원 부족으로 게임 시작 불가");
            return;
        }
        StartCoroutine(StartGameDelayed());
    }
    private IEnumerator StartGameDelayed()
    {
        yield return null;
        yield return new WaitForSeconds(0.1f);
        ServerChangeScene("GamePlay");
    }
    public void ReassignHostAfterDisconnect()
    {
        var remainPlayers = FindObjectsOfType<RoomPlayer>()
            .Where(rp => rp != null && rp.isActiveAndEnabled)
            .ToList();

        if (remainPlayers.Count == 0)
        {
            Debug.Log("[RoomManager] 남아 있는 플레이어가 없어 호스트 재지정 생략");
            RoomSessionData.PreviousHostId = string.Empty;
            return;
        }

        RoomPlayer newHost = null;
        int minConnId = int.MaxValue;

        foreach (var rp in remainPlayers)
        {
            var c = rp.connectionToClient;
            if (c != null && c.connectionId < minConnId)
            {
                minConnId = c.connectionId;
                newHost = rp;
            }
        }

        if (newHost == null)
            newHost = remainPlayers[0];

        foreach (var rp in remainPlayers)
        {
            rp.isHost = (rp == newHost);
        }

        RoomSessionData.PreviousHostId = newHost.userId;

        Debug.Log($"[RoomManager] 호스트 재지정 완료: {newHost.userId} (connId={newHost.connectionToClient?.connectionId})");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Title")
        {
            VoiceManager.Instance?.LeaveAllChannels();
            VoiceManager.Instance?.RefreshDevicesForUI();
            return;
        }

        if (scene.name == "GameRoom")
        {
            if (_voiceJoinCo != null) StopCoroutine(_voiceJoinCo);
            _voiceJoinCo = StartCoroutine(CoJoinLobbyVoiceWhenReady());
            return;
        }
        if (scene.name == "GamePlay")
        {
            if (_voiceJoinGameplayCo != null) StopCoroutine(_voiceJoinGameplayCo);
            _voiceJoinGameplayCo = StartCoroutine(CoJoinGameplayVoiceWhenReady());
            return;
        }
    }

    IEnumerator CoJoinLobbyVoiceWhenReady()
    {
        while (!NetworkClient.isConnected) yield return null;
        while (NetworkClient.localPlayer == null) yield return null;
        while (string.IsNullOrEmpty(RoomSessionData.CurrentRoomCode)) yield return null;

        string roomCode = RoomSessionData.CurrentRoomCode;

        var rp = NetworkClient.localPlayer.GetComponent<RoomPlayer>();

        float t = 0f;
        while (rp != null && string.IsNullOrWhiteSpace(rp.nickname) && t < 3f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        string nickname = (rp != null && !string.IsNullOrWhiteSpace(rp.nickname)) ? rp.nickname : "Player";
        string displayName = VoiceManager.BuildVivoxDisplayName(nickname, rp != null ? rp.netId : 0);

        if (VoiceManager.Instance == null)
        {
            var go = new GameObject("VoiceManager");
            go.AddComponent<VoiceManager>();
        }

        //VoiceManager.Instance.JoinLobby(roomCode, displayName);
        VoiceManager.Instance.EnterGameplay(roomCode, displayName, isAlive: true);
    }

    IEnumerator CoJoinGameplayVoiceWhenReady()
    {
        if (_joiningGameplayVoice) yield break;
        _joiningGameplayVoice = true;

        try
        {
            while (!NetworkClient.isConnected) yield return null;
            while (NetworkClient.localPlayer == null) yield return null;
            while (string.IsNullOrEmpty(RoomSessionData.CurrentRoomCode)) yield return null;

            string roomCode = RoomSessionData.CurrentRoomCode;

            var gp = NetworkClient.localPlayer.GetComponent<GamePlayer>();

            float t = 0f;
            while (gp != null && string.IsNullOrWhiteSpace(gp.nickname) && t < 3f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            string nickname = (gp != null && !string.IsNullOrWhiteSpace(gp.nickname)) ? gp.nickname : "Player";
            string displayName = VoiceManager.BuildVivoxDisplayName(nickname, gp != null ? gp.netId : 0);

            if (VoiceManager.Instance == null)
            {
                Debug.LogError("[RoomManager] VoiceManager.Instance is null. Make sure VoiceManager exists in Start scene and is DontDestroyOnLoad.");
                yield break;
            }

            VoiceManager.Instance.EnterGameplay(roomCode, displayName, isAlive: true);
        }
        finally
        {
            _joiningGameplayVoice = false;
        }       
    }
}
