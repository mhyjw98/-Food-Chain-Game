using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : NetworkRoomManager
{
    public static RoomManager Instance { get; private set; }

    private List<GameObject> players = new List<GameObject>();

    public override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        players.Add(conn.identity.gameObject);

        Debug.Log($"플레이어 {conn.connectionId} 입장. 현재 플레이어 수: {players.Count}");

        foreach (var player in players)
        {
            Debug.Log($"현재 접속 중: {player.GetComponent<NetworkIdentity>().connectionToClient.connectionId}");
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if(conn.identity != null && conn.identity.gameObject != null)
        {
            players.Remove(conn.identity.gameObject);
            base.OnServerDisconnect(conn);
        }
        else
        {
            Debug.LogWarning($"플레이어 {conn.connectionId}의 연결이 종료됨.");

            //강제 종료된 경우 players 리스트에서 연결 ID를 기반으로 제거
            GameObject playerToRemove = players.Find(player => player.GetComponent<NetworkIdentity>().connectionToClient == conn);
            if (playerToRemove != null)
            {
                players.Remove(playerToRemove);
                Debug.Log($"강제 종료된 플레이어 {conn.connectionId} 삭제 완료");
            }
        }

        Debug.Log($"플레이어 퇴장. 현재 플레이어 수: {players.Count}");
    }
}
