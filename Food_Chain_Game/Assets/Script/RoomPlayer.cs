using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomPlayer : NetworkRoomPlayer
{
    [SyncVar] public string nickname;
    [SyncVar] public string assignedCharacter;
    [SyncVar] public bool isHost;

    public override void OnStartClient()
    {
        Debug.Log($"플레이어 {netId}가 방에 입장했습니다.");
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();       

        if (!isServer)
        {
            CmdChangeReadyState(true);
            Debug.Log("[RoomPlayer] 자동 Ready 상태 설정");
        }

        CmdSetNickname(NickNamemanager.GetNickname());

        Debug.Log("내 플레이어가 생성됨");
    }

    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();
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
    public void CmdSendChatMessage(string message)
    {
        string senderName = nickname;

        ChatManager.Instance.RpcReceiveMessage(senderName, message);
    }

    [Command]
    void CmdSetNickname(string nick)
    {
        nickname = nick;

        if(ChatManager.Instance != null)
            ChatManager.Instance.RpcReceiveMessage("System", $"{nickname}님이 입장하셨습니다.");
    }
}
