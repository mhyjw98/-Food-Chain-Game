using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public enum AnimalType { Wolf, Crocodile, Hawk, Hyena, Scorpion, Crow, Plover, Squirrel, Badger, Ostrich, Zebra, Skunk, Fox, None }
public enum PredatorType { Prey, Hyena, Hawk, Crocodile, Wolf, Scorpion }
public enum DeathReason { None = 0, Hunger, MissionFail, KilledByAttack, CounterKill }
public struct DeathInfo { public DeathReason Reason; public uint KillerNetId; public AnimalType KillerType; }
public class GamePlayer : NetworkBehaviour
{
    [SyncVar] public string characterName;
    [SyncVar(hook = nameof(OnNicknameChanged))] public string nickname;
    [SyncVar] public ZoneType homeZone;
    [SyncVar(hook = nameof(OnColorChanged))] public Color gpColor = Color.white;
    [SyncVar(hook = nameof(OnZoneChanged))] public ZoneType currentZone;
    [SyncVar] public bool isReturn;
    [SyncVar] public int hungryStreak = 0;
    [SyncVar] public bool hasEate = false;
    [SyncVar] public bool hasAttacked = false;
    [SyncVar(hook = nameof(OnAliveChanged))] public bool isAlive = true;
    [SyncVar] public AnimalType animalType;
    [SyncVar] public PredatorType predatorType;
    [SyncVar] public AnimalType predictedWinner = AnimalType.None;
    [SyncVar] public bool isPredator;
    [SyncVar] public bool isFly;
    [SyncVar] public bool canScan;
    [SyncVar] public bool canPredict;
    [SyncVar] public bool isDisguise;
    [SyncVar] public AnimalType disguisedAs = AnimalType.None;
    [SyncVar] public AnimalType predictedAs = AnimalType.None;
    [SyncVar] public bool predictedCorrectly;
    [SyncVar] public bool isWin;
    [SyncVar] public string memo;
    [SyncVar] public int scanCount = 0;
    [SyncVar] public int maxScanCount = 0;
    [SyncVar] public AnimalType lastKillerType;
    [SyncVar] public DeathReason lastDeathReason;
    [SyncVar] public uint lastKillerNetId;

    [SerializeField] private PlayerMove playerMove;
    [SerializeField] private VisionUIController visionUI;
    [SerializeField] private GameObject corpsePrefab;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Collider2D bodyCollider;

    public IPlayerAbility[] Abilities { get; private set; }
    public LocalVisionLight localVision;
    public readonly SyncList<MissionSlot> Missions = new SyncList<MissionSlot>();

    public BaseMission progressMission;
    public static GamePlayer LocalPlayer;
    public Scanner scanner;
    public TextMeshProUGUI nicknameText;
    public GameObject killIndicatorUIPrefab;
    private GameObject killIndicatorUIInstance;

