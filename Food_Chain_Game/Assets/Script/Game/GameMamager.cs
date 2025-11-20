using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum RoundTime { None, Disguise, Exploration, Day, Night, End }
public class GameMamager : NetworkBehaviour
{
    public static GameMamager Instance;

    [SyncVar] public bool isRoundActive;
    [SyncVar] public int currentRound = 0;
    [SyncVar(hook = nameof(OnRoundTimeChanged))] public RoundTime currentTime = RoundTime.None;
    public bool IsExplorationPhase => currentTime == RoundTime.Exploration;
    public bool IsNightPhase => currentTime == RoundTime.Night;
    public GameObject textUIGroup;

    public BoxCollider2D[] zoneColliders;
    public TextMeshProUGUI timerText;      

    public int maxRounds = 4;

    private List<GamePlayer> players;
    private float disguiseTime = 10f;
    private float explorationRoundTime = 5f;
    private float dayRoundTime = 10f;
    private float nightRoundTime = 20f;    

    [SyncVar]private float timer = 5f;

    public static Dictionary<AnimalType, int> MaxHungryRounds = new()
    {
        { AnimalType.Lion, 1 },
        { AnimalType.Crocodile, 2 },
        {AnimalType.Eagle, 2 },
        { AnimalType.Hyena, 3 }
    };
    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (isServer)
        {
            if (!isRoundActive) return;
            timer -= Time.deltaTime;
        }

