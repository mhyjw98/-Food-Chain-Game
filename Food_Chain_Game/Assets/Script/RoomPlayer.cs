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

    [Command] // 서버에서만 실행되도록 설정
    public void CmdSetCharacter(string character)
    {
        assignedCharacter = character;
    }

    private void OnCharacterAssigned(string oldCharacter, string newCharacter)
    {
        Debug.Log($"내 캐릭터가 배정됨: {newCharacter}");
        LobbyManager.Instance.UpdateCharacterDisplay(newCharacter); // UI 업데이트
    }

}