    private IMissionCompleteHandler[] _missionCompleteHandlers;
    private Coroutine _applyMissionsCo;
    public override void OnStartServer()
    {
        base.OnStartServer();
        Abilities = GetComponents<IPlayerAbility>();
        _missionCompleteHandlers = GetComponents<IMissionCompleteHandler>();
    }
    public override void OnStartClient()
    {
        base.OnStartClient();

        Missions.Callback += OnMissionsChanged;
        Abilities = GetComponents<IPlayerAbility>();
        _missionCompleteHandlers = GetComponents<IMissionCompleteHandler>();
        GamePlayUI.Instance.AddPlayer(this, homeZone);
    }
    public override void OnStartLocalPlayer()
    {       
        base.OnStartLocalPlayer();
        Debug.Log($"[GamePlayer] 내 캐릭터는 {characterName}");

        LocalPlayer = this;
        visionUI = FindObjectOfType<VisionUIController>();
        visionUI.localPlayer = this;

        if (visionUI) visionUI.enabled = true;

        string roomCode = RoomSessionData.CurrentRoomCode;
        string nick = string.IsNullOrWhiteSpace(nickname) ? "Player" : nickname;
        string displayName = VoiceManager.BuildVivoxDisplayName(nick, netId);

        VoiceManager.Instance?.SetGameplayLifeState(RoomSessionData.CurrentRoomCode, displayName, isAlive: false);

        var prox = GetComponent<InGameVoiceProximity>();
        if (prox == null) prox = gameObject.AddComponent<InGameVoiceProximity>();
        prox.enabled = isAlive;

        StartCoroutine(ShowCharacterUI());
    }
    public override void OnStopClient()
    {
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

        MissionListUI.Instance.RefreshList(Missions);

        //if (Missions.Count < 5) yield break;

        var types = new MissionType[Missions.Count];
        for (int i = 0; i < Missions.Count; i++)
            types[i] = Missions[i].Type;

        yield return EnableMissionObjectsWhenReady(types);
    }
    IEnumerator ShowCharacterUI()
    {
        yield return new WaitForSeconds(0.1f);

        AnimalType type = Enum.Parse<AnimalType>(characterName);
        StartCoroutine(GamePlayUI.Instance.ShowCharacter(type));
    }


    [Command]
    public void CmdSendChatMessage(string message)
    {
        bool senderIsGhost = !isAlive;

        foreach (var player in FindObjectsOfType<GamePlayer>())
        {
            bool receiverIsGhost = !player.isAlive;

            if (senderIsGhost && !receiverIsGhost)
                continue;

            player.TargetReceiveMessage(netId, nickname, message);
        }

        var bubble = GetComponentInChildren<SpeechBubble>();
        if (bubble != null)
            bubble.Show(message);
    }

    [Command]
    public void CmdSendWhisper(uint targetNetId, string message)
    {
        if (NetworkServer.spawned.TryGetValue(targetNetId, out var identity))
        {
            var target = identity.GetComponent<GamePlayer>();

            TargetReceiveWhisper(target.netId, netId, message);
            target.TargetReceiveWhisper(netId, target.netId, message);
        }
    }   

    [Command]
    public void CmdScan(uint targetNetId)
    {
        // 채팅에 결과 출력
        GamePlayer target = NetworkServer.spawned[targetNetId].GetComponent<GamePlayer>();

        string character = AnimalNameMap.AnimalTypeToName[target.animalType];
        string disguiseCh = AnimalNameMap.AnimalTypeToName[target.disguisedAs];
        string result;
        string updateMsg;
        if (disguiseCh == "???")
        {
            result = $"{target.nickname}는 {character}입니다.";
            updateMsg = character;
        }
        else
        {
            result = $"{target.nickname}는 {disguiseCh}입니다.";
            updateMsg = disguiseCh;
        }
        scanCount++;
        TargetReceiveScanResult(connectionToClient, target.netId, result, updateMsg);
    }

