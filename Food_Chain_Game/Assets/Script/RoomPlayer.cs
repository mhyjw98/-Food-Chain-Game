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
        Debug.Log($"�÷��̾� {netId}�� �濡 �����߽��ϴ�.");
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();       

        if (!isServer)
        {
            CmdChangeReadyState(true);
            Debug.Log("[RoomPlayer] �ڵ� Ready ���� ����");
        }

        CmdSetNickname(NickNamemanager.GetNickname());

        Debug.Log("�� �÷��̾ ������");
    }

    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();
    }
    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
    {
        Debug.Log($"�÷��̾� {netId} �غ� ���� ����: {newReadyState}");
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
            ChatManager.Instance.RpcReceiveMessage("System", $"{nickname}���� �����ϼ̽��ϴ�.");
    }
}
