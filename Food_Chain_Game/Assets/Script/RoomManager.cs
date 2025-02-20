using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : NetworkRoomManager
{
    public static RoomManager Instance { get; private set; }

    private List<GameObject> players = new List<GameObject>();

    // 캐릭터 목록 
    private List<string> characterPool = new List<string>()
    {
        "사자", "악어", "매", "하이애나", "뱀", "카멜레온",
        "사슴", "수달", "토끼", "까마귀", "청둥오리", "쥐", "악어새"
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

        Debug.Log($"플레이어 {conn.connectionId} 입장\n현재 플레이어 수: {players.Count}");
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

    public void StartGame()
    {
        if (allPlayersReady) // 모든 플레이어가 준비 상태인지 확인
        {
            Debug.Log("게임 시작!");
            ServerAssignCharacters(); // 캐릭터 랜덤 배정
            ServerChangeToGameScene(); // 게임 씬으로 변경
        }
        else
        {
            Debug.LogWarning("모든 플레이어가 준비되지 않았습니다.");
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
            } while (assignedSet.Contains(assignedCharacter)); // 중복 방지

            assignedSet.Add(assignedCharacter);
            assignedCharacters[conn] = assignedCharacter;

            // 클라이언트에게 배정된 캐릭터 정보 전송
            RoomPlayer roomPlayer = conn.identity.GetComponent<RoomPlayer>();
            roomPlayer.CmdSetCharacter(assignedCharacter);

            Debug.Log($"플레이어 {conn.connectionId} 캐릭터 배정: {assignedCharacter}");
        }
    }
    private string AssignRandomCharacter()
    {
        int randomIndex = Random.Range(0, characterPool.Count);
        return characterPool[randomIndex];
    }

    private void ServerChangeToGameScene()
    {
        ServerChangeScene("GamePlay"); // "GamePlay" 씬으로 변경
    }
}