    [Command]
    public void CmdSetPredict(uint targetNetId)
    {
        GamePlayer predicted = NetworkServer.spawned[targetNetId].GetComponent<GamePlayer>();

        // 예측 기록
        predictedWinner = predicted.animalType;

        // 클라이언트에 예측 UI 갱신 요청
        TargetShowPredictionUI(connectionToClient, predicted.netId);

        Debug.Log($"{nickname}님이 {predicted.nickname}을(를) 승리자로 예측했습니다.");
    }
    [Command]
    public void CmdSetPredict(AnimalType type)
    {
        // 예측 기록
        predictedWinner = type;

        Debug.Log($"{nickname}님이 {AnimalNameMap.AnimalTypeToName[type]}을(를) 승리자로 예측했습니다.");
    }
    [Command]
    public void CmdScanCorpse(uint corpseNetId)
    {
        if (!isAlive) return;
        if (!NetworkServer.spawned.TryGetValue(corpseNetId, out var identity)) return;

        var corpse = identity.GetComponent<Corpse>();
        if (corpse == null) return;

        float dist = Vector2.Distance(transform.position, corpse.transform.position);
        if (dist > 2.0f) return;

        TargetStartCorpseScan(connectionToClient, corpseNetId, corpse);
    }
    [TargetRpc]
    private void TargetStartCorpseScan(NetworkConnectionToClient conn, uint corpseNetId, Corpse corpse)
    {
        if (GamePlayUI.Instance == null) return;

        GamePlayUI.Instance.BeginCorpseScan(corpse);        
    }
    void OnAliveChanged(bool oldVal, bool newVal)
    {
        if (newVal) return;
            
        if (isLocalPlayer)
        {
            FailProgressMission();

            string roomCode = RoomSessionData.CurrentRoomCode;
            string nick = string.IsNullOrWhiteSpace(nickname) ? "Player" : nickname;
            string displayName = VoiceManager.BuildVivoxDisplayName(nick, netId);

            VoiceManager.Instance?.EnterGameplay(roomCode, displayName, isAlive: false);

            var prox = GetComponent<InGameVoiceProximity>();
            if (prox != null) prox.enabled = false;
        }               

        if (isServer)
        {
            SpawnCorpse();
            RpcApplyGhostMode();
                
            if (isPredator)
                GameMamager.Instance.CheckGameOver();
        }

        GamePlayUI.Instance.RemovePlayer(this);

        AllRefreshVisibility();
    }

