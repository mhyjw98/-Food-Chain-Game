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

    [Command] // ���������� ����ǵ��� ����
    public void CmdSetCharacter(string character)
    {
        assignedCharacter = character;
    }

    private void OnCharacterAssigned(string oldCharacter, string newCharacter)
    {
        Debug.Log($"�� ĳ���Ͱ� ������: {newCharacter}");
        LobbyManager.Instance.UpdateCharacterDisplay(newCharacter); // UI ������Ʈ
    }

}
