using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// 값은 예전 순서(1·2번은 없앤 위장/탐색 단계) 그대로 둔다. 씬에 저장된 값과 어긋나지 않게 하기 위함
public enum RoundTime { None = 0, Day = 3, Night = 4, End = 5 }
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
    public bool IsNightPhase => currentTime == RoundTime.Night;

    // 현재 낮/밤이 시작된 뒤 흐른 시간(초). timer는 남은 시간 카운트다운이므로 페이즈 길이에서 뺀다
    public float PhaseElapsedSeconds
    {
        get
        {
            float duration = currentTime switch
            {
                RoundTime.Day => dayRoundTime,
                RoundTime.Night => nightRoundTime,
                _ => 0f
            };
            return Mathf.Clamp(duration - timer, 0f, duration);
        }
    }
    public GameObject textUIGroup;

    public BoxCollider2D[] zoneColliders;
    public TextMeshProUGUI timerText;      

    public int maxRounds = 4;

    public static event System.Action<RoundTime> RoundTimeChanged;
    public static event System.Action<float> TimerChanged;

    public readonly SyncList<GamePlayer> players = new SyncList<GamePlayer>();
    private Coroutine roundFlowCo;

    [Header("Game Flow Timing")]
    [Tooltip("모든 플레이어의 준비를 기다리는 최대 시간(초). 넘으면 준비된 인원으로 시작")]
    [SerializeField] private float readyTimeoutSeconds = 30f;
    [Tooltip("모두 준비된 뒤 재생되는 시작 애니메이션 시간(초)")]
    [SerializeField] private float startAnimationSeconds = 2.5f;
    [Tooltip("미션/능력 설정 후 첫 낮이 시작되기까지의 시간(초). 시작 애니메이션과 합쳐 캐릭터 소개 연출(5초) 길이에 맞춘다")]
    [SerializeField] private float preRoundSeconds = 2.5f;
    [Tooltip("결과 화면을 보여준 뒤 로비로 돌아가기까지의 시간(초)")]
    [SerializeField] private float resultDisplaySeconds = 3f;

    // 시작 연출이 이미 시작됐는지. 준비가 늦어 RPC를 놓친 클라이언트가 이 값을 보고 직접 재생한다
    [SyncVar] public bool introStarted;

    // 서버: 게임 씬 준비(스폰 + 역할 수신 + UI 초기화)를 마쳤다고 알려온 플레이어
    private readonly HashSet<GamePlayer> _readyPlayers = new HashSet<GamePlayer>();

    // 서버: 게임 종료 처리가 한 번만 실행되도록 하는 플래그
    private bool _gameEnded;
    // 서버: 밤 체크처럼 여러 명이 연달아 죽는 구간에서는 종료 판정을 미뤘다가 마지막에 한 번만 한다
    private bool _deferEndCheck;

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
        foreach (var gp in FindObjectsByType<GamePlayer>(FindObjectsSortMode.None))
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
            if (p == null) continue;

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
    // 로컬 플레이어의 게임 UI 초기화. 서버 RPC가 아니라 각 클라이언트가 자기 준비가 끝난 시점에
    // 직접 호출한다(GamePlayer.CoReportGameReady). RPC는 버퍼링되지 않아 로딩이 늦은 클라이언트가 놓칠 수 있다
    public void ClientInitLocalUI(GamePlayer player)
    {
        if (player == null || GamePlayUI.Instance == null) return;

        if (!player.isFly)
            GamePlayUI.Instance.ActiveSkyBlock();

        if (fov != null)
            fov.InitLocalPlayer(player.transform);

        TryBuildPlayerSlots();
        GamePlayUI.Instance.InitLocalPlayer(player);
        GamePlayUI.Instance.DeActiveChatUI();
        GamePlayUI.Instance.DeActivePlayerSlotUI();
        GamePlayUI.Instance.SetDisguiseOptions();
        GamePlayUI.Instance.SetPredictOptions();
    }

    [Server]
    public void ServerMarkPlayerReady(GamePlayer player)
    {
        if (player != null)
            _readyPlayers.Add(player);
    }

    // 접속 중인 모든 연결의 플레이어가 준비를 알려왔는지
    [Server]
    private bool ServerAllPlayersReady()
    {
        int expected = NetworkServer.connections.Count;
        if (expected == 0) return false;

        int ready = 0;
        foreach (var p in _readyPlayers)
        {
            if (p != null) ready++;
        }
        return ready >= expected;
    }

    [ClientRpc]
    private void RpcPlayStartIntro()
    {
        var local = NetworkClient.localPlayer;
        if (local == null) return;

        var gamePlayer = local.GetComponent<GamePlayer>();
        if (gamePlayer != null && gamePlayer.IsRoleReady)
            gamePlayer.PlayStartIntro();
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

        if (player.animalType == AnimalType.Meerkat) return 11f;
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
    [Server]
    IEnumerator RoundFlow()
    {
        // 모든 플레이어가 게임 씬 준비를 마칠 때까지 대기(로딩이 느린 클라이언트가 게임에서 빠지지 않도록)
        float waited = 0f;
        while (!ServerAllPlayersReady() && waited < readyTimeoutSeconds)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        if (!ServerAllPlayersReady())
            Debug.LogWarning($"[GameMamager] {readyTimeoutSeconds}초 안에 준비되지 않은 플레이어가 있어 현재 인원으로 시작합니다.");

        // 시작 애니메이션(전원 동시에 캐릭터 소개 연출)
        introStarted = true;
        RpcPlayStartIntro();
        yield return new WaitForSeconds(startAnimationSeconds);

        InitPlayers();
        PlayerAbilitySet();
        SetMissionsToAllPlayers();
        yield return new WaitForSeconds(preRoundSeconds);

        ActiveTextGroup();
        isRoundActive = true;
        // 위장·승리 예측은 별도 단계 없이 위장 오브젝트(DisguiseObject)·예측 오브젝트(PredictObject)에서 한다

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
            _deferEndCheck = true;
            CheckDay(day);
            CheckPredatorSurvival();
            CheckPreySurvival(day);
            _deferEndCheck = false;

            // 그날의 사망 처리를 전부 끝낸 뒤에 종료 조건을 한 번만 판정
            if (ServerTryEndGame())
                yield break;

            RpcOpenMap();
        }

        ServerEndGame();
    }
    [Server]
    void CheckDay(int day)
    {
        foreach (var player in players)
        {
            if (player == null || player.Abilities == null) continue;

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
            if (player == null || !player.isAlive) continue;

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
            if (player == null || !player.isAlive) continue;
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
            if (gp == null) continue;

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

        // 올빼미처럼 다른 플레이어의 승패를 따르는 조건이 있어 전원의 결과를 한 번에 계산한다
        var results = GameResultEvaluator.EvaluateAll(players);

        foreach (var player in players)
        {
            if (player == null) continue;

            // 승리 조건이 정의되지 않은 캐릭터는 패배 처리(예외로 결과 표시/로비 복귀가 누락되지 않게)
            bool isWinner = results.TryGetValue(player, out bool won) && won;
            player.isWin = isWinner;

            // 결과는 RPC 인자로 본인에게 직접 보낸다. 같은 프레임에 쓴 SyncVar(isWin)는 RPC보다 늦게 도착하므로
            // 클라이언트가 RPC 시점에 isWin을 읽으면 항상 이전 값(false)을 보게 된다
            player.TargetShowGameResult(isWinner);

            Debug.Log($"[Evaluate] {player.characterName} 결과: {(isWinner ? "승리" : "패배")}");
        }
        StartCoroutine(WaitAndReturnToRoom());
    }

    [Server]
    IEnumerator WaitAndReturnToRoom()
    {
        yield return new WaitForSeconds(resultDisplaySeconds);

        if (NetworkManager.singleton == null) yield break;

        var roomManager = NetworkManager.singleton as NetworkRoomManager;
        string roomScene = roomManager != null && !string.IsNullOrEmpty(roomManager.RoomScene)
            ? roomManager.RoomScene
            : "GameRoom";

        NetworkManager.singleton.ServerChangeScene(roomScene);
    }

    // 사망 처리 직후 GamePlayer.Die()에서 호출된다
    [Server]
    public void ServerOnPlayerDied(GamePlayer dead)
    {
        if (_deferEndCheck) return;

        ServerTryEndGame();
    }

    // 접속 종료/튕김으로 게임에서 나간 플레이어 처리(RoomManager.OnServerDisconnect에서 오브젝트 파괴 전에 호출).
    // 게임 진행 중에 나가면 사망으로 처리한다(시체 생성, 사망 원인 = 퇴장 — 임시 구현).
    // 그 뒤 플레이어 목록에서 빼서 이후의 굶주림/미션/승패 판정에서 제외한다(오브젝트가 곧 파괴되므로)
    [Server]
    public void ServerHandlePlayerLeft(GamePlayer leaver)
    {
        if (leaver == null) return;

        string leaverNickname = leaver.nickname;

        if (isRoundActive && !_gameEnded && leaver.isAlive && players.Contains(leaver))
        {
            leaver.Die(new DeathInfo
            {
                Reason = DeathReason.Left,
                KillerNetId = 0,
                KillerType = AnimalType.None
            });
        }

        BreakSymbiosisIfAny(leaver);

        bool wasInGame = players.Remove(leaver);
        if (!wasInGame || _gameEnded) return;

        foreach (var p in players)
        {
            if (p != null)
                p.TargetReceiveMessage(0, "System", $"{leaverNickname}님이 게임에서 나갔습니다.");
        }

        ServerTryEndGame();
    }

    // 종료 조건: 포식자가 있던 게임에서 살아있는 포식자가 없어짐(사망 또는 나감) / 생존자가 없음
    [Server]
    private bool ServerTryEndGame()
    {
        if (_gameEnded) return true;
        if (!isRoundActive) return false;

        int aliveCount = 0;
        int alivePredatorCount = 0;

        foreach (var p in players)
        {
            if (p == null || !p.isAlive) continue;

            aliveCount++;
            if (p.isPredator) alivePredatorCount++;
        }

        deadPredatorCount = Mathf.Max(0, predatorCount - alivePredatorCount);

        bool allPredatorsGone = predatorCount > 0 && alivePredatorCount == 0;
        bool noSurvivorsLeft = aliveCount == 0;

        if (!allPredatorsGone && !noSurvivorsLeft) return false;

        ServerEndGame();
        return true;
    }

    [Server]
    private void ServerEndGame()
    {
        if (_gameEnded) return;
        _gameEnded = true;

        isRoundActive = false;
        DeActiveTextGroup();
        currentTime = RoundTime.End;
        timerText.text = "";

        if (roundFlowCo != null)
        {
            StopCoroutine(roundFlowCo);
            roundFlowCo = null;
        }

        EvaluateGameResult();
    }

}
