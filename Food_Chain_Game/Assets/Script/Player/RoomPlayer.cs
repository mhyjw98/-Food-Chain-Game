using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.SceneManagement;
using TMPro;
public class RoomPlayer : NetworkRoomPlayer
{
    [SyncVar(hook = nameof(OnNicknameChanged))] public string nickname;
    [SyncVar(hook = nameof(OnColorIndexChanged))] public int colorIndex = -1;
    [SyncVar(hook = nameof(OnColorChanged))] public Color rpColor = Color.white;
    // 고른 의상(CostumeCatalog 번호). 겉모습이라 모두에게 보인다. 다른 플레이어와 겹쳐도 된다
    [SyncVar(hook = nameof(OnCostumeChanged))] public int costumeIndex;
    // 음성 채팅 참가자 ID: 음성 참가자 목록에서 닉네임을 찾는 데 쓰고, 게임 시작 시 GamePlayer로 넘긴다
    [SyncVar] public string voiceId;
    // 배정된 캐릭터는 서버만 가진다(SyncVar 아님). 클라이언트에 보내면 게임 시작 전에 전원의 역할이 노출된다
    [System.NonSerialized] public string assignedCharacter;
    // 테스트용 직업 선택: 로비에서 고른 캐릭터(서버만 가짐, 비어 있으면 무작위). 같은 이유로 SyncVar가 아니다
    [System.NonSerialized] public string requestedCharacter;
    [SyncVar] public bool isHost;
    // 서버: 입장 안내를 이미 보냈는지. 방장의 RoomPlayer는 게임 후 로비로 돌아올 때 OnStartLocalPlayer가 다시 불려
    // CmdSetData가 한 번 더 오므로, 이 플래그가 없으면 "입장하셨습니다"가 판마다 다시 표시된다
    [System.NonSerialized] private bool _joinAnnounced;

    // 본인 클라이언트에서 로비 준비가 끝났는지(서버가 닉네임과 색을 정해 돌려줌). "연결 중" 화면을 걷는 기준
    public bool IsLocalSetupComplete => isLocalPlayer && !string.IsNullOrEmpty(nickname) && colorIndex >= 0;

    public TextMeshProUGUI nicknameText;

    [Header("Input Limits")]
    [SerializeField] private int maxNicknameLength = 8;
    [SerializeField] private int maxChatLength = 200;
    // 본인: 이 RoomPlayer로 닉네임·색·의상을 이미 서버에 보냈는지.
    // 게임 후 로비로 돌아오면 같은 RoomPlayer에 OnStartLocalPlayer가 다시 불리는데, 그 순간에는 연결이 아직 준비 전이라
    // 다시 보낸 Command가 "when client not ready" 경고와 함께 버려진다(값은 이미 서버에 있으므로 다시 보낼 필요도 없다)
    private bool _localDataSent;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        int index = RoomSessionData.ColorIndex;
        if (!_localDataSent)
        {
            _localDataSent = true;

            if (!isServer)
            {
                CmdChangeReadyState(true);
                Debug.Log("[RoomPlayer] 자동 Ready 상태 설정");
            }

            CmdSetData(NickNamemanager.GetNickname());
            CmdSetColor(index);
            CmdSetCostume(RoomSessionData.CostumeIndex);

            // 다시 접속해 새 RoomPlayer가 생겨도 이전에 고른 직업이 서버에 남도록 다시 보낸다.
            // 게임 후 로비 복귀(같은 RoomPlayer)에는 값이 서버에 그대로 있고, 연결이 준비 전이라 보내도 버려진다
            if (!string.IsNullOrEmpty(RoomSessionData.RequestedCharacter))
                CmdSetRequestedCharacter(RoomSessionData.RequestedCharacter);
        }
        PlayerColorPalette.InitColorIndex(index);

