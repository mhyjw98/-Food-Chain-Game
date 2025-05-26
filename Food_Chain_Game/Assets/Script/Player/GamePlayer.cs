using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.TextCore.Text;
using static CharacterData;

public enum AnimalType { Lion, Crocodile, Eagle, Hyena, Snake, Rabbit, Plover, Otter, Mouse, Mallard, Deer, Crow, Chameleon, None }
public enum PredatorType { Prey, Hyena, Eagle, Crocodile, Lion, Snake }
public class GamePlayer : NetworkBehaviour
{
    [SyncVar] public string characterName;
    [SyncVar] public string nickname;
    [SyncVar] public ZoneType homeZone;
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
    [SyncVar] public bool predictedCorrectly;
    [SyncVar] public bool isWin;
    [SyncVar] public string memo;
    [SyncVar] public int scanCount = 0;
    [SyncVar] public int maxScanCount = 0;

    public Scanner scanner;
    public GameObject killIndicatorUIPrefab;
    private GameObject killIndicatorUIInstance;

    public override void OnStartClient()
    {
        base.OnStartClient();

        GamePlayUI.Instance.AddPlayer(this, homeZone);
    }
    public override void OnStartLocalPlayer()
    {
        Debug.Log($"[GamePlayer] �� ĳ���ʹ� {characterName}");
        base.OnStartLocalPlayer();
        StartCoroutine(ShowCharacterUI());
    }

    IEnumerator ShowCharacterUI()
    {
        yield return new WaitForSeconds(0.1f);

        CharacterType type = Enum.Parse<CharacterType>(characterName);
        StartCoroutine(GamePlayUI.Instance.ShowCharacter(type));
    }

    [Command]
    public void CmdSendChatMessage(string message)
    {
        foreach (var player in FindObjectsOfType<GamePlayer>())
        {
            TargetReceiveMessage(nickname, message);
        }
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
        // ä�ÿ� ��� ���
        GamePlayer target = NetworkServer.spawned[targetNetId].GetComponent<GamePlayer>();

        string character = AnimalNameMap.AnimalTypeToName[target.animalType];
        string disguiseCh = AnimalNameMap.AnimalTypeToName[target.disguisedAs];
        string result;
        string updateMsg;
        if (disguiseCh == "???")
        {
            result = $"{target.nickname}�� {character}�Դϴ�.";
            updateMsg = character;
        }
        else
        {
            result = $"{target.nickname}�� {disguiseCh}�Դϴ�.";
            updateMsg = disguiseCh;
        }
        scanCount++;
        TargetReceiveScanResult(connectionToClient, target.netId, result, updateMsg);
    }

    [Command]
    public void CmdPredict(uint targetNetId)
    {
        GamePlayer predicted = NetworkServer.spawned[targetNetId].GetComponent<GamePlayer>();

        // ���� ���
        predictedWinner = predicted.animalType;

        // Ŭ���̾�Ʈ�� ���� UI ���� ��û
        TargetShowPredictionUI(connectionToClient, predicted.netId);

        Debug.Log($"{nickname}���� {predicted.nickname}��(��) �¸��ڷ� �����߽��ϴ�.");
    }
    void OnAliveChanged(bool oldVal, bool newVal)
    {
        if (!newVal)
        {
            // ��� UI ó��, ��Ȱ��ȭ ��
            foreach (var conn in NetworkServer.connections.Values)
            {
                var gp = conn.identity.GetComponent<GamePlayer>();
                gp.TargetReceiveMessage("System", $"{nickname}���� ����߽��ϴ�.");
            }
            //foreach (var player in FindObjectsOfType<GamePlayer>())
            //{
            //    if (player.connectionToClient != null)
            //    {
            //        player.TargetReceiveMessage("System", $"{nickname}���� ����߽��ϴ�.");
            //    }
            //}
            gameObject.SetActive(false);
        }
    }
    void OnZoneChanged(ZoneType oldVal, ZoneType newVal)
    {
        GamePlayUI.Instance.MovePlayerIcon(this, newVal);
    }
    [TargetRpc]
    public void TargetReceiveWhisper(uint targetNetId, uint senderNetId, string message)
    {
            var chatManager = FindObjectOfType<ChatManager>();
            if (chatManager != null)
                chatManager.AddWhisperMessage(senderNetId, targetNetId, message);
    }
    [TargetRpc]
    public void TargetReceiveMessage(string sender, string message)
    {
        var chatManager = FindObjectOfType<ChatManager>();
        chatManager.AddSystemMessage(sender, message);
    }
    [TargetRpc]
    public void TargetReceiveScanResult(NetworkConnection target, uint scannedNetId, string message, string updateMsg)
    {
        GamePlayer scanned = NetworkClient.spawned[scannedNetId].GetComponent<GamePlayer>();

        // 1. UI�� ĳ���͸� ����
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
        // 1. UI�� ĳ���͸� ����
        var slot = PlayerSlotUI.Instance.GetSlotByPlayer(scanned);
        slot.MarkPrediction(predictedId); // UI ó�� �Լ�
    }
    
    [Command]
    public void CmdAttack()
    {
        if (!isAlive || hasAttacked) return;
        if (!GameMamager.Instance.IsNightPhase) return;
        if (animalType == AnimalType.Snake) return;

        GamePlayer target = scanner.FindValidTarget(this);

        if (target == null) return;

        foreach (var conn in NetworkServer.connections.Values)
        {
            var gp = conn.identity.GetComponent<GamePlayer>();
            gp.TargetReceiveMessage("System", $"{nickname}���� {target.nickname}�� �����߽��ϴ�.");
        }

        if (target.predatorType == PredatorType.Snake)
        {
            isAlive = false;
            return;
        }
        if (PredatorPriority.CanAttack(this.predatorType, target.predatorType))
        {
            target.isAlive = false;
            hasAttacked = true;
            hasEate = true;
        }
        else
        {
            hasAttacked = true;
        }       
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
    public void SetAnimalType(string characterName)
    {
        switch (characterName)
        {
            case "Lion":
                animalType = AnimalType.Lion;
                predatorType = PredatorType.Lion;
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
            case "Eagle":
                animalType = AnimalType.Eagle;
                predatorType = PredatorType.Eagle;
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
            case "Snake":
                animalType = AnimalType.Snake;
                predatorType = PredatorType.Snake;
                isPredator = false;
                isFly = false;
                canPredict = false;
                canScan = false;
                isDisguise = false;
                break;
            case "Deer":
                animalType = AnimalType.Deer;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = false;
                canPredict = false;
                canScan = false;
                isDisguise = false;
                break;
            case "Otter":
                animalType = AnimalType.Otter;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = false;
                canPredict = false;
                canScan = false;
                isDisguise = false;
                break;
            case "Rabbit":
                animalType = AnimalType.Rabbit;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = false;
                canPredict = false;
                canScan = false;
                isDisguise = false;
                break;
            case "Mouse":
                animalType = AnimalType.Mouse;
                predatorType = PredatorType.Prey;
                isPredator = false;
                isFly = false;
                canPredict = false;
                canScan = true;
                isDisguise = false;
                maxScanCount = 2;
                break;
            case "Mallard":
                animalType = AnimalType.Mallard;
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
            case "Chameleon":
                animalType = AnimalType.Chameleon;
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
