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

        Debug.Log($"�÷��̾� {conn.connectionId} ����. ���� �÷��̾� ��: {players.Count}");

        foreach (var player in players)
        {
            Debug.Log($"���� ���� ��: {player.GetComponent<NetworkIdentity>().connectionToClient.connectionId}");
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
            Debug.LogWarning($"�÷��̾� {conn.connectionId}�� ������ �����.");

            //���� ����� ��� players ����Ʈ���� ���� ID�� ������� ����
            GameObject playerToRemove = players.Find(player => player.GetComponent<NetworkIdentity>().connectionToClient == conn);
            if (playerToRemove != null)
            {
                players.Remove(playerToRemove);
                Debug.Log($"���� ����� �÷��̾� {conn.connectionId} ���� �Ϸ�");
            }
        }

        Debug.Log($"�÷��̾� ����. ���� �÷��̾� ��: {players.Count}");
    }
}