        // 로비 UI는 씬을 불러올 때마다 새로 생기므로 매번 다시 연결한다
        var setting = FindFirstObjectByType<CharacterSetting>();
        if (setting != null)
            setting.SetLocalPlayer(this, index);

        StartCoroutine(CoReportVoiceId());

        Debug.Log("내 플레이어가 생성됨");
    }

    // 음성 로그인이 끝나면 내 음성 ID를 서버에 알린다
    private IEnumerator CoReportVoiceId()
    {
        while (VoiceManager.Instance == null || string.IsNullOrEmpty(VoiceManager.Instance.LocalVoiceId))
            yield return null;

        string localVoiceId = VoiceManager.Instance.LocalVoiceId;
        if (voiceId != localVoiceId)
            CmdSetVoiceId(localVoiceId);
    }

    [Command]
    private void CmdSetVoiceId(string id)
    {
        if (!VoiceManager.IsValidVoiceId(id)) return;

        // 다른 플레이어의 ID를 사칭하지 못하게 한다
        foreach (var other in FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None))
        {
            if (other != this && other.voiceId == id) return;
        }

        voiceId = id;
    }
    // Mirror 기본 방 디버그 GUI(준비/제거 버튼)는 쓰지 않는다
    public override void OnGUI() { }

    // 표시/숨김을 클라이언트가 현재 씬을 보고 스스로 결정한다.
    // 서버 RPC(RpcSetRoomVisualsActive)는 그 시점에 준비(ready)된 클라이언트에게만 전달되므로,
    // 서버보다 씬 로딩이 늦은 클라이언트는 놓칠 수 있다
    public override void OnStartClient()
    {
        base.OnStartClient();

        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        RefreshRoomVisualsForCurrentScene();
        ApplyCostume();
    }

    public override void OnStopClient()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        base.OnStopClient();
    }

    private void OnActiveSceneChanged(Scene previous, Scene current)
    {
        RefreshRoomVisualsForCurrentScene();
    }

    private void RefreshRoomVisualsForCurrentScene()
    {
        var room = NetworkManager.singleton as NetworkRoomManager;
        if (room == null) return;

        SetRoomVisualsActive(Utils.IsSceneActive(room.RoomScene));
    }

    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
    {
        Debug.Log($"플레이어 {netId} 준비 상태 변경: {newReadyState}");
    }

    public void SetCharacter(string character)
    {
        assignedCharacter = character;
    }

    // 테스트용 직업 선택. 빈 문자열이면 무작위로 되돌린다. 기능이 꺼져 있거나 없는 캐릭터면 무시한다
    [Command]
    public void CmdSetRequestedCharacter(string character)
    {
        var roomManager = RoomManager.singleton as RoomManager;
        if (roomManager == null || !roomManager.CharacterSelectEnabled) return;

        if (string.IsNullOrEmpty(character))
        {
            requestedCharacter = null;
            return;
        }
        if (!CharacterZoneData.CharacterHomeZone.ContainsKey(character)) return;

        requestedCharacter = character;
    }

    // 게임 플레이 중에도 RoomPlayer 오브젝트 자체는 파괴하지 않고 유지해야
    // 로비로 돌아올 때 Mirror가 재사용할 수 있다(로비 복귀 로직 참고).
    // 대신 화면에 겹쳐 보이지 않도록 자식 시각 요소만 껐다 켠다.
    // nicknameText/SpeechBubble은 전부 자식 오브젝트라, 여기서 끄고 켜도
    // NetworkIdentity가 붙은 루트 오브젝트의 OnDisable(roomSlots 이탈)은 발생하지 않는다.
    public void SetRoomVisualsActive(bool active)
    {
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.enabled = active;

        var collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = active;

        if (nicknameText != null)
            nicknameText.gameObject.SetActive(active);

        var bubble = GetComponentInChildren<SpeechBubble>(true);
        if (bubble != null)
            bubble.gameObject.SetActive(active);
    }

    // 서버가 확실히 감지하는 시점(RoomManager.OnRoomServerSceneChanged)에서
    // 직접 지시하는 RPC. NetworkRoomPlayer의 OnClientEnterRoom은
    // NetworkClient.isConnected가 false인 타이밍에는 호출 자체가 스킵될 수 있어
    // (실제 로그로 로비 복귀 시 호출되지 않는 것을 확인함) 신뢰할 수 없었다.
    [ClientRpc]
    public void RpcSetRoomVisualsActive(bool active)
    {
        Debug.Log($"[RoomPlayer] RpcSetRoomVisualsActive({active}) netId={netId} nickname={nickname}");
        SetRoomVisualsActive(active);
    }

    // 서버가 정한 자리로 본인 캐릭터를 옮긴다(로비 복귀 재배치). 위치는 본인이 동기화하므로(Client→Server) 본인이 직접 옮긴다
    [TargetRpc]
    public void TargetMoveTo(Vector3 destination)
    {
        transform.position = destination;

        var rigid = GetComponent<Rigidbody2D>();
        if (rigid != null)
        {
            rigid.position = destination;
            rigid.linearVelocity = Vector2.zero;
        }
    }

    // NetworkRoomPlayer가 제공하는 훅. 참고용 로그만 남기고 실제 표시/숨김은
    // 서버발 RpcSetRoomVisualsActive가 담당한다(위 주석 참고).
    public override void OnClientEnterRoom()
    {
        base.OnClientEnterRoom();
        Debug.Log($"[RoomPlayer] OnClientEnterRoom netId={netId} nickname={nickname}");
    }

    public override void OnClientExitRoom()
    {
        base.OnClientExitRoom();
        Debug.Log($"[RoomPlayer] OnClientExitRoom netId={netId} nickname={nickname}");

        // 이 훅은 "내가 방을 나갈 때"뿐 아니라 다른 RoomPlayer 하나가 파괴될 때마다(누군가 로비에서 퇴장)
        // 남아 있는 모든 RoomPlayer에 대해 불린다(NetworkRoomPlayer.OnDisable → CallOnClientExitRoom).
        // 무조건 숨기면 로비에 남은 플레이어 전원이 안 보이게 되므로, 현재 씬 기준으로만 결정한다
        RefreshRoomVisualsForCurrentScene();
    }

    [Command]
    public void CmdSendChatMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        if (message.Length > maxChatLength) message = message.Substring(0, maxChatLength);
        message = TextSanitizer.BlockRichText(message);

        foreach (var player in FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None))
        {
            player.TargetReceiveMessage(netId, this.nickname, message);
        }

        var bubble = GetComponentInChildren<SpeechBubble>();
        if (bubble != null)
            bubble.Show(message);
    }
    [TargetRpc]
    public void TargetReceiveMessage(uint senderNetId, string sender, string message)
    {
        var chatManager = FindFirstObjectByType<ChatManager>();
        if (chatManager != null)
            chatManager.AddSystemMessage(sender, message);

        if (sender == "System")
            return;

        if (NetworkClient.spawned.TryGetValue(senderNetId, out var identity))
        {
            var senderPlayer = identity.GetComponent<RoomPlayer>();
            if (senderPlayer != null)
            {
                var bubble = senderPlayer.GetComponentInChildren<SpeechBubble>();
                if (bubble != null)
                    bubble.Show(message);
            }
        }
    }
    void OnNicknameChanged(string oldNick, string newNick)
    {
        nicknameText.text = newNick;
    }
    void OnColorChanged(Color oldColor, Color newColor)
    {
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.color = newColor;
    }

    void OnCostumeChanged(int oldIndex, int newIndex)
    {
        ApplyCostume();
    }

    private void ApplyCostume()
    {
        var view = GetComponent<PlayerCostumeView>();
        if (view != null) view.SetCostume(costumeIndex);
    }

    [Command]
    public void CmdSetCostume(int index)
    {
        if (!CostumeCatalog.IsValidIndex(index)) return;
        costumeIndex = index;
    }

    void OnColorIndexChanged(int oldIndex, int newIndex)
    {
        PlayerColorPalette.RefreshButtons(oldIndex, newIndex);

        // 요청한 색이 이미 사용 중이라 서버가 다른 색을 배정한 경우, 내 미리보기와 저장값을 실제 색에 맞춘다
        if (isLocalPlayer && newIndex >= 0 && newIndex != RoomSessionData.ColorIndex)
        {
            RoomSessionData.ColorIndex = newIndex;

            var setting = FindFirstObjectByType<CharacterSetting>();
            if (setting != null)
                setting.SetLocalPlayer(this, newIndex);
        }
    }

    [ClientRpc]
    public void RpcColorReleased(int index)
    {
        PlayerColorPalette.OnUnSelectColor(index);
    }

    [Command]
    void CmdSetData(string nick)
    {
        nick = string.IsNullOrWhiteSpace(nick) ? "Player" : nick.Trim();
        if (nick.Length > maxNicknameLength) nick = nick.Substring(0, maxNicknameLength);

        nickname = TextSanitizer.BlockRichText(nick);

        if (!_joinAnnounced)
        {
            _joinAnnounced = true;
            ServerBroadcastSystemMessage($"{nickname}님이 입장하셨습니다.");
        }

        // 새 플레이어가 들어올 때마다 방장을 다시 정한다(첫 입장자가 방장이 되는 것도 여기서 결정)
        var roomManager = RoomManager.singleton as RoomManager;
        if (roomManager != null)
            roomManager.ReassignHostAfterDisconnect();

        if (GameRoomUI.Instance != null)
            GameRoomUI.Instance.CheckHostStatus(this);
    }

    // 방에 있는 모든 플레이어의 채팅창에 안내 문구를 보낸다.
    // (서버에서 ChatManager에 직접 적으면 방장 화면에만 보인다)
    [Server]
    public static void ServerBroadcastSystemMessage(string message, RoomPlayer except = null)
    {
        if (string.IsNullOrEmpty(message)) return;

        foreach (var player in FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None))
        {
            if (player == null || player == except) continue;
            if (player.connectionToClient == null) continue;

            player.TargetReceiveMessage(0, "System", message);
        }
    }
    [Command]
    public void CmdSetColor(int index)
    {
        if (index < 0 || index >= PlayerColorPalette.Count)
        {
            Debug.LogWarning($"[RoomPlayer] CmdSetColor: 잘못된 색상 index({index})가 전달되어 무시합니다.");
            return;
        }

        // 다른 플레이어가 쓰는 색이면: 아직 색이 없는 최초 입장은 빈 색으로 대신 배정하고,
        // 이미 색이 있는 변경 요청은 거부한다(입장 전에는 방의 색 사용 현황을 알 수 없어 겹칠 수 있다)
        if (ServerIsColorTaken(index))
        {
            if (colorIndex >= 0) return;

            index = ServerFindFreeColorIndex();
            if (index < 0) return;
        }

        Color newColor = PlayerColorPalette.GetByIndex(index).Color;

        colorIndex = index;
        rpColor = newColor;
    }
    [Command]
    public void CmdSetIndex(int index)
    {
        if (index < 0 || index >= PlayerColorPalette.Count) return;
        if (ServerIsColorTaken(index)) return;

        colorIndex = index;
    }

    [Server]
    private bool ServerIsColorTaken(int index)
    {
        foreach (var other in FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None))
        {
            if (other != this && other.colorIndex == index)
                return true;
        }
        return false;
    }

    [Server]
    private int ServerFindFreeColorIndex()
    {
        for (int i = 0; i < PlayerColorPalette.Count; i++)
        {
            if (!ServerIsColorTaken(i))
                return i;
        }
        return -1;
    }
}
