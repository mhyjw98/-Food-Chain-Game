using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : NetworkRoomManager
{
    public static RoomManager Instance { get; private set; }

    private List<GameObject> players = new List<GameObject>();

    // ĳ���� ��� 
    private List<string> characterPool = new List<string>()
    {
        "����", "�Ǿ�", "��", "���ֳ̾�", "��", "ī�᷹��",
        "�罿", "����", "�䳢", "���", "û�տ���", "��", "�Ǿ��"
    };

    private Dictionary<NetworkConnectionToClient, string> assignedCharacters = new Dictionary<NetworkConnectionToClient, string>();

    public override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);
        players.Add(conn.identity.gameObject);

        if(NetworkServer.active && players.Count == 1)
        {
            LobbyManager.Instance.ShowStartGameButton();
        }

        Debug.Log($"�÷��̾� {conn.connectionId} ����\n���� �÷��̾� ��: {players.Count}");
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

    public void StartGame()
    {
        if (allPlayersReady) // ��� �÷��̾ �غ� �������� Ȯ��
        {
            Debug.Log("���� ����!");
            ServerAssignCharacters(); // ĳ���� ���� ����
            ServerChangeToGameScene(); // ���� ������ ����
        }
        else
        {
            Debug.LogWarning("��� �÷��̾ �غ���� �ʾҽ��ϴ�.");
        }
    }
    private void ServerAssignCharacters()
    {
        List<NetworkRoomPlayer> roomPlayers = new List<NetworkRoomPlayer>(roomSlots);
        HashSet<string> assignedSet = new HashSet<string>();

        foreach (var player in roomPlayers)
        {
            NetworkConnectionToClient conn = player.connectionToClient;
            string assignedCharacter;

            do
            {
                assignedCharacter = AssignRandomCharacter();
            } while (assignedSet.Contains(assignedCharacter)); // �ߺ� ����

            assignedSet.Add(assignedCharacter);
            assignedCharacters[conn] = assignedCharacter;

            // Ŭ���̾�Ʈ���� ������ ĳ���� ���� ����
            RoomPlayer roomPlayer = conn.identity.GetComponent<RoomPlayer>();
            roomPlayer.CmdSetCharacter(assignedCharacter);

            Debug.Log($"�÷��̾� {conn.connectionId} ĳ���� ����: {assignedCharacter}");
        }
    }
    private string AssignRandomCharacter()
    {
        int randomIndex = Random.Range(0, characterPool.Count);
        return characterPool[randomIndex];
    }

    private void ServerChangeToGameScene()
    {
        ServerChangeScene("GamePlay"); // "GamePlay" ������ ����
    }
}
