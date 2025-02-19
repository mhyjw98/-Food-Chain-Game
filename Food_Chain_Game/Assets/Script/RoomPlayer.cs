using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomPlayer : NetworkRoomPlayer
{
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
}
