using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public enum RoundTime { None, Disguise, Exploration, Day, Night, End }
public class GameMamager : NetworkBehaviour
{
    public static GameMamager Instance;

    [SerializeField] private FOVMaskController2D fov;

    [SyncVar] public bool isRoundActive;
    [SyncVar] public int currentRound = 0;
    [SyncVar(hook = nameof(OnRoundTimeChanged))] public RoundTime currentTime = RoundTime.None;
    [SyncVar(hook = nameof(OnTimerChanged))] public float timer;
    [SyncVar] public int predatorCount = 0;
    [SyncVar] public int deadPredatorCount = 0;
    public bool IsExplorationPhase => currentTime == RoundTime.Exploration;
    public bool IsNightPhase => currentTime == RoundTime.Night;
    public GameObject textUIGroup;

    public BoxCollider2D[] zoneColliders;
    public TextMeshProUGUI timerText;      

    public int maxRounds = 4;

    public static event System.Action<RoundTime> RoundTimeChanged;
    public static event System.Action<float> TimerChanged;

    public readonly SyncList<GamePlayer> players = new SyncList<GamePlayer>();
    private Coroutine roundFlowCo;

    //private float disguiseTime = 10f;
    //private float explorationRoundTime = 5f;
    public float dayRoundTime = 20f;
    private float nightRoundTime = 20f;    

    public static Dictionary<AnimalType, int> MaxHungryRounds = new()
    {
        { AnimalType.Wolf, 1 },
        { AnimalType.Crocodile, 2 },
        {AnimalType.Hawk, 2 },
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
    public override void OnStartClient()
    {
        base.OnStartClient();
        players.Callback += OnPlayersChanged;
    }
    public override void OnStartServer()
    {
        roundFlowCo = StartCoroutine(RoundFlow());
    }
    private void OnPlayersChanged(SyncList<GamePlayer>.Operation op, int index, GamePlayer oldItem, GamePlayer newItem)
    {
        TryBuildPlayerSlots();
    }

    private void TryBuildPlayerSlots()
    {
        if (PlayerSlotUI.Instance == null)
            return;

        var list = new List<GamePlayer>(players.Count);
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null)
                list.Add(players[i]);
        }

