using Mirror;
using Mirror.Examples.Chat;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Mirror.BouncyCastle.Math.EC.ECCurve;

public class RoomManager : NetworkRoomManager
{

    public static RoomManager Instance { get; private set; }

    public GameObject chatManagerPrefab;

    private List<GameObject> players = new List<GameObject>();

    private List<string> characterPool = new List<string>()
    {
        "사자", "악어", "매", "하이애나", "뱀", "카멜레온",
        "사슴", "수달", "토끼", "까마귀", "청둥오리", "쥐", "악어새"
    };

    public override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        //GameObject chatMgr = Instantiate(chatManagerPrefab);
        //NetworkServer.Spawn(chatMgr);      
    }
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        GameObject playerObj = conn.identity.gameObject;
        players.Add(playerObj);

        RoomPlayer roomPlayer = conn.identity.GetComponent<RoomPlayer>();

        if (players.Count == 1)
        {
            roomPlayer.isHost = true;
            Debug.Log($"[RoomManager] conn {conn.connectionId} 은 호스트로 지정");
        }
        else
        {
            roomPlayer.isHost = false;
        }

        string character = AssignRandomCharacter();

        roomPlayer.SetCharacter(character);
        Debug.Log($"[RoomPlayer] {conn.connectionId} 캐릭터 배정: {character}");       
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (conn.identity != null && conn.identity.gameObject != null)
        {
            players.Remove(conn.identity.gameObject);

            var roomPlayer = conn.identity.GetComponent<RoomPlayer>();
            if (roomPlayer != null && !string.IsNullOrEmpty(roomPlayer.nickname))
            {
                ChatManager.Instance.RpcReceiveMessage("System", $"{roomPlayer.nickname}님이 퇴장하셨습니다.");
            }
        }
        else
        {
            Debug.LogWarning($"플레이어 {conn.connectionId}의 연결이 종료됨.");

            GameObject playerToRemove = players.Find(player => player.GetComponent<NetworkIdentity>().connectionToClient == conn);
            if (playerToRemove != null)
            {
                players.Remove(playerToRemove);
                var roomplayer = playerToRemove.GetComponent<RoomPlayer>();
                if (roomplayer != null && !string.IsNullOrEmpty(roomplayer.nickname))
                {
                    ChatManager.Instance.RpcReceiveMessage("System", $"{roomplayer.nickname}님이 퇴장하셨습니다.");
                }
                Debug.Log($"강제 종료된 플레이어 {conn.connectionId} 삭제 완료");
            }                                                   
        }
        Debug.Log($"플레이어 퇴장. 현재 플레이어 수: {players.Count}");
        base.OnServerDisconnect(conn);
    }
    public override GameObject OnRoomServerCreateGamePlayer(NetworkConnectionToClient conn, GameObject roomPlayerObj)
    {
        GameObject obj = Instantiate(playerPrefab);
        GamePlayer gamePlayer = obj.GetComponent<GamePlayer>();

        RoomPlayer roomPlayer = conn.identity.GetComponent<RoomPlayer>();
        if (roomPlayer != null)
        {
            gamePlayer.characterName = roomPlayer.assignedCharacter;
        }

        return obj;
    }

    private string AssignRandomCharacter()
    {
        if (characterPool.Count == 0)
            return "Unknown";

        int index = Random.Range(0, characterPool.Count);
        string selected = characterPool[index];
        characterPool.RemoveAt(index);
        return selected;
    }
}