    void OnZoneChanged(ZoneType oldVal, ZoneType newVal)
    {
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
        if (corpse != null) 
            corpse.Init(netId, lastKillerType, animalType, NetworkTime.time, gameMamager.currentRound, gameMamager.IsNightPhase, gameMamager.timer);
        
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

            visionUI.fov.viewRadius = 11;
            visionUI.fov.occluderMask = 0;                                
        }
    }
    public static void AllRefreshVisibility()
    {
        var players = FindObjectsOfType<GamePlayer>();
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
            var chatManager = FindObjectOfType<ChatManager>();
            if (chatManager != null)
                chatManager.AddWhisperMessage(senderNetId, targetNetId, message);
    }
    [TargetRpc]
    public void TargetReceiveMessage(uint senderNetId, string sender, string message)
    {
        var chatManager = FindObjectOfType<ChatManager>();
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
    public void TargetReceiveScanResult(NetworkConnection target, uint scannedNetId, string message, string updateMsg)
    {
        GamePlayer scanned = NetworkClient.spawned[scannedNetId].GetComponent<GamePlayer>();

        // 1. UI에 캐릭터명 공개
        var slot = PlayerSlotUI.Instance.GetSlotByPlayer(scanned);
        if (slot != null)
            slot.UpdateNicknameWithAnimal(updateMsg);

        var chatManager = FindObjectOfType<ChatManager>();
        chatManager.AddLocalNotice(message);
    }

    [TargetRpc]
    public void TargetShowPredictionUI(NetworkConnection target, uint predictedId)
    {
        GamePlayer scanned = NetworkClient.spawned[predictedId].GetComponent<GamePlayer>();
        // 1. UI에 캐릭터명 공개
        var slot = PlayerSlotUI.Instance.GetSlotByPlayer(scanned);
        slot.MarkPrediction(predictedId); // UI 처리 함수
    }
    
    [Command]
    public void CmdAttack(uint targetNetId)
    {
        if (!isAlive || hasAttacked) return;
        if (!isPredator) return;

        Debug.Log($"[CmdAttack] 공격 로직 호출");

        if (targetNetId == 0)
        {
            Debug.Log("[CmdAttack] targetNetId 타겟 없음");
            return;
        }

        if (!NetworkServer.spawned.TryGetValue(targetNetId, out var identity))
        {
            Debug.LogError($"[CmdAttack] NetworkServer.spawned에 netId={targetNetId} 없음");
            return;
        }           

        var target = identity.GetComponent<GamePlayer>();
        if (target == null)
        {
            Debug.LogError("[CmdAttack] target이 null");
            return;
        }
        if (!target.isAlive) 
        {
            Debug.LogError("[CmdAttack] target이 이미 죽음");
            return;
        } 

        Debug.Log($"[CmdAttack] {nickname}이 {target.nickname} 공격");

        var ctx = new AttackContext
        {
            attacker = this,
            target = target,
            cancelAttack = false,
            killAttacker = false,
            killTarget = false,
            reason = null
        };

        if (Abilities != null)
        {
            for (int i = 0; i < Abilities.Length; i++)
            {
                try { Abilities[i].OnBeforeAttack(ref ctx); }
                catch (Exception e) { Debug.LogError($"Ability error: {Abilities[i].GetType().Name}\n{e}"); }
            }               
        }
            

        var targetAbilities = target.Abilities;
        for (int i = 0; i < targetAbilities.Length; i++)
            targetAbilities[i].OnBeforeAttack(ref ctx);

        // 역공
        if (ctx.killAttacker)
        {
            Die(new DeathInfo
            {
                Reason = DeathReason.CounterKill,
                KillerNetId = target.netId,
                KillerType = target.animalType
            });
        }
        // 공격 취소
        if (ctx.cancelAttack)
        {
            hasAttacked = true;
            return;
        }
        // 공격
        if (ctx.killTarget) 
        {
            target.Die(new DeathInfo
            {
                Reason = DeathReason.KilledByAttack,
                KillerNetId = netId,
                KillerType = animalType
            });
            hasEate = true;
            hasAttacked = true;
        } 

        //if (target.predatorType == PredatorType.Scorpion)
        //{
        //    Die(animalType);
        //    return;
        //}
        //if (PredatorPriority.CanAttack(predatorType, target.predatorType))
        //{
        //    target.Die(animalType);

        //    hasAttacked = true;
        //    hasEate = true;
        //}
        //else
        //{
        //    hasAttacked = true;
        //}
    }
    [Server]
    public void Die(in DeathInfo info)
    {
        if (!isAlive) return;

        lastDeathReason = info.Reason;
        lastKillerNetId = info.KillerNetId;
        lastKillerType = info.KillerType;

        if(animalType == AnimalType.Ostrich || animalType == AnimalType.Zebra)
        {
            var sym = GetComponent<PreySymbiosisAbility>();
            if (sym != null)
                sym.ServerClearPartnerBothSides();
        }        
        else if(animalType == AnimalType.Badger)
        {
            var bga = GetComponent<BadgerAbility>();
            if (bga != null)
                bga.ServerOnlocalDied(info.KillerNetId);
        }       

        isAlive = false;
    }
    [Command]
    public void CmdSetDisguise(AnimalType selectedType)
    {
        disguisedAs = selectedType;
    }
    [Command]
    public void CmdChangeZone(ZoneType newZone)
    {
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
    void OnColorChanged(Color oldColor, Color newColor)
    {
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.color = newColor;
    }
    private IEnumerator EnableMissionObjectsWhenReady(MissionType[] missionTypes)
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

    [Command]
    public void CmdStartMission(MissionType type)
    {
        SetMissionStatusServer(type, MissionStatus.InProgress);
    }

    [Command]
    public void CmdCompleteMission(MissionType type)
    {
        SetMissionStatusServer(type, MissionStatus.Completed);

        if (_missionCompleteHandlers != null)
        {
            for (int i = 0; i < _missionCompleteHandlers.Length; i++)
                _missionCompleteHandlers[i].OnMissionCompletedServer(this, type);
        }
        AbilityDispatcher.ServerOnMissionCompleted(this, type);
    }

    [Command]
    public void CmdCancelMission(MissionType type)
    {
        SetMissionStatusServer(type, MissionStatus.NotStarted);
    }    
    public void TryStartMission(MissionType missionType)
    {
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

        UIManager.Instance.Push(UIPriority.Modal);
        playerMove.StopMove();

        if (slot.Status == MissionStatus.NotStarted)
            CmdStartMission(missionType);

        MissionUIManager.Instance.StartMission(
            missionType,
            onComplete: () =>
            {
                CmdCompleteMission(missionType);
                UIManager.Instance.Pop(UIPriority.Modal);
                MissionRegistry.Instance.DisableType(missionType);
            },
            onClosed: () =>
            {
                UIManager.Instance.Pop(UIPriority.Modal);

                int i = FindMissionIndex(missionType);
                if (i >= 0 && Missions[i].Status != MissionStatus.Completed)
                    CmdCancelMission(missionType);
            },
            this
        );
    }
    public void FailProgressMission()
    {
        if (progressMission == null) return;

        progressMission.Fail();
        progressMission = null;
    }

    public void TryStartInvestigation()
    {       
        playerMove.StopMove();

        GamePlayUI.Instance.Startinvestigation();
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
                isDisguise = false;
                break;
            case "Crocodile":
                animalType = AnimalType.Crocodile;
                predatorType = PredatorType.Crocodile;
                isPredator = true;
                isFly = false;
                canPredict = false;
                canScan = false;
                isDisguise = false;
                break;
            case "Hawk":
                animalType = AnimalType.Hawk;
                predatorType = PredatorType.Hawk;
                isPredator = true;
                isFly = true;
                canPredict = false;
                canScan = false;
                isDisguise = false;
                break;
            case "Hyena":
                animalType = AnimalType.Hyena;
                predatorType = PredatorType.Hyena;
                isPredator = true;
                isFly = false;
                canPredict = false;
                canScan = false;
                isDisguise = false;
                break;
            case "Scorpion":
                animalType = AnimalType.Scorpion;
                predatorType = PredatorType.Scorpion;
                isPredator = false;
                isFly = false;
                canPredict = false;
                canScan = false;
                isDisguise = false;
                break;
            case "Zebra":
                animalType = AnimalType.Zebra;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = false;
                canPredict = false;
                canScan = false;
                isDisguise = false;
                break;
            case "Badger":
                animalType = AnimalType.Badger;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = false;
                canPredict = false;
                canScan = false;
                isDisguise = false;
                break;
            case "Squirrel":
                animalType = AnimalType.Squirrel;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = false;
                canPredict = false;
                canScan = false;
                isDisguise = false;
                break;
            case "Fox":
                animalType = AnimalType.Fox;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = false;
                canPredict = false;
                canScan = false;
                isDisguise = false;
                maxScanCount = 2;
                break;
            case "Ostrich":
                animalType = AnimalType.Ostrich;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = true;
                canPredict = false;
                canScan = false;
                isDisguise = false;
                break;
            case "Plover":
                animalType = AnimalType.Plover;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = true;
                canPredict = false;
                canScan = true;
                isDisguise = false;
                maxScanCount = 2;
                break;
            case "Skunk":
                animalType = AnimalType.Skunk;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = false;
                canPredict = false;
                canScan = false;
                isDisguise = true;
                break;
            case "Crow":
                animalType = AnimalType.Crow;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = true;
                canPredict = true;
                canScan = true;
                isDisguise = false;
                maxScanCount = 2;
                break;
            default:
                animalType = AnimalType.None;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = false;
                canPredict = false;
                canScan = false;
                isDisguise = false;
                break;
        }
    }
}
public static class AbilityDispatcher
{
    [Server]
    public static void ServerOnMissionCompleted(GamePlayer local, MissionType type)
    {
        var abilities = local.GetComponents<IMissionCompleteHandler>();
        for (int i = 0; i < abilities.Length; i++)
        {
            try { abilities[i].OnMissionCompletedServer(local, type); }
            catch (Exception e) { Debug.LogError(e); }
        }
    }
}
