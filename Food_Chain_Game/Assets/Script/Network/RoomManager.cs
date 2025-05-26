using Mirror;
using Mirror.Examples.Chat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
using static Mirror.BouncyCastle.Math.EC.ECCurve;
using static Mirror.NetworkRuntimeProfiler;

public class RoomManager : NetworkRoomManager
{

    public static RoomManager Instance { get; private set; }

    public RoomHost roomHost;
    public GameObject chatManagerPrefab;

    public List<GameObject> players = new List<GameObject>();

    private List<string> GetCharacterPool(int playerCount)
    {
        List<string> baseCharacters = new()
    {
        "Lion", "Snake", "Crocodile", "Mouse", "Rabbit", "Deer", "Otter", 
    };

        List<string> additionalCharacters = new()
    {
        "Mallard", "Eagle", "Plover", "Hyena","Chameleon", "Crow" 
    };

        if (playerCount < 7)
        {
            int clampedCount = Mathf.Clamp(playerCount, 0, baseCharacters.Count);
            return baseCharacters.GetRange(0, clampedCount);
        }
        else
        {
            int extraCount = Mathf.Clamp(playerCount - 7, 0, additionalCharacters.Count);
            baseCharacters.AddRange(additionalCharacters.GetRange(0, extraCount));
            return baseCharacters;
        }
    }
    public int maxPlayerCount = 1;
    private readonly HashSet<NetworkConnectionToClient> joinedConnections = new();

    public override void Awake()
    {
        if(Instance == null)
        {
            base.Awake();
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }          
    }
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        NetworkRoomPlayer basePlayer = Instantiate(roomPlayerPrefab);
        GameObject roomPlayerObj = basePlayer.gameObject;
        roomPlayerObj.transform.position = SpawnManager.Instance.GetAvailableSpawnPosition();
        NetworkServer.AddPlayerForConnection(conn, roomPlayerObj);

        RoomPlayer roomPlayer = basePlayer as RoomPlayer;

        if (!players.Contains(roomPlayerObj))
            players.Add(roomPlayerObj);

        string code = RoomSessionData.CurrentRoomCode;
        roomPlayer.roomCode = code;

        uint playerId = conn.identity.netId;

        if (!joinedConnections.Contains(conn))
        {
            roomHost.ComeAndGoing(roomPlayer.roomCode, 1);
            joinedConnections.Add(conn);
            Debug.Log($"[Room] 입장 처리 완료: {conn.connectionId}");
        }

        if (players.Count == maxPlayerCount)
        {
            Debug.Log("모든 인원 입장 완료! 캐릭터 배정 시작");
            ServerAssignCharacters();
        }       
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        var roomPlayer = conn.identity.GetComponent<RoomPlayer>();
        if (conn.identity != null && conn.identity.gameObject != null)
        {
            players.Remove(conn.identity.gameObject);

            
            if (roomPlayer != null && !string.IsNullOrEmpty(roomPlayer.nickname))
            {
                var chatManager = FindObjectOfType<ChatManager>();
                chatManager.AddSystemMessage("System", $"{roomPlayer.nickname}님이 퇴장하셨습니다.");
            }
            uint playerId = conn.identity.netId;

            if (!joinedConnections.Contains(conn))
            {
                roomHost.ComeAndGoing(roomPlayer.roomCode, -1);
                joinedConnections.Add(conn);
                Debug.Log($"[Room] 입장 처리 완료: {conn.connectionId}");
            }
        }
        else
        {
            Debug.LogWarning($"플레이어 {conn.connectionId}의 연결이 종료됨.");

            GameObject playerToRemove = players.Find(player => player.GetComponent<NetworkIdentity>().connectionToClient == conn);
            if (playerToRemove != null)
            {
                players.Remove(playerToRemove);
                if (playerToRemove.TryGetComponent(out RoomPlayer fallbackPlayer))
                {
                    if (!string.IsNullOrEmpty(fallbackPlayer.nickname))
                    {
                        var chatManager = FindObjectOfType<ChatManager>();
                        chatManager.AddSystemMessage("System", $"{fallbackPlayer.nickname}님이 퇴장하셨습니다.");
                    }

                    uint playerId = conn.identity.netId;

                    if (!joinedConnections.Contains(conn))
                    {
                        roomHost.ComeAndGoing(roomPlayer.roomCode, -1);
                        joinedConnections.Add(conn);
                        Debug.Log($"[Room] 입장 처리 완료: {conn.connectionId}");
                    }
                }
            }                                                   
        }        
        Debug.Log($"플레이어 퇴장. 현재 플레이어 수: {players.Count}");
        string roomCode = roomPlayer.roomCode;
        roomHost.ComeAndGoing(roomCode, -1);