        PlayerSlotUI.Instance.CreateSlots(list);
    }
    void InitPlayers()
    {
        predatorCount = 0;
        deadPredatorCount = 0;

        players.Clear();
        foreach (var gp in FindObjectsOfType<GamePlayer>())
        {
            players.Add(gp);           
            if (gp.isPredator)
                predatorCount++;
        }           
    }

    void PlayerAbilitySet()
    {
        GamePlayer ostrich = null;
        GamePlayer zebra = null;

        foreach (var p in players)
        {
            if (p.animalType == AnimalType.Ostrich) ostrich = p;
            else if (p.animalType == AnimalType.Zebra) zebra = p;
        }

        if (ostrich == null || zebra == null) return;

        var oa = ostrich.GetComponent<PreySymbiosisAbility>();
        var za = zebra.GetComponent<PreySymbiosisAbility>();
        if (oa == null || za == null) return;

        oa.ServerSetPartner(zebra.netId);
        za.ServerSetPartner(ostrich.netId);     
    }
    [ClientRpc]
    void SetGameUI()
    {
        StartCoroutine(SetUIWhenReady());
    }
    private IEnumerator SetUIWhenReady()
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

        fov.InitLocalPlayer(player.transform);
        TryBuildPlayerSlots();
        GamePlayUI.Instance.InitLocalPlayer(player);
        GamePlayUI.Instance.DeActiveChatUI();
        GamePlayUI.Instance.DeActivePlayerSlotUI();
        GamePlayUI.Instance.SetDisguiseOptions();
        GamePlayUI.Instance.SetPredictOptions();
    }
    [ClientRpc]
    private void RpcOnDayNightChanged(bool isNight)
    {
        var local = NetworkClient.localPlayer;
        if (local == null) return;

        var gamePlayer = local.GetComponent<GamePlayer>();
        if (gamePlayer == null || !gamePlayer.isAlive) return;

        float baseVision = GetVisionForAnimal(gamePlayer, isNight);       

        var sym = gamePlayer.GetComponent<PreySymbiosisAbility>();
        if (sym != null && sym.isActiveAndEnabled && sym.partnerNetId != 0)
        {
            sym.ClientOnDayNightVisionUpdated(isNight, baseVision);
            return;
        }

        var myVision = gamePlayer.localVision;
        if (myVision != null)
            myVision.TransitionToVision(baseVision, isNight);      
    }

    private float GetVisionForAnimal(GamePlayer player, bool isNight)
    {
        if (!isNight) return 11f;

        if(player.isPredator) return 6f;

        if (player.animalType == AnimalType.Squirrel) return 11f;
        else return 8f;
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
    void RPCShowDisguiseUI()
    {
        GamePlayUI.Instance.ShowDisguiseUI();
    }
    [ClientRpc]
    void RPCShowPredictUI()
    {
        GamePlayUI.Instance.ShowPredictUI();
    }
    [ClientRpc]
    void RPCCheckPredict()
    {
        GamePlayUI.Instance.CheckPredict();
    }
    [Server]
    IEnumerator RoundFlow()
    {
        // 시작 애니메이션
        yield return new WaitForSeconds(2.5f);
        InitPlayers();
        PlayerAbilitySet();
        SetGameUI();                        
        SetMissionsToAllPlayers();
        yield return new WaitForSeconds(2.5f);

        ActiveTextGroup();
        isRoundActive = true;
        // 위장 시간
        //currentTime = RoundTime.Disguise;
        //timer = disguiseTime;
        //RPCShowDisguiseUI();             
        //yield return new WaitForSeconds(disguiseTime);

        // 탐색시간        
        //currentTime = RoundTime.Exploration;
        //timer = explorationRoundTime;
        //RPCShowPredictUI();
        //yield return new WaitForSeconds(explorationRoundTime);
        //RPCCheckPredict();

        // 라운드 시작
        for (int day = 1; day <= maxRounds; day++)
        {
            currentRound = day;

            // 낮 시간
            currentTime = RoundTime.Day;
            timer = dayRoundTime;
            if(day != 1) RpcOnDayNightChanged(IsNightPhase);
            yield return new WaitForSeconds(dayRoundTime);
            
            // 밤 시간
            currentTime = RoundTime.Night;
            timer = nightRoundTime;
            RpcBlockMap();
            RpcOnDayNightChanged(IsNightPhase);
            yield return new WaitForSeconds(nightRoundTime);

            // 체크
            CheckDay(day);
            CheckPredatorSurvival();
            CheckPreySurvival(day);
            RpcOpenMap();
        }

        isRoundActive = false;
        DeActiveTextGroup();
        currentTime = RoundTime.End;
        timerText.text = "";

        EvaluateGameResult();  
    }
    [Server]
    void CheckDay(int day)
    {
        foreach (var player in players)
        {
            var targetAbilities = player.Abilities;
            for (int i = 0; i < targetAbilities.Length; i++)
                targetAbilities[i].OnNewDay(day);
        }        
    }

    [Server]
    void CheckPredatorSurvival()
    {
        foreach (var player in players)
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
                        player.Die(new DeathInfo
                        {
                            Reason = DeathReason.Hunger,
                            KillerNetId = 0,
                            KillerType = AnimalType.None
                        });
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
    [Server]
    void CheckPreySurvival(int day)
    {
        foreach (var player in players)
        {
            if (!player.isAlive) continue;
            if (player.isPredator) continue;

            int count = 0;
            foreach(var misson in player.Missions)
                if(misson.Status == MissionStatus.Completed) count++;

            if(count < day)
            {
                player.Die(new DeathInfo
                {
                    Reason = DeathReason.MissionFail,
                    KillerNetId = 0,
                    KillerType = AnimalType.None
                });
                BreakSymbiosisIfAny(player);
            }                           
        }
    }
    [Server]
    private void BreakSymbiosisIfAny(GamePlayer dead)
    {
        if (dead == null) return;

        var a = dead.GetComponent<PreySymbiosisAbility>();
        if (a == null) return;

        uint partnerId = a.partnerNetId;
        if (partnerId == 0) return;

        a.ServerSetPartner(0);

        if (NetworkServer.spawned.TryGetValue(partnerId, out var partnerIdentity) && partnerIdentity != null)
        {
            var partnerGp = partnerIdentity.GetComponent<GamePlayer>();
            if (partnerGp != null)
            {
                var b = partnerGp.GetComponent<PreySymbiosisAbility>();
                if (b != null && b.partnerNetId == dead.netId)
                    b.ServerSetPartner(0);
            }
        }
    }
    void OnRoundTimeChanged(RoundTime oldVal, RoundTime newVal)
    {
        RoundTimeChanged?.Invoke(newVal);
        GamePlayUI.Instance.UpdateRoundText(currentRound, newVal);
    }
    void OnTimerChanged(float oldVal, float newVal)
    {
        TimerChanged?.Invoke(newVal);
    }

    [Server]
    public void SetMissionsToAllPlayers()
    {
        foreach (var gp in players)
        {
            var missionTypes = MissionSelector.GetRandomMissions(gp.animalType, 5);

            gp.Missions.Clear();
            foreach (var type in missionTypes)
                gp.Missions.Add(new MissionSlot { Type = type, Status = MissionStatus.NotStarted });

            gp.ActivateAbilitie();
        }
    }

    [Server]
    void EvaluateGameResult()
    {
        Debug.Log("게임 종료! 승패 판단 시작");       

        foreach (var player in players)
        {           
            IWinCondition winCondition = WinCondutionFactory.GetCondition(player.animalType);

            bool isWinner = winCondition.Evaluate(player, players);
            player.isWin = isWinner;

            Debug.Log($"[Evaluate] {player.characterName} 결과: {(isWinner ? "승리" : "패배")}");
        }
        RpcShowGameResult();

        StartCoroutine(WaitAndReturnToRoom());
    }

    [Server]
    IEnumerator WaitAndReturnToRoom()
    {
        yield return new WaitForSeconds(3f);

        NetworkManager.singleton.ServerChangeScene("GameRoom");
    }

    public void CheckGameOver(bool wasPredator)
    {
        if (!isRoundActive) return;

        if (wasPredator)
            deadPredatorCount++;

        bool allPredatorsDead = deadPredatorCount == predatorCount;
        bool noSurvivorsLeft = players.Count > 0 && players.All(p => p == null || !p.isAlive);

        if (allPredatorsDead || noSurvivorsLeft)
        {
            isRoundActive = false;
            DeActiveTextGroup();
            currentTime = RoundTime.End;
            timerText.text = "";

            if (roundFlowCo != null)
                StopCoroutine(roundFlowCo);

            EvaluateGameResult();
        }
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

    [Command(requiresAuthority = false)]
    public void RequestPlayerType(uint netId, string nickname, NetworkConnectionToClient sender = null)
    {
        if (sender == null)
            return;

        if (!NetworkServer.spawned.TryGetValue(netId, out var identity))
            return;

        var target = identity.GetComponent<GamePlayer>();
        if (target == null)
            return;

        TargetRpcInvestigationResult(sender, netId, target.nickname, target.animalType, target.isPredator);
    }

    [TargetRpc]
    private void TargetRpcInvestigationResult(NetworkConnectionToClient conn, uint targetNetId, string nickname, AnimalType type, bool isPredator)
    {
        if (GamePlayUI.Instance == null) return;
        GamePlayUI.Instance.ActiveInvestigationResult(targetNetId, nickname, type, isPredator);
    }
}