        if (isRoundActive)
            timerText.text = $"{Mathf.CeilToInt(timer)}초";
    }
    
    public override void OnStartServer()
    {
        StartCoroutine(RoundFlow());
    }

    [Server]
    IEnumerator ForceReturnToHome()
    {
        RpcBlockMap();
        foreach (var col in zoneColliders)
            col.enabled = false;

        yield return new WaitForSeconds(1.5f);

        foreach (var col in zoneColliders)
            col.enabled = true;

        yield return new WaitForSeconds(1.5f);

        foreach (GamePlayer player in FindObjectsOfType<GamePlayer>())
        {           
            if (player.currentZone != player.homeZone && player.isReturn)
            {               
                Vector3 homePos = SpawnManager.Instance.GetZonePosition(player.homeZone);
                player.transform.position = homePos;
                player.currentZone = player.homeZone;
            }           
        }        
    }
    [ClientRpc]
    void RpcBlockSky()
    {
        StartCoroutine(BlockSkyWhenReady());
    }
    private IEnumerator BlockSkyWhenReady()
    {
        while (NetworkClient.localPlayer == null || GamePlayUI.Instance == null)
            yield return null;

        GamePlayer player = NetworkClient.localPlayer.GetComponent<GamePlayer>();
        if (player == null)
        {
            Debug.LogError($"[BlockSkyWhenReady] player가 null값");
            yield break;
        }          
        if (!player.isFly)
            GamePlayUI.Instance.ActiveSkyBlock();
    }
    [ClientRpc]
    void RpcBlockMap()
    {
        GamePlayUI.Instance.ActiveBlock();
    }

    [ClientRpc]
    void RpcOpenMap()
    {
        GamePlayUI.Instance.DeActiveBlock();
    }

    [ClientRpc]
    void ActiveTextGroup()
    {        
        textUIGroup.SetActive(true);
    }
    [ClientRpc]
    void DeActiveTextGroup()
    {       
        textUIGroup.SetActive(false);
    }
    [ClientRpc]
    void SetDisguiseUI()
    {
        GamePlayUI.Instance.SetDisguiseOptions();
    }
    [ClientRpc]
    void RPCShowDisguiseUI()
    {
        GamePlayUI.Instance.ShowDisguiseUI();
    }

    [Server]
    IEnumerator RoundFlow()
    {
        // 시작 애니메이션
        yield return new WaitForSeconds(2.5f);
        RpcBlockSky();
        SetDisguiseUI();
        players = new List<GamePlayer>(FindObjectsOfType<GamePlayer>());
        DeleteRoomPlayer();
        yield return new WaitForSeconds(1f);
        RpcSetupPlayerList(players.Select(p => p.netId).ToArray());
        yield return new WaitForSeconds(0.5f);       
        PlayerMove.RegisterInputField();
        yield return new WaitForSeconds(1f);

        // 위장 시간
        isRoundActive = true;
        currentTime = RoundTime.Disguise;
        timer = disguiseTime;
        RPCShowDisguiseUI();             
        yield return new WaitForSeconds(disguiseTime);

        // 탐색시간        
        currentTime = RoundTime.Exploration;
        timer = explorationRoundTime;       
        yield return new WaitForSeconds(explorationRoundTime);        

        // 라운드 시작
        for (int i = 1; i <= 4; i++)
        {
            currentRound = i;

            // 낮 시간
            currentTime = RoundTime.Day;
            timer = dayRoundTime;
            yield return new WaitForSeconds(dayRoundTime);

            // 밤 되기 전 대기시간
            isRoundActive = false;
            DeActiveTextGroup();           
            yield return ForceReturnToHome(); // 대기시간
            isRoundActive = true;
            ActiveTextGroup();
            

            // 밤 시간
            currentTime = RoundTime.Night;
            timer = nightRoundTime;
            yield return new WaitForSeconds(nightRoundTime);

            // 낮 되기 전 대기 시간
            isRoundActive = false;
            DeActiveTextGroup();
            CheckPredatorSurvival();
            EndNight();
            if(i < 4)
            {
                yield return new WaitForSeconds(3f); // 대기시간
                RpcOpenMap();
                isRoundActive = true;
                ActiveTextGroup();
            }            
        }

        isRoundActive = false;
        DeActiveTextGroup();
        currentTime = RoundTime.End;
        timerText.text = "";

        EvaluateGameResult(); // 승패 판단    
    }
    void DeleteRoomPlayer()
    {
        RoomPlayer[] roomPlayers = FindObjectsOfType<RoomPlayer>();

        if (NetworkServer.active)
        {
            foreach (var rp in roomPlayers)
            {
                NetworkServer.Destroy(rp.gameObject);
                ((RoomManager)RoomManager.singleton).roomPlayers.Remove(rp);
            }
        }
    }
    [Server]
    void EvaluateCrowPredictions(List<GamePlayer> players)
    {
        foreach (var player in players)
        {
            if (player.animalType == AnimalType.Crow)
            {
                // 예측한 캐릭터가 실제로 승리했는지 체크
                bool isSuccess = players.Any(p =>
                    p.animalType == player.predictedWinner && p.isWin);

                player.isWin = isSuccess;              
            }
        }
    }

    [Server]
    void EndNight()
    {
        foreach (GamePlayer player in FindObjectsOfType<GamePlayer>())
        {
            if (player.currentZone != player.homeZone)
            {
                player.isReturn = true;
            }
            else
            {
                player.isReturn = false;
            }
        }
    }
    [Server]
    void CheckPredatorSurvival()
    {
        foreach (var player in FindObjectsOfType<GamePlayer>())
        {
            if (!player.isAlive) continue;

            if (player.isPredator)
            {
                if (!player.hasEate)
                {
                    player.hungryStreak++;

                    int maxStreak = MaxHungryRounds[player.animalType];
                    if (player.hungryStreak >= maxStreak)
                    {
                        Debug.Log($"{player.characterName}는 {player.hungryStreak}라운드 동안 굶어 사망했습니다.");
                        player.isAlive = false;
                    }
                }
                else
                {
                    player.hungryStreak = 0;
                }

                player.hasAttacked = false;
                player.hasEate = false;
            }
        }
    }

    void OnRoundTimeChanged(RoundTime oldVal, RoundTime newVal)
    {
        GamePlayUI.Instance.UpdateRoundText(currentRound, newVal);
    }

    [Server]
    void EvaluateGameResult()
    {
        // 각 캐릭터 승리 조건 판단 로직
        Debug.Log("게임 종료! 승패 판단 시작");       

        foreach (var player in players)
        {
            if (player.animalType == AnimalType.Crow)
                continue;
            
            IWinCondition winCondition = WinCondutionFactory.GetCondition(player.animalType);

            bool isWinner = winCondition.Evaluate(player, players);
            player.isWin = isWinner;

            Debug.Log($"[Evaluate] {player.characterName} 결과: {(isWinner ? "승리" : "패배")}");
        }
        EvaluateCrowPredictions(players);
        RpcShowGameResult();

        StartCoroutine(WaitAndReturnToRoom());
    }

    [Server]
    IEnumerator WaitAndReturnToRoom()
    {
        yield return new WaitForSeconds(3f);

        // Server가 씬 이동 명령
        NetworkManager.singleton.ServerChangeScene("GameRoom");
    }

    [ClientRpc]
    void RpcSetupPlayerList(uint[] netIds)
    {
        var players = new List<GamePlayer>();
        foreach (var id in netIds)
        {
            if (NetworkClient.spawned.TryGetValue(id, out var obj))
            {
                players.Add(obj.GetComponent<GamePlayer>());
            }
        }

        PlayerSlotUI.Instance.CreateSlots(players);
    }

    [ClientRpc]
    void RpcShowGameResult()
    {
        GamePlayer localPlayer = NetworkClient.localPlayer.GetComponent<GamePlayer>();

        if (localPlayer != null)
        {
            GamePlayUI.Instance.ShowResult(localPlayer.isWin);
        }
    }
}
