using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomPlayer : NetworkRoomPlayer
{
    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"플레이어 {netId}가 방에 입장했습니다.");
    }

    public override void OnStartLocalPlayer()
    {
        Debug.Log("내 플레이어가 생성됨");
    }

    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
    {
        Debug.Log($"플레이어 {netId} 준비 상태 변경: {newReadyState}");
    }
}
