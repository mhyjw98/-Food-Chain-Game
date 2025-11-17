using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.SceneManagement;
using TMPro;
public class RoomPlayer : NetworkRoomPlayer
{
    [SyncVar] public string roomCode;
    [SyncVar(hook = nameof(OnNicknameChanged))] public string nickname;
    [SyncVar] public string userId;
    [SyncVar] public string assignedCharacter;
    [SyncVar] public bool isHost;

    public GameObject nicknameUIPrefab;
    private GameObject nicknameUIInstance;
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
        ((RoomManager)RoomManager.singleton).TryAssignHost(this);
        GameRoomUI.Instance.CheckHostStatus(this);
    }

    [Command]
    public void CmdSendChatMessage(string message)
    {
        foreach (var player in FindObjectsOfType<RoomPlayer>())
        {
            player.TargetReceiveMessage(this.nickname, message);
        }
    }
    [TargetRpc]
    public void TargetReceiveMessage(string sender, string message)
    {
        var chatManager = FindObjectOfType<ChatManager>();
        chatManager.AddSystemMessage(sender, message);
    }
    void OnNicknameChanged(string oldNick, string newNick)
    {
        if (nicknameUIInstance != null) return;

        nicknameUIInstance = Instantiate(nicknameUIPrefab, transform);
        nicknameUIInstance.transform.localPosition = new Vector3(0, 1.2f, 0);
        nicknameUIInstance.GetComponentInChildren<TextMeshProUGUI>().text = newNick;
    }
    [Command]
    void CmdSetData(string nick)
    {
        nickname = nick;
        
        var chatManager = FindObjectOfType<ChatManager>();
        chatManager.AddSystemMessage("System", $"{nickname}님이 입장하셨습니다.");
    }
   
}
