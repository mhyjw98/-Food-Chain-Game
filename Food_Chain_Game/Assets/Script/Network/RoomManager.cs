using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;


// 서버가 연결을 끊기 직전에 클라이언트에게 사유를 알리는 메시지.
// 받은 쪽은 타이틀로 돌아갔을 때 일반 문구("서버와의 연결이 끊어졌습니다") 대신 이 사유를 보여준다
public enum RoomNoticeReason : byte
{
    None,
    GameInProgress,
    RoomFull,
    HostClosedRoom
}

public struct RoomNoticeMessage : NetworkMessage
{
    public RoomNoticeReason reason;
}

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

    [Header("Game Start")]
    [Tooltip("게임 시작에 필요한 최소 인원 (임시값)")]
    [SerializeField] private int minPlayersToStart = 3;
    [Tooltip("켜면 에디터/개발 빌드에서는 최소 인원 미만(1~2명)으로도 시작할 수 있다. 정식 빌드에서는 무시된다")]
    [SerializeField] private bool allowBelowMinPlayersForTest = true;
    private bool _startingGame;

    // 로비 복귀 시 본인 클라이언트가 아직 씬 로딩 중이라 재배치 지시를 미뤄 둔 플레이어 (연결 → 목적지)
    private readonly Dictionary<NetworkConnectionToClient, Vector3> _pendingRoomTeleports = new();

    [Header("Disconnect Notice")]
    [Tooltip("입장을 거부할 때 사유 메시지가 전달될 시간을 주고 나서 연결을 끊기까지의 지연(초)")]
    [SerializeField] private float rejectDisconnectDelay = 0.5f;
    [Tooltip("방장이 방을 닫을 때 안내 메시지가 전달될 시간을 주는 지연(초)")]
    [SerializeField] private float hostCloseNoticeDelay = 0.2f;

    // 서버: 입장을 거부해 곧 끊을 연결(그사이 플레이어 생성 요청이 와도 무시)
    private readonly HashSet<NetworkConnectionToClient> _rejectedConnections = new();
    // 클라: 서버가 알려준 끊김 사유
    private RoomNoticeReason _pendingNotice = RoomNoticeReason.None;
    private bool _leaving;
    private readonly HashSet<NetworkConnectionToClient> joinedConnections = new();
    public List<RoomPlayer> roomPlayers = new();

    private bool _userRequestedQuit = false;
    Coroutine _voiceJoinCo;
    void OnEnable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Mirror의 기본 방 디버그 GUI(게임 중 "Return to Room" 버튼, 로비의 PLAYERS 박스)는 쓰지 않는다
    public override void OnGUI() { }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        // 게임 진행 중(또는 씬 전환 중)에는 새로 입장할 수 없다
        if (!Utils.IsSceneActive(RoomScene))
        {
            ServerRejectConnection(conn, RoomNoticeReason.GameInProgress);
            return;
        }

        // 정원 제한은 Node 서버의 조회 단계에만 있으므로 서버에서도 한 번 더 막는다
        if (joinedConnections.Count >= maxPlayerCount)
        {
            ServerRejectConnection(conn, RoomNoticeReason.RoomFull);
            return;
        }

        base.OnServerConnect(conn);
    }

    // 사유를 먼저 보내고, 전달될 시간을 준 뒤 연결을 끊는다(바로 끊으면 메시지가 도착하기 전에 끊긴다)
    private void ServerRejectConnection(NetworkConnectionToClient conn, RoomNoticeReason reason)
    {
        Debug.Log($"[RoomManager] 연결 거부({reason}): {conn}");

        _rejectedConnections.Add(conn);
        conn.Send(new RoomNoticeMessage { reason = reason });
        StartCoroutine(CoDisconnectAfterDelay(conn));
    }

    private IEnumerator CoDisconnectAfterDelay(NetworkConnectionToClient conn)
    {
        yield return new WaitForSecondsRealtime(rejectDisconnectDelay);

        if (conn != null && NetworkServer.connections.ContainsKey(conn.connectionId))
            conn.Disconnect();
    }

    private void OnRoomNoticeReceived(RoomNoticeMessage message)
    {
        _pendingNotice = message.reason;
    }

    // 끊김 사유를 한 번 꺼내 쓰고 비운다. 서버가 알려준 사유가 없으면 fallback을 그대로 쓴다
    private string ConsumeDisconnectMessage(string fallback)
    {
        RoomNoticeReason reason = _pendingNotice;
        _pendingNotice = RoomNoticeReason.None;

        return reason switch
        {
            RoomNoticeReason.GameInProgress => "게임이 진행 중인 방에는 입장할 수 없습니다.",
            RoomNoticeReason.RoomFull => "방 인원이 가득 찼습니다.",
            RoomNoticeReason.HostClosedRoom => "방장이 방을 닫았습니다.",
            _ => fallback
        };
    }

    // 플레이어가 직접 방(게임)에서 나갈 때 호출. 방장이면 남은 사람들에게 방이 닫혔음을 먼저 알린다
    public void LeaveRoom()
    {
        if (_leaving) return;
        StartCoroutine(CoLeaveRoom());
    }

    private IEnumerator CoLeaveRoom()
    {
        _leaving = true;
        _userRequestedQuit = true;

        if (NetworkServer.active)
        {
            NetworkServer.SendToAll(new RoomNoticeMessage { reason = RoomNoticeReason.HostClosedRoom });
            yield return new WaitForSecondsRealtime(hostCloseNoticeDelay);
        }

        _leaving = false;
        CleanupAndLoadTitle(showError: false, errorMessage: null);
        _userRequestedQuit = false;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _rejectedConnections.Clear();
        // 새 호스팅 세션이 시작되는 시점이므로, 이전 세션 종료 때 세워둔
        // 정리 가드를 다시 풀어줘야 다음 연결 끊김/에러도 정상적으로 처리된다.
        _isCleaningUp = false;

        // 방이 살아 있음을 Node 서버에 주기적으로 알린다(끊기면 서버가 죽은 방으로 보고 지운다)
        if (roomHost != null)
            roomHost.StartHeartbeat();
    }

    public override void OnStopServer()
    {
        // 서버가 내려가면 방 코드는 더 이상 쓸 수 없다. 접속 종료 처리(마지막 퇴장 보고)가
        // 전부 돌지 않고 서버가 꺼지는 경우에도 Node 서버에 방이 남지 않도록 여기서 직접 지운다
        if (roomHost != null)
        {
            roomHost.StopHeartbeat();

            string code = RoomSessionData.CurrentRoomCode;
            if (!string.IsNullOrEmpty(code))
                roomHost.DeleteRoom(code);
        }

        base.OnStopServer();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        _isCleaningUp = false;

        _pendingNotice = RoomNoticeReason.None;
        NetworkClient.RegisterHandler<RoomNoticeMessage>(OnRoomNoticeReceived, false);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // 입장을 거부해 곧 끊을 연결의 플레이어 생성 요청은 무시
        if (_rejectedConnections.Contains(conn)) return;

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
        // 아래 정리 과정에서 예외가 나더라도 base 호출(해당 연결의 플레이어 오브젝트 파괴)은 반드시 실행한다.
        // 건너뛰면 나간 플레이어의 캐릭터가 월드에 그대로 남는다
        try
        {
            ServerCleanupDisconnectedPlayer(conn);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            base.OnServerDisconnect(conn);
        }
    }

    private void ServerCleanupDisconnectedPlayer(NetworkConnectionToClient conn)
    {
        // 게임 진행 중이면 게임 쪽에 먼저 "나감"을 알린다.
        // (이 메서드 끝의 base 호출에서 해당 연결의 플레이어 오브젝트가 파괴된다)
        GamePlayer leavingGamePlayer = null;
        if (conn != null && conn.identity != null)
            leavingGamePlayer = conn.identity.GetComponent<GamePlayer>();

        if (leavingGamePlayer != null && GameMamager.Instance != null)
            GameMamager.Instance.ServerHandlePlayerLeft(leavingGamePlayer);

        // 로비→게임 전환 대기 중에 끊긴 연결이 남아 있으면, 서버 로딩 완료 시 파괴된 RoomPlayer로
        // 게임 캐릭터를 만들려다 예외가 나서 다른 대기 플레이어까지 생성이 막힌다
        pendingPlayers.RemoveWhere(p => p.conn == conn);
        _rejectedConnections.Remove(conn);
        if (conn != null) _pendingRoomTeleports.Remove(conn);

        // 실제로 입장 처리(+1)된 연결만 퇴장(-1)으로 집계한다.
        // 게임 중 입장 시도처럼 바로 끊긴 연결까지 차감하면 Node 서버의 인원수가 실제보다 작아진다
        bool wasJoined = conn != null && joinedConnections.Remove(conn);

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

        }

        if (!string.IsNullOrEmpty(nickname) && leavingGamePlayer == null)
        {
            // 남아 있는 모든 플레이어에게 알린다(나가는 본인은 이미 연결이 끊겼으므로 제외)
            RoomPlayer.ServerBroadcastSystemMessage($"{nickname}님이 퇴장하셨습니다.", roomPlayer);                        
        }

        if (wasHost)
        {
            ReassignHostAfterDisconnect();
        }

        string code = RoomSessionData.CurrentRoomCode;

        if (wasJoined && !string.IsNullOrEmpty(code))
            roomHost.ComeAndGoing(code, -1);

        // 방에 아무도 남지 않았을 때만 방을 완전히 닫는다.
        // (한 명이라도 남아있으면 roomCode는 계속 유효해야 하고,
        //  그동안은 그 코드로만 입장이 가능한 구조를 유지해야 한다)
        if (wasJoined && roomPlayers.Count == 0)
        {
            if (!string.IsNullOrEmpty(code))
                roomHost.DeleteRoom(code);

            RoomSessionData.Reset();
        }
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

        CleanupAndLoadTitle(showError: true, errorMessage: ConsumeDisconnectMessage("서버와의 연결이 끊어졌습니다."));
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
        RoomPlayer roomPlayer = roomPlayerObj != null ? roomPlayerObj.GetComponent<RoomPlayer>() : null;

        if (roomPlayer != null)
        {
            gamePlayer.characterName = roomPlayer.assignedCharacter;
            gamePlayer.nickname = roomPlayer.nickname;
            gamePlayer.gpColor = roomPlayer.rpColor;
            gamePlayer.voiceId = roomPlayer.voiceId;

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

        var pool = GetCharacterPool(roomPlayers.Count);
        HashSet<string> assigned = new();

        foreach (var playerObj in roomPlayers)
        {

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
                Debug.Log($"플레이어 {roomPlayer.nickname} 캐릭터 배정: {assignedCharacter}");
            }
        }
    }   
    public void StartGame()
    {
        TryStartGame(out _);
    }

    // 시작하지 못하면 false. 플레이어에게 보여줄 사유가 있으면 failReason에 담는다
    public bool TryStartGame(out string failReason)
    {
        failReason = null;

        if (!NetworkServer.active)
        {
            failReason = "방장만 게임을 시작할 수 있습니다.";
            return false;
        }

        if (_startingGame || !Utils.IsSceneActive(RoomScene))
            return false;

        roomPlayers.RemoveAll(rp => rp == null);
        int playerCount = roomPlayers.Count;

        // 테스트 편의: 에디터/개발 빌드에서는 최소 인원 미만으로도 시작 허용(정식 빌드에서는 무시)
        bool testBypass = allowBelowMinPlayersForTest && (Application.isEditor || Debug.isDebugBuild);
        int requiredCount = testBypass ? 1 : minPlayersToStart;

        if (playerCount < requiredCount)
        {
            failReason = $"게임을 시작하려면 최소 {minPlayersToStart}명이 필요합니다. (현재 {playerCount}명)";
            Debug.LogWarning($"[RoomManager] {failReason}");
            return false;
        }

        _startingGame = true;
        StartCoroutine(StartGameDelayed());
        return true;
    }
    private IEnumerator StartGameDelayed()
    {
        yield return null;
        yield return new WaitForSeconds(0.1f);

        // 동물(직업) 배정은 로비 색상/닉네임과 달리 매 게임 시작마다 새로 무작위 배정한다.
        roomPlayers.RemoveAll(rp => rp == null);
        ServerAssignCharacters();

        ServerChangeScene(GameplayScene);
    }
    public void ReassignHostAfterDisconnect()
    {
        var remainPlayers = FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None)
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

        Debug.Log($"[RoomManager] 호스트 재지정 완료: {newHost.nickname} (connId={newHost.connectionToClient?.connectionId})");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Title")
        {
            if (_voiceJoinCo != null) StopCoroutine(_voiceJoinCo);
            _voiceJoinCo = null;
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
            // 로비 채널 입장을 기다리던 중이면 취소한다(게임 채널 입장과 엇갈리지 않게)
            if (_voiceJoinCo != null) StopCoroutine(_voiceJoinCo);
            _voiceJoinCo = null;
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

        _startingGame = false;

        Debug.Log($"[RoomManager] OnRoomServerSceneChanged({sceneName}), roomPlayers.Count={roomPlayers.Count}, SpawnManager.Instance={(SpawnManager.Instance != null)}");

        _pendingRoomTeleports.Clear();

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
                ServerRelocateRoomPlayer(rp, SpawnManager.Instance.GetAvailableSpawnPosition());
        }

        if (SpawnManager.Instance == null)
            Debug.LogWarning("[RoomManager] OnRoomServerSceneChanged: SpawnManager.Instance가 null이라 RoomPlayer 재배치를 건너뜁니다.");
    }

    // RoomPlayer의 위치는 본인 클라이언트가 정한다(NetworkTransform: Client→Server).
    // 서버가 transform만 바꾸면 방장 외에는 반영되지 않고 클라이언트가 보내는 기존 위치로 덮이므로 Teleport로 지시한다.
    // ClientRpc는 준비(ready)된 클라이언트에게만 전달되므로, 본인이 아직 씬 로딩 중이면 준비될 때까지 미룬다(OnServerReady)
    private void ServerRelocateRoomPlayer(RoomPlayer rp, Vector3 destination)
    {
        if (rp == null) return;

        rp.transform.position = destination;

        var conn = rp.connectionToClient;
        if (conn != null && !conn.isReady)
        {
            _pendingRoomTeleports[conn] = destination;
            return;
        }

        ServerSendRoomTeleport(rp, destination);
    }

    private void ServerSendRoomTeleport(RoomPlayer rp, Vector3 destination)
    {
        var networkTransform = rp.GetComponent<NetworkTransformBase>();
        if (networkTransform == null)
        {
            Debug.LogWarning("[RoomManager] RoomPlayer에 NetworkTransform이 없어 재배치를 전달하지 못했습니다.");
            return;
        }

        networkTransform.RpcTeleport(destination);
    }

    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);

        if (conn == null || !_pendingRoomTeleports.TryGetValue(conn, out Vector3 destination))
            return;

        _pendingRoomTeleports.Remove(conn);

        if (!Utils.IsSceneActive(RoomScene) || conn.identity == null)
            return;

        var rp = conn.identity.GetComponent<RoomPlayer>();
        if (rp == null) return;

        rp.transform.position = destination;
        ServerSendRoomTeleport(rp, destination);
    }

    // 로비 음성 채널 입장: 방 코드와 연결이 준비된 뒤에 들어간다.
    // 게임 채널로의 전환은 GamePlayer가 맡는다(OnStartLocalPlayer / 사망 시)
    IEnumerator CoJoinLobbyVoiceWhenReady()
    {
        while (!NetworkClient.isConnected) yield return null;
        while (NetworkClient.localPlayer == null) yield return null;
        while (string.IsNullOrEmpty(RoomSessionData.CurrentRoomCode)) yield return null;

        if (VoiceManager.Instance == null)
        {
            var go = new GameObject("VoiceManager");
            go.AddComponent<VoiceManager>();
        }

        VoiceManager.Instance.JoinLobby(RoomSessionData.CurrentRoomCode);
        _voiceJoinCo = null;
    }
}
