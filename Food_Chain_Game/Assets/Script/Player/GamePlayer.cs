using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 코드 이름과 화면 이름이 다른 것: Owl = 올빼미, Hippo = 하마, Chameleon = 카멜레온, Meerkat = 미어캣 (화면 이름은 AnimalNameMap / CharacterConfig)
public enum AnimalType { Wolf, Crocodile, Hawk, Hyena, Scorpion, Crow, Plover, Meerkat, Hippo, Ostrich, Zebra, Owl, Chameleon, None }
public enum PredatorType { Prey, Hyena, Hawk, Crocodile, Wolf, Scorpion }
// Left: 게임 도중 퇴장(접속 종료/튕김). 임시 구현 — 추후 수정 예정
public enum DeathReason { None = 0, Hunger, MissionFail, KilledByAttack, CounterKill, Left }
public struct DeathInfo { public DeathReason Reason; public uint KillerNetId; public AnimalType KillerType; }
[RequireComponent(typeof(PlayerPrivateState))]
public class GamePlayer : NetworkBehaviour
{
    // 역할 정보: 서버와 본인(오너)만 가진다. 다른 클라이언트에는 보내지 않으므로 SyncVar가 아니며,
    // 오너는 스폰 후 CmdRequestRole → TargetReceiveRole로 받는다. 다른 플레이어의 값은 클라이언트에서 항상 기본값이다.
    [NonSerialized] public string characterName;
    [NonSerialized] public AnimalType animalType = AnimalType.None;
    [NonSerialized] public PredatorType predatorType = PredatorType.Prey;
    [NonSerialized] public bool isPredator;
    [NonSerialized] public bool isFly;
    [NonSerialized] public bool canScan;
    [NonSerialized] public bool canPredict;
    public bool IsRoleReady { get; private set; }

    // 사망 원인: 서버 전용 (시체 생성/능력 판정에만 사용)
    [NonSerialized] public AnimalType lastKillerType;
    [NonSerialized] public DeathReason lastDeathReason;
    [NonSerialized] public uint lastKillerNetId;

    // 거주지: 서버 전용. 전원에게 보내면 거주지로 동물을 추정할 수 있다
    [NonSerialized] public ZoneType homeZone;

    // 전원에게 공개되는 값
    [SyncVar(hook = nameof(OnNicknameChanged))] public string nickname;
    [SyncVar(hook = nameof(OnColorChanged))] public Color gpColor = Color.white;
    // 로비에서 고른 의상(CostumeCatalog 번호). 겉모습이라 모두에게 보인다
    [SyncVar(hook = nameof(OnCostumeChanged))] public int costumeIndex;
    [SyncVar] public bool isReturn;
    [SyncVar(hook = nameof(OnAliveChanged))] public bool isAlive = true;
    [SyncVar] public bool isWin;
    // 음성 채팅 참가자 ID: 듣는 쪽이 "이 목소리가 어느 캐릭터인지" 찾는 데 쓴다(역할 정보 아님)
    [SyncVar] public string voiceId;

    // 이 클라이언트에 존재하는 모든 플레이어(근접 음성 판정용)
    public static readonly List<GamePlayer> AllPlayers = new List<GamePlayer>();

    // 다른 플레이어에 대해 내가 적어 둔 메모: 각 클라이언트 로컬 값(동기화하지 않음)
    [NonSerialized] public string memo;

    // 서버와 본인만 아는 값: PlayerPrivateState(Sync Mode = Owner)에 있고 여기서는 중계만 한다.
    // 다른 플레이어의 값은 클라이언트에서 항상 기본값이다
    private PlayerPrivateState _privateState;
    private PlayerPrivateState PrivateState => _privateState != null ? _privateState : (_privateState = GetComponent<PlayerPrivateState>());

    public int hungryStreak { get => PrivateState.hungryStreak; set => PrivateState.hungryStreak = value; }
    public bool hasEate { get => PrivateState.hasEate; set => PrivateState.hasEate = value; }
    public bool hasAttacked { get => PrivateState.hasAttacked; set => PrivateState.hasAttacked = value; }
    public int scanCount { get => PrivateState.scanCount; set => PrivateState.scanCount = value; }
    public AnimalType predictedWinner { get => PrivateState.predictedWinner; set => PrivateState.predictedWinner = value; }
    public uint predictedPlayerNetId { get => PrivateState.predictedPlayerNetId; set => PrivateState.predictedPlayerNetId = value; }
    public AnimalType disguisedAs { get => PrivateState.disguisedAs; set => PrivateState.disguisedAs = value; }
    public AnimalType predictedAs { get => PrivateState.predictedAs; set => PrivateState.predictedAs = value; }
    public ZoneType currentZone { get => PrivateState.currentZone; set => PrivateState.currentZone = value; }
    public bool predictedCorrectly { get => PrivateState.predictedCorrectly; set => PrivateState.predictedCorrectly = value; }
    public SyncList<MissionSlot> Missions => PrivateState.Missions;

    [SerializeField] private PlayerMove playerMove;
    [SerializeField] private VisionUIController visionUI;
    [SerializeField] private GameObject corpsePrefab;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Collider2D bodyCollider;

    [Header("Investigation")]
    [SerializeField] private float investigationDuration = 3f;
    [Tooltip("게임 한 판에 탐색 오브젝트로 탐색할 수 있는 횟수(조력자가 아닌 플레이어)")]
    [SerializeField] private int investigationCount = 1;
    [Tooltip("게임 한 판에 탐색 오브젝트로 탐색할 수 있는 횟수(조력자)")]
    [SerializeField] private int helperInvestigationCount = 2;
    public float InvestigationDuration => investigationDuration;
    // 역할(canScan)과 scanCount는 서버와 본인만 알므로 본인 UI 표시와 서버 판정에만 쓴다
    public int MaxInvestigationCount => IsHelper() ? helperInvestigationCount : investigationCount;
    public int RemainingInvestigationCount => Mathf.Max(0, MaxInvestigationCount - scanCount);
    private Coroutine _serverInvestigationCo;

    [Header("Corpse Scan")]
    [SerializeField] private float corpseScanDuration = 3f;
    [SerializeField] private float corpseScanRange = 2f;
    public float CorpseScanDuration => corpseScanDuration;
    public float CorpseScanRange => corpseScanRange;
    private Coroutine _serverCorpseScanCo;

    [Header("Attack")]
    [Tooltip("서버가 공격을 허용하는 최대 거리(플레이어 중심 간). Scanner의 scanRadius + 콜라이더 크기보다 크게 잡아 지연 오차를 허용")]
    [SerializeField] private float attackRange = 3f;

    [Header("Chat")]
    [SerializeField] private int maxChatLength = 200;

