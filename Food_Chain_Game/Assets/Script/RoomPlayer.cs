using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomPlayer : NetworkRoomPlayer
{
    [SyncVar(hook = nameof(OnCharacterAssigned))]
    public string assignedCharacter;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isLocalPlayer)
        {
            //Debug.Log("[RoomPlayer] ���� �÷��̾� - �ڵ� �غ� ��û");
            //CmdChangeReadyState(true); // �ڵ����� �غ� ���·� ����
        }
        Debug.Log($"�÷��̾� {netId}�� �濡 �����߽��ϴ�.");
    }

    public override void OnStartLocalPlayer()
    {
        Debug.Log("�� �÷��̾ ������");
    }
    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
    {
        Debug.Log($"�÷��̾� {netId} �غ� ���� ����: {newReadyState}");
    }

    public void SetCharacter(string character)
    {
        assignedCharacter = character;
    }
    private void OnCharacterAssigned(string oldValue, string newValue)
    {
        Debug.Log($"�� ĳ���Ͱ� ������: {newValue}");
    }
}
