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
            //Debug.Log("[RoomPlayer] 로컬 플레이어 - 자동 준비 요청");
            //CmdChangeReadyState(true); // 자동으로 준비 상태로 만듦
        }
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

    public void SetCharacter(string character)
    {
        assignedCharacter = character;
    }
    private void OnCharacterAssigned(string oldValue, string newValue)
    {
        Debug.Log($"내 캐릭터가 배정됨: {newValue}");
    }
}
