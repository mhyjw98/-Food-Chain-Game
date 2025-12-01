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

    [Command]
    private void CmdSetUserId(string id)
    {
        userId = id;
        ((RoomManager)RoomManager.singleton).ReassignHostAfterDisconnect();
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

    [Command]
    void CmdSetData(string nick)
    {
        nickname = nick;
        
        var chatManager = FindObjectOfType<ChatManager>();
        chatManager.AddSystemMessage("System", $"{nickname}님이 입장하셨습니다.");
    }
    [Command]
    public void CmdSetColor(int index)
    {
        Color newColor = PlayerColorPalette.GetByIndex(index).Color;
        Debug.Log("index : " + index);
        rpColor = newColor;
    }    
}