        base.OnServerDisconnect(conn);
    }
    public override GameObject OnRoomServerCreateGamePlayer(NetworkConnectionToClient conn, GameObject roomPlayerObj)
    {
        GameObject gamePlayerObj = Instantiate(playerPrefab);       

        GamePlayer gamePlayer = gamePlayerObj.GetComponent<GamePlayer>();       

        RoomPlayer roomPlayer = roomPlayerObj.GetComponent<RoomPlayer>();
        if (roomPlayer != null)
        {
            gamePlayer.characterName = roomPlayer.assignedCharacter;
            gamePlayer.nickname = roomPlayer.nickname;
            if (CharacterZoneData.CharacterHomeZone.TryGetValue(roomPlayer.assignedCharacter, out ZoneType zone))
            {
                gamePlayer.homeZone = zone;
            }
            gamePlayerObj.transform.position = SpawnManager.Instance.GetRandomPositionInZone(gamePlayer.homeZone);
            gamePlayer.SetAnimalType(roomPlayer.assignedCharacter);
        }

        NetworkServer.Destroy(roomPlayerObj);
        players.Remove(roomPlayerObj);

        return gamePlayerObj;
    }
    private void ServerAssignCharacters()
    {
        Debug.Log("캐릭터 배정 로직 실행");

        var pool = GetCharacterPool(maxPlayerCount);
        HashSet<string> assigned = new();

        foreach (var playerObj in players)
        {
            NetworkConnectionToClient conn = playerObj.GetComponent<NetworkIdentity>().connectionToClient;

            string assignedCharacter;
            do
            {
                assignedCharacter = pool[UnityEngine.Random.Range(0, pool.Count)];
            } while (assigned.Contains(assignedCharacter));

            assigned.Add(assignedCharacter);

            RoomPlayer roomPlayer = playerObj.GetComponent<RoomPlayer>();
            if (roomPlayer != null)
            {
                roomPlayer.SetCharacter(assignedCharacter);
                Debug.Log($"플레이어 {conn.connectionId} 캐릭터 배정: {assignedCharacter}");
            }
        }
    }

    public void StartGame()
    {
        if (players.Count < maxPlayerCount)
        {
            Debug.LogWarning($"[RoomManager] 현재 인원 {players.Count}/{maxPlayerCount} → 인원 부족으로 게임 시작 불가");
            return;
        }

        ServerChangeScene("GamePlay");
    }

    public void TryAssignHost(RoomPlayer player)
    {
        if (string.IsNullOrEmpty(RoomSessionData.PreviousHostId))
        {
            RoomSessionData.PreviousHostId = player.userId;
            player.isHost = true;
            Debug.Log($"[RoomManager] 호스트 지정됨: {player.userId}");
        }
        else if (player.userId == RoomSessionData.PreviousHostId)
        {
            player.isHost = true;
            Debug.Log($"[RoomManager] 기존 호스트 복원됨: {player.userId}");
        }
        else
        {
            player.isHost = false;
            Debug.Log($"[RoomManager] 일반 참여자: {player.userId}");
        }
    }   
}
