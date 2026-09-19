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

    public override void OnStartServer()
    {
        base.OnStartServer();
        // 새 호스팅 세션이 시작되는 시점이므로, 이전 세션 종료 때 세워둔
        // 정리 가드를 다시 풀어줘야 다음 연결 끊김/에러도 정상적으로 처리된다.
        _isCleaningUp = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        _isCleaningUp = false;
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);        

        var roomPlayer = conn.identity.GetComponent<RoomPlayer>();

        if (SpawnManager.Instance != null)
            roomPlayer.gameObject.transform.position = SpawnManager.Instance.GetAvailableSpawnPosition();
        else
            Debug.LogWarning("[RoomManager] SpawnManager.Instance가 아직 준비되지 않아 스폰 위치를 지정하지 못했습니다.");

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

            // SyncVar 훅은 오브젝트가 파괴되기 전에 동기화된다는 보장이 없어
            // RPC로 명시적으로 색상 해제를 알린다.
            if (oldIdx >= 0)
                roomPlayer.RpcColorReleased(oldIdx);

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

        // 방에 아무도 남지 않았을 때만 방을 완전히 닫는다.
        // (한 명이라도 남아있으면 roomCode는 계속 유효해야 하고,
        //  그동안은 그 코드로만 입장이 가능한 구조를 유지해야 한다)
        if (roomPlayers.Count == 0)
        {
            if (!string.IsNullOrEmpty(code))
                roomHost.DeleteRoom(code);

            RoomSessionData.Reset();
        }

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

        // NetworkServer.active/NetworkClient.isConnected는 타임아웃성 끊김 직후
        // 이미 바뀌어 있을 수 있어 조건부 분기로는 정리가 스킵될 수 있었다.
        // Stop*()는 이미 꺼져있으면 스스로 아무 동작도 하지 않으므로(Mirror 소스 확인)
        // 상태 체크 없이 항상 호출해서 확실히 정리한다.
        Debug.Log("[RoomManager] Cleanup: StopClient()/StopServer()");
        StopClient();
        StopServer();

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

            if (SpawnManager.Instance != null)
                gamePlayerObj.transform.position = SpawnManager.Instance.GetRandomPositionInZone();
            else
                Debug.LogWarning("[RoomManager] SpawnManager.Instance가 아직 준비되지 않아 게임 스폰 위치를 지정하지 못했습니다.");

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
            if (assigned.Count < pool.Count)
            {
                do
                {
                    assignedCharacter = pool[UnityEngine.Random.Range(0, pool.Count)];
                } while (assigned.Contains(assignedCharacter));
            }
            else
            {
                // 설계 범위(6~13명)를 벗어나 풀이 부족한 예외 상황 — 무한루프 대신 중복 배정으로 안전하게 대체
                Debug.LogWarning($"[RoomManager] 캐릭터 풀({pool.Count}종)보다 플레이어 수가 많아 캐릭터가 중복 배정됩니다.");
                assignedCharacter = pool[UnityEngine.Random.Range(0, pool.Count)];
            }

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

        // 동물(직업) 배정은 로비 색상/닉네임과 달리 매 게임 시작마다 새로 무작위 배정한다.
        ServerAssignCharacters();

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

    // 서버에서 씬 전환이 실제로 끝난 뒤 호출되는 Mirror 훅.
    // RoomPlayer의 OnClientEnterRoom(클라이언트 훅)은 NetworkClient.isConnected가
    // false인 타이밍에는 호출 자체가 스킵되어(로비 복귀 시 재호출되지 않는 것을 로그로 확인)
    // 표시/숨김을 여기서 서버가 직접 RPC로 지시하는 방식으로 대체했다.
    // 로비로 복귀했을 때는 위치도 게임 씬 좌표에 남아있지 않도록 재배치한다(서버 권위).
    public override void OnRoomServerSceneChanged(string sceneName)
    {
        base.OnRoomServerSceneChanged(sceneName);

        Debug.Log($"[RoomManager] OnRoomServerSceneChanged({sceneName}), roomPlayers.Count={roomPlayers.Count}, SpawnManager.Instance={(SpawnManager.Instance != null)}");

        if (sceneName == "GamePlay")
        {
            foreach (var rp in roomPlayers)
            {
                if (rp == null) continue;
                rp.RpcSetRoomVisualsActive(false);
            }
            return;
        }

        if (sceneName != "GameRoom")
            return;

        foreach (var rp in roomPlayers)
        {
            if (rp == null) continue;

            rp.RpcSetRoomVisualsActive(true);

            if (SpawnManager.Instance != null)
                rp.transform.position = SpawnManager.Instance.GetAvailableSpawnPosition();
        }

        if (SpawnManager.Instance == null)
            Debug.LogWarning("[RoomManager] OnRoomServerSceneChanged: SpawnManager.Instance가 null이라 RoomPlayer 재배치를 건너뜁니다.");
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
