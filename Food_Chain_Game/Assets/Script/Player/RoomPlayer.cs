using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.VisualScripting;
using static UnityEngine.EventSystems.EventTrigger;
public class RoomPlayer : NetworkRoomPlayer
{
    [SyncVar(hook = nameof(OnNicknameChanged))] public string nickname;
    [SyncVar(hook = nameof(OnColorIndexChanged))] public int colorIndex = -1;
    [SyncVar(hook = nameof(OnColorChanged))] public Color rpColor = Color.white;
    [SyncVar] public string userId;
    [SyncVar] public string assignedCharacter;
    [SyncVar] public bool isHost;

    public TextMeshProUGUI nicknameText;
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (!isServer)
        {
            CmdChangeReadyState(true);
            Debug.Log("[RoomPlayer] 자동 Ready 상태 설정");
        }

        CmdSetData(NickNamemanager.GetNickname());

        string generatedId = UserIdManager.GenerateUserId();
        Debug.Log($"[RoomPlayer] 로컬에서 생성된 UserId: {generatedId}");
        CmdSetUserId(generatedId);

        int index = RoomSessionData.ColorIndex;
        CmdSetColor(index);
        PlayerColorPalette.InitColorIndex(index);

        var setting = FindObjectOfType<CharacterSetting>();
        if (setting != null)
            setting.SetLocalPlayer(this, index);

        Debug.Log("내 플레이어가 생성됨");
    }
    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
    {
        Debug.Log($"플레이어 {netId} 준비 상태 변경: {newReadyState}");
    }

    public void SetCharacter(string character)
    {
        assignedCharacter = character;
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
        SetRoomVisualsActive(false);
    }

    [Command]
    private void CmdSetUserId(string id)
    {
        userId = id;
        ((RoomManager)RoomManager.singleton).ReassignHostAfterDisconnect();

        if (GameRoomUI.Instance != null)
            GameRoomUI.Instance.CheckHostStatus(this);
    }

    [Command]
    public void CmdSendChatMessage(string message)
    {
        foreach (var player in FindObjectsOfType<RoomPlayer>())
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
        var chatManager = FindObjectOfType<ChatManager>();
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

    void OnColorIndexChanged(int oldIndex, int newIndex)
    {
        PlayerColorPalette.RefreshButtons(oldIndex, newIndex);
    }

    [ClientRpc]
    public void RpcColorReleased(int index)
    {
        PlayerColorPalette.OnUnSelectColor(index);
    }

    [Command]
    void CmdSetData(string nick)
    {
        nickname = nick;

        var chatManager = FindObjectOfType<ChatManager>();
        if (chatManager != null)
            chatManager.AddSystemMessage("System", $"{nickname}님이 입장하셨습니다.");
    }
    [Command]
    public void CmdSetColor(int index)
    {
        if (index < 0 || index >= PlayerColorPalette.Count)
        {
            Debug.LogWarning($"[RoomPlayer] CmdSetColor: 잘못된 색상 index({index})가 전달되어 무시합니다.");
            return;
        }

        Color newColor = PlayerColorPalette.GetByIndex(index).Color;

        colorIndex = index;
        rpColor = newColor;
    }
    [Command]
    public void CmdSetIndex(int index)
    {
        colorIndex = index;
    }
}