    public IPlayerAbility[] Abilities { get; private set; }
    public LocalVisionLight localVision;

    public BaseMission progressMission;
    public static GamePlayer LocalPlayer;
    // 미션 중 ESC를 막을 때 쓰는 ESC 목록 키
    private static readonly object MissionEscapeKey = new object();
    public Scanner scanner;
    public TextMeshProUGUI nicknameText;
    public GameObject killIndicatorUIPrefab;
    private GameObject killIndicatorUIInstance;

    private IMissionCompleteHandler[] _missionCompleteHandlers;
    private Coroutine _applyMissionsCo;

    // 서버: 시작을 승인한 미션(플레이어당 하나)과 시작 시각
    private bool _serverHasActiveMission;
    private MissionType _serverActiveMission;
    // 실제 시간(초). Time.time은 방장 PC가 심하게 느리면 한 프레임에 최대 0.33초만 늘어 실제보다 느리게 가므로,
    // 정상적으로 끝낸 미션이 최소 소요 시간 미달로 거부될 수 있었다
    private double _serverMissionStartTime;
    // 클라: 시작 요청을 보내고 서버 응답을 기다리는 중
    private bool _missionRequestPending;
    // 클라: 시작 연출을 이미 재생했는지
    private bool _introPlayed;
    public override void OnStartServer()
    {
        base.OnStartServer();
        Abilities = GetComponents<IPlayerAbility>();
        _missionCompleteHandlers = GetComponents<IMissionCompleteHandler>();
    }
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!AllPlayers.Contains(this)) AllPlayers.Add(this);
        ApplyCostume();

        Missions.Callback += OnMissionsChanged;
        Abilities = GetComponents<IPlayerAbility>();
        _missionCompleteHandlers = GetComponents<IMissionCompleteHandler>();
    }
    public override void OnStartLocalPlayer()
    {       
        base.OnStartLocalPlayer();

        LocalPlayer = this;
        // 구역 아이콘은 내 것만 놓는다(현재 구역은 본인에게만 동기화된다). 거주지가 아니라 현재 구역 기준
        if (GamePlayUI.Instance != null) GamePlayUI.Instance.AddPlayer(this, currentZone);
        _knownDeadNetIds.Clear();
        visionUI = FindFirstObjectByType<VisionUIController>();
        visionUI.localPlayer = this;

        if (visionUI) visionUI.enabled = true;

        // 음성: 게임 채널로 옮기고(생존/유령은 현재 상태 기준), 거리·시야에 따른 음량 조절을 켠다.
        // 죽은 뒤에도 "가까운 생존자의 목소리"를 판정해야 하므로 계속 켜 둔다
        VoiceManager.Instance?.EnterGameplay(RoomSessionData.CurrentRoomCode, isAlive);

        var prox = GetComponentInChildren<InGameVoiceProximity>(true);
        if (prox != null) prox.enabled = true;
        else Debug.LogWarning("[GamePlayer] InGameVoiceProximity가 없어 근접 음성이 적용되지 않습니다.");

        StartCoroutine(CoReportVoiceId());

        CmdRequestRole();
    }

    // 음성 로그인이 끝나면 내 음성 ID를 서버에 알린다(로비에서 이미 알렸으면 서버가 넘겨받은 값과 같아 생략)
    private IEnumerator CoReportVoiceId()
    {
        while (VoiceManager.Instance == null || string.IsNullOrEmpty(VoiceManager.Instance.LocalVoiceId))
            yield return null;

        string localVoiceId = VoiceManager.Instance.LocalVoiceId;
        if (voiceId != localVoiceId)
            CmdSetVoiceId(localVoiceId);
    }

    [Command]
    private void CmdSetVoiceId(string id)
    {
        if (!VoiceManager.IsValidVoiceId(id)) return;

        // 다른 플레이어의 ID를 사칭하지 못하게 한다
        foreach (var other in FindObjectsByType<GamePlayer>(FindObjectsSortMode.None))
        {
            if (other != this && other.voiceId == id) return;
        }

        voiceId = id;
    }

    [Command]
    public void CmdRequestRole()
    {
        TargetReceiveRole(characterName);
    }

    [TargetRpc]
    private void TargetReceiveRole(string assignedCharacter)
    {
        characterName = assignedCharacter;
        SetAnimalType(assignedCharacter);
        Debug.Log($"[GamePlayer] 내 캐릭터는 {characterName}");

        // 역할을 받기 전에 만들어진 내 슬롯은 "???"로 표시돼 있으므로 갱신
        if (PlayerSlotUI.Instance != null)
        {
            var slot = PlayerSlotUI.Instance.GetSlotByPlayer(this);
            if (slot != null && AnimalNameMap.AnimalTypeToName.TryGetValue(animalType, out string animalName))
                slot.UpdateNicknameWithAnimal(animalName);
        }

        StartCoroutine(CoReportGameReady());
    }

    // 게임 씬의 매니저/UI가 준비되면 내 UI를 초기화하고 서버에 준비 완료를 알린다.
    // 서버는 모든 플레이어의 준비가 확인된 뒤에 시작 연출과 게임을 진행한다(GameMamager.RoundFlow)
    private IEnumerator CoReportGameReady()
    {
        while (GameMamager.Instance == null || !GameMamager.Instance.isActiveAndEnabled || GamePlayUI.Instance == null)
            yield return null;

        GameMamager.Instance.ClientInitLocalUI(this);
        CmdReportGameReady();

        // 준비가 늦어 시작 연출 RPC를 놓쳤다면 지금 재생
        if (GameMamager.Instance.introStarted)
            PlayStartIntro();
    }

    [Command]
    public void CmdReportGameReady()
    {
        if (GameMamager.Instance != null)
            GameMamager.Instance.ServerMarkPlayerReady(this);
    }

    // 시작 연출(캐릭터 소개)은 한 번만 재생
    public void PlayStartIntro()
    {
        if (_introPlayed) return;
        _introPlayed = true;

        StartCoroutine(ShowCharacterUI());
    }

    // 내가 죽음을 알고 있는 플레이어(내가 죽였거나, 시체를 조사해 알아낸 경우). 음성 목록의 사망 표시에 쓴다
    private static readonly HashSet<uint> _knownDeadNetIds = new();
    private PlayerUIVisibility _uiVisibility;

    // 유령은 죽은 사람을 전부 알고, 생존자는 자신이 죽였거나 시체를 조사한 사람만 안다
    public static bool IsKnownDeadToLocal(GamePlayer player)
    {
        if (player == null || player.isAlive || LocalPlayer == null) return false;
        if (player == LocalPlayer || !LocalPlayer.isAlive) return true;

        return _knownDeadNetIds.Contains(player.netId);
    }

    // 지금 내 시야 안에 있는지(머리 위 닉네임이 보이는 상태와 같다)
    public bool IsInLocalVision
    {
        get
        {
            if (_uiVisibility == null) _uiVisibility = GetComponentInChildren<PlayerUIVisibility>(true);
            return _uiVisibility == null || _uiVisibility.IsVisible;
        }
    }

    [TargetRpc]
    private void TargetLearnDeath(uint deadNetId)
    {
        _knownDeadNetIds.Add(deadNetId);
    }

    [TargetRpc]
    public void TargetShowGameResult(bool isWinner)
    {
        if (GamePlayUI.Instance != null)
            GamePlayUI.Instance.ShowResult(isWinner);

        // 음성: 결과 화면부터는 생존자·유령 구분 없이 전원이 서로 들린다
        VoiceManager.Instance?.EnterResult(RoomSessionData.CurrentRoomCode);
    }
    public override void OnStopClient()
    {
        AllPlayers.Remove(this);

        // 게임 도중 나간 플레이어의 구역 아이콘이 남지 않도록 정리
        if (GamePlayUI.Instance != null)
            GamePlayUI.Instance.RemovePlayer(this);

        if (visionUI) visionUI.enabled = false;
        Missions.Callback -= OnMissionsChanged;
        base.OnStopClient();
    }
    [Server]
    public void ActivateAbilitie()
    {
        foreach (var ab in GetComponents<IAnimalAbility>())
            ab.ServerActivate(this, animalType);
    }
    void OnMissionsChanged(SyncList<MissionSlot>.Operation op, int index, MissionSlot oldItem, MissionSlot newItem)
    {
        if (!isLocalPlayer) return;

        if (_applyMissionsCo != null)
            StopCoroutine(_applyMissionsCo);

        _applyMissionsCo = StartCoroutine(CoApplyMissionsToLocalUI());
    }

    private IEnumerator CoApplyMissionsToLocalUI()
    {
        while (MissionListUI.Instance == null)
            yield return null;

        // 죽은 뒤에는 사망 후 전용 미션이 있는 캐릭터(하마)만 미션을 본다
        if (CanDoMissions)
            MissionListUI.Instance.RefreshList(Missions);
        else
            MissionListUI.Instance.RefreshList(System.Array.Empty<MissionSlot>());

        //if (Missions.Count < 5) yield break;

        // 완료한 미션의 오브젝트는 다시 켜지 않는다
        var types = new List<MissionType>(Missions.Count);
        for (int i = 0; i < Missions.Count; i++)
        {
            if (CanDoMissions && Missions[i].Status != MissionStatus.Completed)
                types.Add(Missions[i].Type);
        }

        yield return EnableMissionObjectsWhenReady(types);
    }
    IEnumerator ShowCharacterUI()
    {
        yield return new WaitForSeconds(0.1f);

        if (GamePlayUI.Instance == null || animalType == AnimalType.None) yield break;

        StartCoroutine(GamePlayUI.Instance.ShowCharacter(animalType));
    }


    [Server]
    private bool ServerTrySanitizeChat(ref string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;

        if (message.Length > maxChatLength)
            message = message.Substring(0, maxChatLength);

        // 말풍선·채팅에 태그(<size>, <color> 등)가 적용되지 않게 한다
        message = TextSanitizer.BlockRichText(message);

        return true;
    }

    [Command]
    public void CmdSendChatMessage(string message)
    {
        if (!ServerTrySanitizeChat(ref message)) return;

        bool senderIsGhost = !isAlive;

        foreach (var player in FindObjectsByType<GamePlayer>(FindObjectsSortMode.None))
        {
            bool receiverIsGhost = !player.isAlive;

            if (senderIsGhost && !receiverIsGhost)
                continue;

            player.TargetReceiveMessage(netId, nickname, message);
        }
        // 말풍선은 수신자 쪽(TargetReceiveMessage)에서만 띄운다. 서버에서 직접 띄우면 호스트가 살아있어도 유령의 말풍선을 보게 된다
    }

    [Command]
    public void CmdSendWhisper(uint targetNetId, string message)
    {
        if (!ServerTrySanitizeChat(ref message)) return;
        if (targetNetId == netId) return;
        if (!NetworkServer.spawned.TryGetValue(targetNetId, out var identity) || identity == null) return;

        var target = identity.GetComponent<GamePlayer>();
        if (target == null) return;

        // 산 사람과 유령 사이에는 귓속말이 오가지 않는다(유령은 유령끼리만 소통)
        if (isAlive != target.isAlive) return;

        // 인자: (귓속말 탭의 상대, 말한 사람). 보낸 쪽 탭의 상대는 받는 사람, 받는 쪽 탭의 상대는 보낸 사람이다.
        // 말한 사람은 양쪽 모두 나(보낸 사람)여야 한다(예전에는 받는 쪽에 받는 사람 자신의 이름이 표시됐다)
        TargetReceiveWhisper(target.netId, netId, message);
        target.TargetReceiveWhisper(netId, netId, message);
    }

    // 승리 예측(올빼미). 역할은 서버와 본인만 알므로 본인 UI와 서버 판정에만 쓴다
    public bool CanPredict => canPredict;
    // 예측과 위장은 한 번 정하면 바꿀 수 없다
    public bool HasPredicted => predictedPlayerNetId != 0;
    public bool HasDisguised => disguisedAs != AnimalType.None;

    // 예측 오브젝트에서 상호작용했을 때 예측 창(플레이어 목록)을 연다
    public void TryStartPredict()
    {
        if (!isAlive || !CanPredict) return;
        if (GamePlayUI.Instance == null) return;

        if (playerMove != null) playerMove.StopMove();

        GamePlayUI.Instance.ShowPredictUI();
    }

    [Command]
    public void CmdSetPredict(uint targetNetId)
    {
        if (!isAlive || !CanPredict) return;
        if (HasPredicted) return;
        if (targetNetId == netId) return;
        // 예측은 예측 오브젝트 근처에서만 할 수 있다
        if (!PredictObject.IsAnyInRange(transform.position)) return;
        if (!NetworkServer.spawned.TryGetValue(targetNetId, out var identity) || identity == null) return;

        GamePlayer predicted = identity.GetComponent<GamePlayer>();
        if (predicted == null) return;

        // 예측 기록
        predictedPlayerNetId = predicted.netId;
        predictedWinner = predicted.animalType;

        // 클라이언트에 예측 UI 갱신 요청
        TargetShowPredictionUI(connectionToClient, predicted.netId);

        Debug.Log($"{nickname}님이 {predicted.nickname}을(를) 승리자로 예측했습니다.");
    }
    [Command]
    public void CmdScanCorpse(uint corpseNetId)
    {
        ServerStopCorpseScan();

        if (!NetworkServer.spawned.TryGetValue(corpseNetId, out var identity) || identity == null) return;

        var corpse = identity.GetComponent<Corpse>();
        if (!ServerCanScanCorpse(corpse)) return;

        TargetStartCorpseScan(corpseNetId);
        _serverCorpseScanCo = StartCoroutine(ServerCoScanCorpse(corpse));
    }

    [Command]
    public void CmdCancelCorpseScan()
    {
        ServerStopCorpseScan();
    }

    [Server]
    private void ServerStopCorpseScan()
    {
        if (_serverCorpseScanCo == null) return;

        StopCoroutine(_serverCorpseScanCo);
        _serverCorpseScanCo = null;
    }

    [Server]
    private bool ServerCanScanCorpse(Corpse corpse)
    {
        if (corpse == null || !isAlive || !canScan) return false;

        Vector2 toCorpse = corpse.transform.position - transform.position;
        return toCorpse.sqrMagnitude <= corpseScanRange * corpseScanRange;
    }

    // 조사 시간과 조건(생존/조력자/거리)은 서버가 재고, 끝까지 유지됐을 때만 사망 정보를 본인에게 보낸다
    private IEnumerator ServerCoScanCorpse(Corpse corpse)
    {
        float elapsed = 0f;
        while (elapsed < corpseScanDuration)
        {
            if (!ServerCanScanCorpse(corpse))
            {
                _serverCorpseScanCo = null;
                TargetCorpseScanCanceled();
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        _serverCorpseScanCo = null;

        if (!ServerCanScanCorpse(corpse))
        {
            TargetCorpseScanCanceled();
            yield break;
        }

        // 시체를 조사한 사람은 그 플레이어가 죽었다는 것을 안다(음성 목록의 사망 표시)
        TargetLearnDeath(corpse.deadPlayerNetId);
        TargetCorpseScanResult(corpse.netId, corpse.deadAnimalType, corpse.KillerType, corpse.deathReason, corpse.deathDay, corpse.deathWasNight, corpse.ElapsedSec);
    }

    [TargetRpc]
    private void TargetStartCorpseScan(uint corpseNetId)
    {
        if (GamePlayUI.Instance == null) return;

        Corpse corpse = null;
        if (NetworkClient.spawned.TryGetValue(corpseNetId, out var identity) && identity != null)
            corpse = identity.GetComponent<Corpse>();

        if (corpse == null)
        {
            CmdCancelCorpseScan();
            return;
        }

        GamePlayUI.Instance.BeginCorpseScan(corpse);
    }

    [TargetRpc]
    private void TargetCorpseScanResult(uint corpseNetId, AnimalType deadType, AnimalType killerType, DeathReason deathReason, int day, bool wasNight, float elapsedSec)
    {
        if (GamePlayUI.Instance == null) return;
        GamePlayUI.Instance.OnCorpseScanResult(corpseNetId, deadType, killerType, deathReason, day, wasNight, elapsedSec);
    }

    [TargetRpc]
    private void TargetCorpseScanCanceled()
    {
        if (GamePlayUI.Instance == null) return;
        GamePlayUI.Instance.OnCorpseScanCanceled();
    }

    public void TryScanCorpse(Corpse corpse)
    {
        if (corpse == null || !isAlive || !IsHelper()) return;

        // 이미 조사한 시체는 조사 목록에서 다시 볼 수 있으므로 재조사하지 않는다
        if (InvestigationLog.Instance != null && InvestigationLog.Instance.TryGet($"{corpse.netId}", out _)) return;

        CmdScanCorpse(corpse.netId);
    }
    void OnAliveChanged(bool oldVal, bool newVal)
    {
        if (newVal) return;
            
        if (isLocalPlayer)
        {
            // 진행 중이던 미션과 서버 응답을 기다리던 미션 시작 요청을 취소한다
            FailProgressMission();
            CancelPendingMissionRequest();

            // 탐색·부검 등 준비·진행 중이던 행동을 취소하고 열린 창을 모두 닫은 뒤 사망 연출을 보여 준다
            if (GamePlayUI.Instance != null)
                GamePlayUI.Instance.BeginLocalDeath();

            // 음성: 유령 채널로 옮긴다(유령끼리 대화, 생존자 채널은 듣기만).
            // 게임이 끝나 전원 대화로 넘어간 뒤의 사망 처리라면 그대로 둔다
            var voice = VoiceManager.Instance;
            if (voice != null && voice.DesiredContext == VoiceManager.VoiceContext.GameplayAlive)
                voice.EnterGameplay(RoomSessionData.CurrentRoomCode, isAlive: false);
        }
        else if (VoiceManager.Instance != null)
        {
            // 음성: 사망이 확정된 순간 이 플레이어의 목소리를 내 쪽에서도 바로 끈다
            VoiceManager.Instance.SilenceInAliveChannelNow(voiceId);
        }

        if (isLocalPlayer)
        {
            // 유령이 되면 살아있을 때의 기믹(미션 오브젝트 등)을 끄고, 사망 후 전용 기믹만 남긴다
            if (_applyMissionsCo != null)
                StopCoroutine(_applyMissionsCo);

            _applyMissionsCo = StartCoroutine(CoApplyMissionsToLocalUI());
        }

        if (GamePlayUI.Instance != null)
            GamePlayUI.Instance.RemovePlayer(this);

        AllRefreshVisibility();
    }

    // PlayerPrivateState의 currentZone 훅에서 부른다. 호스트에서는 다른 플레이어 값도 바뀌므로 내 것만 처리한다
    public void HandleZoneChanged(ZoneType newVal)
    {
        if (!isLocalPlayer || GamePlayUI.Instance == null) return;

        GamePlayUI.Instance.MovePlayerIcon(this, newVal);
    }

    [Server]
    private void SpawnCorpse()
    {
        if (corpsePrefab == null)        
            return;
        
        var corpseObj = Instantiate(corpsePrefab, transform.position, Quaternion.identity);
        var corpse = corpseObj.GetComponent<Corpse>();
        var gameMamager = GameMamager.Instance;
        if (corpse != null && gameMamager != null)
            corpse.Init(netId, lastKillerType, animalType, lastDeathReason, NetworkTime.time, gameMamager.currentRound, gameMamager.IsNightPhase, gameMamager.PhaseElapsedSeconds);
        
        NetworkServer.Spawn(corpseObj);
    }
    [ClientRpc]
    private void RpcApplyGhostMode()
    {
        if (bodyRenderer) bodyRenderer.color = new Color32(255,255,255,150);

        int layer = LayerMask.NameToLayer("Ghost");
        gameObject.layer = layer;

        if (isLocalPlayer)
        {
            if (localVision != null)            
                localVision.TransitionToVision(11, false);

            if (visionUI != null && visionUI.fov != null)
            {
                visionUI.fov.viewRadius = 11;
                visionUI.fov.ignoreOccluders = true;
            }                                
        }
    }
    public static void AllRefreshVisibility()
    {
        if (LocalPlayer == null) return;

        var players = FindObjectsByType<GamePlayer>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            p.ApplyVisibility(LocalPlayer);
        }
    }
    public void ApplyVisibility(GamePlayer viewer)
    {
        bool viewerIsGhost = !viewer.isAlive;
        bool thisIsGhost = !isAlive;

        bool visible;
        if (viewerIsGhost)
            visible = true;
        else
            visible = !thisIsGhost;

        if (bodyRenderer != null)
            bodyRenderer.enabled = visible;
        if (nicknameText != null)
            nicknameText.enabled = visible;
    }
    [TargetRpc]
    public void TargetReceiveWhisper(uint targetNetId, uint senderNetId, string message)
    {
            var chatManager = FindFirstObjectByType<ChatManager>();
            if (chatManager != null)
                chatManager.AddWhisperMessage(senderNetId, targetNetId, message);
    }
    [TargetRpc]
    public void TargetReceiveMessage(uint senderNetId, string sender, string message)
    {
        var chatManager = FindFirstObjectByType<ChatManager>();
        if (chatManager != null)
            chatManager.AddSystemMessage(sender, message);

        if (sender == "System")
            return;

        if (NetworkClient.spawned.TryGetValue(senderNetId, out var identity))
        {
            var senderPlayer = identity.GetComponent<GamePlayer>();
            if (senderPlayer != null)
            {
                var bubble = senderPlayer.GetComponentInChildren<SpeechBubble>();
                if (bubble != null)
                    bubble.Show(message);
            }
        }
    }
    [TargetRpc]
    public void TargetShowPredictionUI(NetworkConnection target, uint predictedId)
    {
        // 플레이어 목록에서 예측한 사람에게 표시(대상이 이미 나갔으면 건너뜀)
        if (PlayerSlotUI.Instance == null) return;
        if (!NetworkClient.spawned.TryGetValue(predictedId, out var identity) || identity == null) return;
        if (!identity.TryGetComponent(out GamePlayer predicted)) return;

        var slot = PlayerSlotUI.Instance.GetSlotByPlayer(predicted);
        if (slot != null) slot.MarkPrediction(predictedId);
    }
    
    [Command]
    public void CmdAttack(uint targetNetId)
    {
        if (!isAlive || hasAttacked) return;
        if (!isPredator) return;
        if (GameMamager.Instance == null || !GameMamager.Instance.isRoundActive) return;
        if (targetNetId == 0 || targetNetId == netId) return;

        if (!NetworkServer.spawned.TryGetValue(targetNetId, out var identity) || identity == null) return;

        var target = identity.GetComponent<GamePlayer>();
        if (target == null || !target.isAlive) return;

        // 사거리는 클라이언트 Scanner가 아니라 서버 위치 기준으로 판정
        Vector2 toTarget = target.transform.position - transform.position;
        if (toTarget.sqrMagnitude > attackRange * attackRange) return;

        Debug.Log($"[CmdAttack] {nickname}이 {target.nickname} 공격");

        // 기본 규칙: 피식자(독 없는 전갈 포함)는 사망, 포식자끼리는 서열이 더 높은 쪽만 공격 성공.
        // 서열이 낮은 포식자가 높은 포식자를 공격하면 공격자가 사망한다(같은 서열이면 실패만).
        // 능력은 이 기본 결과에 예외만 덮어쓴다(예: 전갈의 독 반격).
        bool attackerIsLowerRank = target.isPredator
            && PredatorPriority.GetPriority(predatorType) < PredatorPriority.GetPriority(target.predatorType);

        var ctx = new AttackContext
        {
            attacker = this,
            target = target,
            cancelAttack = false,
            killAttacker = attackerIsLowerRank,
            killTarget = !target.isPredator || PredatorPriority.CanAttack(predatorType, target.predatorType),
            reason = null
        };

        ServerRunAttackAbilities(Abilities, ref ctx);
        ServerRunAttackAbilities(target.Abilities, ref ctx);

        // 성공/실패와 무관하게 공격 기회는 소모
        hasAttacked = true;

        // 역공: 공격자만 사망
        if (ctx.killAttacker)
        {
            Die(new DeathInfo
            {
                Reason = DeathReason.CounterKill,
                KillerNetId = target.netId,
                KillerType = target.animalType
            });
            return;
        }

        if (ctx.cancelAttack || !ctx.killTarget) return;

        target.Die(new DeathInfo
        {
            Reason = DeathReason.KilledByAttack,
            KillerNetId = netId,
            KillerType = animalType
        });
        hasEate = true;
    }

    [Server]
    private static void ServerRunAttackAbilities(IPlayerAbility[] abilities, ref AttackContext ctx)
    {
        if (abilities == null) return;

        for (int i = 0; i < abilities.Length; i++)
        {
            try { abilities[i].OnBeforeAttack(ref ctx); }
            catch (Exception e) { Debug.LogError($"Ability error: {abilities[i].GetType().Name}\n{e}"); }
        }
    }
    [Server]
    public void Die(in DeathInfo info)
    {
        if (!isAlive) return;

        ServerReleaseActiveMission();

        // 진행 중이던 탐색/시체 조사는 죽는 즉시 중단한다(결과도 실패 안내도 보내지 않는다. 창은 본인 클라이언트가 닫는다)
        ServerStopInvestigation();
        ServerStopCorpseScan();

        lastDeathReason = info.Reason;
        lastKillerNetId = info.KillerNetId;
        lastKillerType = info.KillerType;

        if(animalType == AnimalType.Ostrich || animalType == AnimalType.Zebra)
        {
            var sym = GetComponent<PreySymbiosisAbility>();
            if (sym != null)
                sym.ServerClearPartnerBothSides();
        }        
        else if(animalType == AnimalType.Hippo)
        {
            var bga = GetComponent<HippoAbility>();
            if (bga != null)
                bga.ServerOnlocalDied(info.KillerNetId);
        }       

        isAlive = false;

        // 죽인 쪽은 이 플레이어가 죽었다는 것을 안다(음성 목록의 사망 표시)
        if (info.KillerNetId != 0
            && NetworkServer.spawned.TryGetValue(info.KillerNetId, out var killerIdentity)
            && killerIdentity != null
            && killerIdentity.TryGetComponent(out GamePlayer killer)
            && killer.connectionToClient != null)
        {
            killer.TargetLearnDeath(netId);
        }

        // 서버 처리는 SyncVar 훅(OnAliveChanged)이 아니라 여기서 직접 한다.
        // 훅은 호스트 모드에서만 서버 쪽에서 불리므로 훅에 두면 전용 서버에서 동작하지 않는다
        SpawnCorpse();
        RpcApplyGhostMode();

        if (GameMamager.Instance != null)
            GameMamager.Instance.ServerOnPlayerDied(this);
    }
    // 위장은 카멜레온만 가능.
    // 역할은 서버와 본인만 알므로 본인 UI와 서버 판정에만 쓴다
    public bool CanDisguise => animalType == AnimalType.Chameleon;

    // 위장 오브젝트에서 상호작용했을 때 위장 창(모든 동물 목록)을 연다
    public void TryStartDisguise()
    {
        if (!isAlive || !CanDisguise) return;
        if (GamePlayUI.Instance == null) return;

        if (playerMove != null) playerMove.StopMove();

        GamePlayUI.Instance.ShowDisguiseUI();
    }

    [Command]
    public void CmdSetDisguise(AnimalType selectedType)
    {
        if (!isAlive) return;
        if (!CanDisguise) return;
        if (HasDisguised) return;
        if (selectedType == AnimalType.None || !AnimalNameMap.AnimalTypeToName.ContainsKey(selectedType)) return;
        // 위장은 위장 오브젝트 근처에서만 할 수 있다
        if (!DisguiseObject.IsAnyInRange(transform.position)) return;

        disguisedAs = selectedType;
    }
    [Command]
    public void CmdChangeZone(ZoneType newZone)
    {
        if (!Enum.IsDefined(typeof(ZoneType), newZone)) return;

        currentZone = newZone;
    }
    public void SetKillUI(bool active)
    {
        if (killIndicatorUIInstance == null)
        {
            killIndicatorUIInstance = Instantiate(killIndicatorUIPrefab, transform);
            killIndicatorUIInstance.transform.localPosition = new Vector3(0, 1.5f, 0);
        }

        killIndicatorUIInstance.SetActive(active);       
    }

    void OnNicknameChanged(string oldNick, string newNick)
    {
        nicknameText.text = newNick;
    }
    void OnCostumeChanged(int oldIndex, int newIndex)
    {
        ApplyCostume();
    }

    private void ApplyCostume()
    {
        var view = GetComponent<PlayerCostumeView>();
        if (view != null) view.SetCostume(costumeIndex);
    }

    void OnColorChanged(Color oldColor, Color newColor)
    {
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.color = newColor;
    }
    private IEnumerator EnableMissionObjectsWhenReady(IReadOnlyCollection<MissionType> missionTypes)
    {
        int guard = 0;
        while (MissionRegistry.Instance == null && guard++ < 300)
            yield return null;

        if (MissionRegistry.Instance == null)
            yield break;

        guard = 0;
        while (MissionRegistry.Instance.RegisteredCount == 0 && guard++ < 300)
            yield return null;

        MissionRegistry.Instance.EnableOnly(missionTypes);
        Debug.Log($"[GamePlayer] 미션 설정 완료");
    }
    public bool CanStartMissionType(MissionType type)
    {
        int idx = FindMissionIndex(type);
        if (idx < 0) return false;
        return Missions[idx].Status != MissionStatus.Completed;
    }   
    private int FindMissionIndex(MissionType type)
    {
        for (int i = 0; i < Missions.Count; i++)
            if (Missions[i].Type == type)
                return i;
        return -1;
    }

    [Server]
    private void SetMissionStatusServer(MissionType type, MissionStatus status)
    {
        int idx = FindMissionIndex(type);
        if (idx < 0) return;

        var slot = Missions[idx];

        if (slot.Status == MissionStatus.Completed) return;

        slot.Status = status;

        Missions[idx] = slot;
    }

    // 죽은 뒤에도 미션(복수 미션)을 하는 건 하마뿐
    public bool CanDoMissions => isAlive || animalType == AnimalType.Hippo;

    // ===== 미션 흐름 =====
    // [클라] TryStartMission → CmdStartMission
    // [서버] 검증(배정/완료/생존/거리/사용 중) → 잠금 + TargetMissionStartAllowed (거부 시 TargetMissionStartDenied)
    // [클라] 미니게임 UI 진행(로컬) → 클리어 시 CmdCompleteMission, 닫으면 CmdCancelMission
    // [서버] 재검증(시작 승인 여부/최소 소요 시간/거리/중복) → Missions(SyncList) 갱신

    [Server]
    private MissionDenyReason ServerValidateMissionStart(MissionType type)
    {
        int idx = FindMissionIndex(type);
        if (idx < 0) return MissionDenyReason.NotAssigned;
        if (Missions[idx].Status == MissionStatus.Completed) return MissionDenyReason.AlreadyCompleted;
        if (!CanDoMissions) return MissionDenyReason.CannotDoMissions;

        var registry = MissionRegistry.Instance;
        if (registry == null || !registry.IsAnyInRange(type, transform.position)) return MissionDenyReason.OutOfRange;
        if (registry.IsLockedByOther(type, netId)) return MissionDenyReason.InUse;

        return MissionDenyReason.None;
    }

    [Command]
    public void CmdStartMission(MissionType type)
    {
        // 이전에 진행 중이던 미션이 남아 있으면 정리하고 새로 판정
        ServerReleaseActiveMission();

        var reason = ServerValidateMissionStart(type);
        if (reason != MissionDenyReason.None)
        {
            TargetMissionStartDenied(type, reason);
            return;
        }

        MissionRegistry.Instance.Lock(type, netId);
        _serverHasActiveMission = true;
        _serverActiveMission = type;
        _serverMissionStartTime = Time.realtimeSinceStartupAsDouble;

        SetMissionStatusServer(type, MissionStatus.InProgress);
        TargetMissionStartAllowed(type);
    }

    [Server]
    private bool ServerCanCompleteMission(MissionType type)
    {
        // 서버가 시작을 승인한 미션이어야 한다
        if (!_serverHasActiveMission || _serverActiveMission != type) return false;

        int idx = FindMissionIndex(type);
        if (idx < 0 || Missions[idx].Status != MissionStatus.InProgress) return false;
        if (!CanDoMissions) return false;

        var registry = MissionRegistry.Instance;
        if (registry == null || !registry.IsAnyInRange(type, transform.position)) return false;

        return Time.realtimeSinceStartupAsDouble - _serverMissionStartTime >= registry.GetMinDuration(type);
    }

    [Command]
    public void CmdCompleteMission(MissionType type)
    {
        if (!ServerCanCompleteMission(type))
        {
            ServerReleaseActiveMission();
            TargetMissionCompleteRejected(type);
            return;
        }

        _serverHasActiveMission = false;
        MissionRegistry.Instance.Unlock(type, netId);
        SetMissionStatusServer(type, MissionStatus.Completed);

        if (_missionCompleteHandlers == null) return;

        for (int i = 0; i < _missionCompleteHandlers.Length; i++)
        {
            try { _missionCompleteHandlers[i].OnMissionCompletedServer(this, type); }
            catch (Exception e) { Debug.LogError(e); }
        }
    }

    [Command]
    public void CmdCancelMission(MissionType type)
    {
        if (_serverHasActiveMission && _serverActiveMission == type)
            ServerReleaseActiveMission();
    }

    // 진행 중이던 미션의 잠금을 풀고 상태를 되돌린다(취소/거부/사망/접속 종료 공통)
    [Server]
    private void ServerReleaseActiveMission()
    {
        if (!_serverHasActiveMission) return;
        _serverHasActiveMission = false;

        MissionType type = _serverActiveMission;

        if (MissionRegistry.Instance != null)
            MissionRegistry.Instance.Unlock(type, netId);

        int idx = FindMissionIndex(type);
        if (idx >= 0 && Missions[idx].Status == MissionStatus.InProgress)
            SetMissionStatusServer(type, MissionStatus.NotStarted);
    }

    public override void OnStopServer()
    {
        ServerReleaseActiveMission();
        base.OnStopServer();
    }

    public void TryStartMission(MissionType missionType)
    {
        if (_missionRequestPending || progressMission != null) return;
        if (!CanDoMissions) return;

        int idx = FindMissionIndex(missionType);
        if (idx < 0)
        {
            Debug.Log($"[GamePlayer] 이 플레이어에게 없는 미션: {missionType}");
            return;
        }

        var slot = Missions[idx];
        if (slot.Status == MissionStatus.Completed)
        {
            Debug.Log($"[GamePlayer] 이미 완료한 미션: {missionType}");
            return;
        }

        // UI를 띄울 수 없는 미션은 요청 자체를 하지 않는다(잠금만 걸리고 풀리지 않는 상황 방지)
        if (UIManager.Instance == null || MissionUIManager.Instance == null || !MissionUIManager.Instance.HasMission(missionType))
        {
            Debug.LogError($"[GamePlayer] 미션 UI를 열 수 없음: {missionType}");
            return;
        }

        // 서버 응답(TargetMissionStartAllowed/Denied)이 올 때까지 이동/상호작용을 잠그고 대기
        _missionRequestPending = true;
        UIManager.Instance.Push(UIPriority.Modal);
        // 미션 중(응답 대기 포함)에는 ESC를 막는다. 미션 창은 미션의 닫기 버튼으로만 나간다
        UIManager.RegisterEscape(MissionEscapeKey, null);
        if (playerMove != null) playerMove.StopMove();

        CmdStartMission(missionType);
    }

    // 미션 시작을 요청하고 서버 응답을 기다리던 중이면 잠금을 풀고 요청을 버린다.
    // 나중에 허용 응답이 와도 TargetMissionStartAllowed가 대기 중이 아님을 보고 서버에 취소를 보낸다
    private void CancelPendingMissionRequest()
    {
        if (!_missionRequestPending) return;
        _missionRequestPending = false;

        UIManager.Instance.Pop(UIPriority.Modal);
        UIManager.UnregisterEscape(MissionEscapeKey);
    }

    [TargetRpc]
    private void TargetMissionStartAllowed(MissionType type)
    {
        if (!_missionRequestPending)
        {
            CmdCancelMission(type);
            return;
        }
        _missionRequestPending = false;

        // 응답을 기다리는 사이 죽었거나 UI를 열 수 없으면 서버 쪽 진행 상태를 되돌린다
        bool opened = CanDoMissions
            && MissionUIManager.Instance != null
            && MissionUIManager.Instance.StartMission(type, completed => OnLocalMissionFinished(type, completed), this);

        if (!opened)
        {
            UIManager.Instance.Pop(UIPriority.Modal);
            UIManager.UnregisterEscape(MissionEscapeKey);
            CmdCancelMission(type);
        }
    }

    [TargetRpc]
    private void TargetMissionStartDenied(MissionType type, MissionDenyReason reason)
    {
        if (!_missionRequestPending) return;
        _missionRequestPending = false;

        UIManager.Instance.Pop(UIPriority.Modal);
        UIManager.UnregisterEscape(MissionEscapeKey);

        string message = reason switch
        {
            MissionDenyReason.InUse => "다른 플레이어가 사용 중인 미션입니다.",
            MissionDenyReason.OutOfRange => "미션 장소에서 너무 멉니다.",
            _ => "지금은 이 미션을 시작할 수 없습니다."
        };
        Debug.Log($"[GamePlayer] 미션 시작 거부: {type} ({reason})");

        var chatManager = FindFirstObjectByType<ChatManager>();
        if (chatManager != null)
            chatManager.AddLocalNotice(message);
    }

    // 미니게임 UI가 끝났을 때(클리어/닫기/실패) 한 번만 호출된다. Modal Pop도 여기서 한 번만 한다
    private void OnLocalMissionFinished(MissionType type, bool completed)
    {
        UIManager.Instance.Pop(UIPriority.Modal);
        UIManager.UnregisterEscape(MissionEscapeKey);

        if (!completed)
        {
            CmdCancelMission(type);
            return;
        }

        CmdCompleteMission(type);

        if (MissionRegistry.Instance != null)
            MissionRegistry.Instance.DisableType(type);
    }

    // 완료가 거부되면 클라이언트가 꺼버린 미션 오브젝트를 다시 켜서 재시도할 수 있게 한다
    [TargetRpc]
    private void TargetMissionCompleteRejected(MissionType type)
    {
        Debug.LogWarning($"[GamePlayer] 서버가 미션 완료를 거부함: {type}");

        if (_applyMissionsCo != null)
            StopCoroutine(_applyMissionsCo);

        _applyMissionsCo = StartCoroutine(CoApplyMissionsToLocalUI());
    }
    public void FailProgressMission()
    {
        if (progressMission == null) return;

        progressMission.Fail();
        progressMission = null;
    }

    // 탐색 오브젝트는 모든 플레이어가 쓸 수 있다(횟수만 역할에 따라 다르다)
    public void TryStartInvestigation()
    {
        if (!isAlive) return;
        if (GamePlayUI.Instance == null) return;

        if (playerMove != null) playerMove.StopMove();

        GamePlayUI.Instance.Startinvestigation();
    }

    [Command]
    public void CmdBeginInvestigation(uint targetNetId)
    {
        ServerStopInvestigation();

        if (scanCount >= MaxInvestigationCount
            || !ServerCanInvestigate() || !ServerTryGetInvestigationTarget(targetNetId, out _))
        {
            TargetInvestigationCanceled();
            return;
        }

        _serverInvestigationCo = StartCoroutine(ServerCoInvestigate(targetNetId));
    }

    [Command]
    public void CmdCancelInvestigation()
    {
        ServerStopInvestigation();
    }

    [Server]
    private void ServerStopInvestigation()
    {
        if (_serverInvestigationCo == null) return;

        StopCoroutine(_serverInvestigationCo);
        _serverInvestigationCo = null;
    }

    [Server]
    private bool ServerCanInvestigate()
    {
        return isAlive && InvestigationObject.IsAnyInRange(transform.position);
    }

    private bool ServerTryGetInvestigationTarget(uint targetNetId, out GamePlayer target)
    {
        target = null;
        if (targetNetId == netId) return false;
        if (!NetworkServer.spawned.TryGetValue(targetNetId, out var identity) || identity == null) return false;

        target = identity.GetComponent<GamePlayer>();
        return target != null;
    }

    // 탐색 시간과 조건(생존/거리/횟수)은 서버가 직접 재고, 끝까지 유지됐을 때만 결과를 보낸다.
    // 횟수는 결과를 보낼 때만 줄어든다(중간에 실패하면 차감하지 않는다)
    private IEnumerator ServerCoInvestigate(uint targetNetId)
    {
        float elapsed = 0f;
        while (elapsed < investigationDuration)
        {
            if (!ServerCanInvestigate())
            {
                _serverInvestigationCo = null;
                TargetInvestigationCanceled();
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        _serverInvestigationCo = null;

        if (!ServerCanInvestigate() || !ServerTryGetInvestigationTarget(targetNetId, out var target))
        {
            TargetInvestigationCanceled();
            yield break;
        }

        if (scanCount >= MaxInvestigationCount)
        {
            TargetInvestigationCanceled();
            yield break;
        }

        // 위장 중인 카멜레온은 위장한 동물로 보인다(위장 값이 비정상이면 실제 동물로 대체)
        AnimalType shownType = target.animalType;
        bool shownIsPredator = target.isPredator;
        if (target.disguisedAs != AnimalType.None && AnimalNameMap.AnimalTypeToName.ContainsKey(target.disguisedAs))
        {
            shownType = target.disguisedAs;
            shownIsPredator = IsPredatorAnimal(shownType);
        }

        scanCount++;
        TargetInvestigationResult(targetNetId, target.nickname, shownType, shownIsPredator);
    }

    private static bool IsPredatorAnimal(AnimalType type)
    {
        return type == AnimalType.Wolf || type == AnimalType.Crocodile
            || type == AnimalType.Hawk || type == AnimalType.Hyena;
    }

    [TargetRpc]
    private void TargetInvestigationResult(uint targetNetId, string targetNickname, AnimalType targetType, bool targetIsPredator)
    {
        // 플레이어 목록의 닉네임 옆에 탐색으로 알아낸 동물을 표시(대상이 이미 나갔으면 건너뜀)
        if (PlayerSlotUI.Instance != null
            && AnimalNameMap.AnimalTypeToName.TryGetValue(targetType, out string shownName)
            && NetworkClient.spawned.TryGetValue(targetNetId, out var identity) && identity != null
            && identity.TryGetComponent(out GamePlayer investigated))
        {
            var slot = PlayerSlotUI.Instance.GetSlotByPlayer(investigated);
            if (slot != null) slot.UpdateNicknameWithAnimal(shownName);
        }

        if (GamePlayUI.Instance == null) return;
        GamePlayUI.Instance.ActiveInvestigationResult(targetNetId, targetNickname, targetType, targetIsPredator);
    }

    [TargetRpc]
    private void TargetInvestigationCanceled()
    {
        if (GamePlayUI.Instance == null) return;
        GamePlayUI.Instance.OnInvestigationCanceled();
    }
    public bool IsHelper()
    {
        return canScan;
    }
    public void SetAnimalType(string characterName)
    {
        switch (characterName)
        {
            case "Wolf":
                animalType = AnimalType.Wolf;
                predatorType = PredatorType.Wolf;
                isPredator = true;
                isFly = false;
                canPredict = false;
                canScan = false;
                break;
            case "Crocodile":
                animalType = AnimalType.Crocodile;
                predatorType = PredatorType.Crocodile;
                isPredator = true;
                isFly = false;
                canPredict = false;
                canScan = false;
                break;
            case "Hawk":
                animalType = AnimalType.Hawk;
                predatorType = PredatorType.Hawk;
                isPredator = true;
                isFly = true;
                canPredict = false;
                canScan = false;
                break;
            case "Hyena":
                animalType = AnimalType.Hyena;
                predatorType = PredatorType.Hyena;
                isPredator = true;
                isFly = false;
                canPredict = false;
                canScan = false;
                break;
            case "Scorpion":
                animalType = AnimalType.Scorpion;
                predatorType = PredatorType.Scorpion;
                isPredator = false;
                isFly = false;
                canPredict = false;
                canScan = false;
                break;
            case "Zebra":
                animalType = AnimalType.Zebra;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = false;
                canPredict = false;
                canScan = false;
                break;
            case "Hippo":
                animalType = AnimalType.Hippo;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = false;
                canPredict = false;
                canScan = false;
                break;
            case "Meerkat":
                animalType = AnimalType.Meerkat;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = false;
                canPredict = false;
                canScan = false;
                break;
            case "Chameleon":
                animalType = AnimalType.Chameleon;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = false;
                canPredict = false;
                canScan = false;
                break;
            case "Ostrich":
                animalType = AnimalType.Ostrich;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = true;
                canPredict = false;
                canScan = false;
                break;
            case "Plover":
                animalType = AnimalType.Plover;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = true;
                canPredict = false;
                canScan = true;
                break;
            case "Owl":
                animalType = AnimalType.Owl;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = false;
                // 올빼미의 능력: 승리 예측(예측 오브젝트에서 한 번). 고른 플레이어가 이기면 함께 승리(OwlWinCondition)
                canPredict = true;
                canScan = false;
                break;
            case "Crow":
                animalType = AnimalType.Crow;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = true;
                // 까마귀는 늑대의 조력자: 조력자 능력(시체 조사, 탐색 2회)만 갖고, 늑대가 살아 있으면 승리(CrowWinCondition)
                canPredict = false;
                canScan = true;
                break;
            default:
                animalType = AnimalType.None;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = false;
                canPredict = false;
                canScan = false;
                break;
        }

        IsRoleReady = true;
    }
}
