using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static CharacterData;

public class GamePlayUI : MonoBehaviour
{
    public static GamePlayUI Instance;

    [Header("UI")]
    public Canvas canvas;
    public GameObject chatUI;
    public GameObject resultUI;
    public GameObject disguiseUI;
    public GameObject predictUI;
    public GameObject checkUI;
    public GameObject predictCheckUI;
    public GameObject missionListUI;
    public GameObject playerSlotUI;
    public GameObject block;
    public GameObject skyBlock;
    public Slider scanPanel;
    public Slider invProgressPanel;
    public GameObject scanResultUI;
    public GameObject investigationUI;
    public GameObject invCheckBtnGroup;

    [Header("Text")]
    public GameObject characterPanel;
    public TextMeshProUGUI characterText;
    public TextMeshProUGUI territoryText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI timerDisplay;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI checkText;
    public TextMeshProUGUI predictCheckText;
    public TextMeshProUGUI scanKillerType;
    public TextMeshProUGUI scanCorpseType;
    public TextMeshProUGUI scanDeathTime;
    public TextMeshProUGUI investigationDescript;
    public TextMeshProUGUI investigationResult;
    public TextMeshProUGUI investigationType;

    [Header("Zone Panels")]
    public Transform fieldPanel;
    public Transform forestPanel;
    public Transform riverPanel;
    public Transform skyPanel;

    [Header("Prefab")]
    public GameObject uiButtonPrefab;
    public Transform disguiseButtonGroup;
    public Transform predictButtonGroup;
    public Transform investigationButtonGroup;
    public GameObject playerIconPrefab;

    public GameObject investigationObj;

    Animator textAni;
    Animator panelAni;
    Animator territoryAni;
    Animator descriptionAni;
    Animator resultAni;    

    private Coroutine scanCo;
    private Coroutine invCo;

    private Dictionary<GamePlayer, GameObject> iconMap = new();
    public TMP_InputField chatInputField;
    private float disguiseTimer = 10f;
    private float predictTimer = 5f;
    private bool isSelected = false;
    private GamePlayer localPlayer;
    private AnimalType selectType;
    
    private uint InvestigationTargetNetId;
    private string InvestigationTargetNickname;
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        textAni = characterText.GetComponent<Animator>();
        panelAni = characterPanel.GetComponent<Animator>();
        territoryAni = territoryText.GetComponent<Animator>();
        descriptionAni = descriptionText.GetComponent<Animator>();
        resultAni = resultUI.GetComponent<Animator>();
    }
    public void InitLocalPlayer(GamePlayer lgp)
    {
        localPlayer = lgp;
    }

    public IEnumerator ShowCharacter(AnimalType type)
    {
        UIManager.Instance.Push(UIPriority.Modal);
        CharacterInfoData info = CharacterConfig.Characters[type];

        characterText.text = $"{info.DisplayName}";
        territoryText.text = $"거주지: {TranslateTerritory(info.HomeTerritory)}";
        descriptionText.text = info.WinConditionDescription;

        textAni.SetTrigger("ShowText");
        panelAni.SetTrigger("ShowPanel");
        territoryAni.SetTrigger("Territory");
        descriptionAni.SetTrigger("Description");
 
        yield return new WaitForSeconds(5f);

        characterPanel.SetActive(false);

        yield return new WaitForSeconds(0.1f);

        UIManager.Instance.Pop(UIPriority.Modal);
    }

    public void UpdateRoundText(int round, RoundTime time)
    {
        if(time == RoundTime.Disguise)
        {
            roundText.text = "위장 시간";
            timerDisplay.text = "위장 종료까지";
        }
        else if (time == RoundTime.Exploration)
        {
            roundText.text = "탐색 시간";
            timerDisplay.text = "탐색 종료까지";
        }
        else if (time == RoundTime.End)
        {
            roundText.text = "게임 종료";
            timerDisplay.text = "";
        }
        else
        {
            roundText.text = $"{round}일차 {GetTimeName(time)}";
            timerDisplay.text = $"{GetTimeName(time)} 종료까지";
        }
            
    }
    public void ShowDisguiseUI()
    {        
        if (localPlayer.animalType == AnimalType.Fox)
        {
            UIManager.Instance.Push(UIPriority.Modal);
            disguiseUI.SetActive(true);
            disguiseUI.transform.SetAsLastSibling();
            isSelected = false;
            StartCoroutine(CountdownDisguise());
        }     
    }

    IEnumerator CountdownDisguise()
    {
        yield return new WaitForSeconds(disguiseTimer);
        if (!isSelected)
        {
            var randomType = GetRandomAnimalType();
            localPlayer.CmdSetDisguise(randomType);

            string updateText = $"여우 > {AnimalNameMap.AnimalTypeToName[randomType]}";
            PlayerSlot slot = PlayerSlotUI.Instance.GetSlotByPlayer(localPlayer);
            slot.UpdateNicknameWithAnimal(updateText);
            disguiseUI.SetActive(false);
            UIManager.Instance.Pop(UIPriority.Modal);
        }
    }

    public void OnDisguiseSelected(AnimalType type)
    {
        if (isSelected) return;
        isSelected = true;
        localPlayer.CmdSetDisguise(type);
        disguiseUI.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Modal);

        string updateText = $"여우 > {AnimalNameMap.AnimalTypeToName[type]}";
        PlayerSlot slot = PlayerSlotUI.Instance.GetSlotByPlayer(localPlayer);
        slot.UpdateNicknameWithAnimal(updateText);
    }
    public void ShowPredictUI()
    {
        if (localPlayer.animalType == AnimalType.Crow)
        {
            UIManager.Instance.Push(UIPriority.Modal);
            predictUI.SetActive(true);
            predictUI.transform.SetAsLastSibling();
            isSelected = false;
            StartCoroutine(CountdownPredict());
        }
    }

    public void BeginCorpseScan(Corpse corpse)
    {
        if (scanCo != null) StopCoroutine(scanCo);
        scanCo = StartCoroutine(CoScanCorpse(corpse));
    }

    private IEnumerator CoScanCorpse(Corpse corpse)
    {
        if (localPlayer == null) Debug.Log("[CoScanCorpse] 로컬플레이어 Null");
        if (localPlayer.scanner.CurrentTargetCorpse == null) Debug.Log("[CoScanCorpse] 시체옵젝 Null");
        if (corpse.scanPlayers.Contains(localPlayer.netId)) yield break;

        scanPanel.gameObject.SetActive(true);
        scanPanel.transform.SetAsLastSibling();
        scanPanel.value = 0f;
        SetScanPanelPositionOnce(localPlayer.transform);
        UIManager.Instance.Push(UIPriority.Scan);

        float t = 0f;

        while (t < 3)
        {
            t += Time.deltaTime;
            scanPanel.value = Mathf.Clamp01(t / 3);

            if (corpse == null)
            {
                CancelScanUI();
                yield break;
            }

            Vector2 p = localPlayer.transform.position;
            Vector2 c = corpse.transform.position;
            if ((p - c).sqrMagnitude > 4)
            {
                CancelScanUI();
                yield break;
            }
            yield return null;
        }

        CancelScanUI();

        ActiveScanResult(corpse);
    }
    private void SetScanPanelPositionOnce(Transform transform)
    {
        var rt = scanPanel.GetComponent<RectTransform>();
        Vector3 worldPos = transform.position + new Vector3(0, 1.2f, 0);

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            rt.position = screenPos;
            return;
        }

        Camera cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

        RectTransform parent = rt.parent as RectTransform;
        if (parent != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, cam, out var localPoint))
        {
            rt.anchoredPosition = localPoint;
        }
    }
    private void CancelScanUI()
    {
        Debug.Log("[CancelScanUI] 스캔 취소");
        scanPanel.gameObject.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Scan);
        scanPanel.value = 0f;
        scanCo = null;
    }

    public void ActiveScanResult(Corpse corpse)
    {
        int sec = Mathf.Max(0, Mathf.FloorToInt(corpse.ElapsedSec));       

        var rec = new InvestigationRecord
        {
            type = InvestigationType.Corpse,
            recordId = $"{corpse.netId}",
            corpseAnimalType = corpse.deadAnimalType,
            targetAnimalType = corpse.KillerType,

            Day = corpse.deathDay,
            WasNight = corpse.deathWasNight,
            ElapsedSec = corpse.ElapsedSec,

            title = $"{AnimalNameMap.AnimalTypeToName[corpse.deadAnimalType]}",
            subtitle = $"{corpse.deathDay}일차 {(corpse.deathWasNight ? "밤" : "낮")} {sec}초 경과",
            createdAtLocal = Time.time
        };

        InvestigationLog.Instance.Add(rec);
        corpse.scanPlayers.Add(localPlayer.netId);
        ActiveScanResult(rec);
    }
    public void ActiveScanResult(InvestigationRecord record)
    {
        int sec = Mathf.Max(0, Mathf.FloorToInt(record.ElapsedSec));
        scanKillerType.text = AnimalNameMap.AnimalTypeToName[record.targetAnimalType];
        scanCorpseType.text = AnimalNameMap.AnimalTypeToName[record.corpseAnimalType];
        scanDeathTime.text = $"{record.Day}일차 {(record.WasNight ? "밤" : "낮")} {sec}초 경과";

        scanResultUI.SetActive(true);
        scanResultUI.transform.SetAsLastSibling();
        UIManager.Instance.Push(UIPriority.Scan);
    }
    public void DeActiveScanResult()
    {
        scanResultUI.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Scan);
    }

    public void ActivePlayerScanResult(InvestigationRecord record)
    {
        scanKillerType.text = AnimalNameMap.AnimalTypeToName[record.targetAnimalType];
        scanCorpseType.text = AnimalNameMap.AnimalTypeToName[record.corpseAnimalType];
        scanDeathTime.text = $"{record.Day}일차 {(record.WasNight ? "밤" : "낮")} {record.ElapsedSec}초 경과";

        scanResultUI.SetActive(true);
        scanResultUI.transform.SetAsLastSibling();
        UIManager.Instance.Push(UIPriority.Scan);
    }

    IEnumerator CountdownPredict()
    {
        yield return new WaitForSeconds(predictTimer);
        if (!isSelected)
        {
            var randomType = GetRandomAnimalType();
            localPlayer.CmdSetPredict(randomType);

            predictUI.SetActive(false);
            UIManager.Instance.Pop(UIPriority.Modal);
        }
    }

    public void OnPredictSelected(AnimalType type)
    {
        if (isSelected) return;
        if(type != AnimalType.None)
        {
            isSelected = true;
            localPlayer.CmdSetPredict(type);
        }        

        predictUI.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Modal);
    }

    public void CheckPredict()
    {
        if (localPlayer.animalType != AnimalType.Crow || isSelected || selectType != AnimalType.None) return;

        var type = GetRandomAnimalType();
        localPlayer.CmdSetPredict(type);       
    }
    public void Startinvestigation()
    {
        ActiveInvestigationSelectUI();        
    }

    void ActiveInvestigationSelectUI()
    {
        UIManager.Instance.Push(UIPriority.Modal);

        investigationUI.SetActive(true);
        investigationUI.transform.SetAsLastSibling();

        DestroyBtn();

        investigationResult.text = string.Empty;
        investigationType.text = string.Empty;
        investigationType.text = string.Empty;
        InitBtnGroup();       
    }
    public void DeActiveInvestigationSelectUI()
    {
        investigationUI.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Modal);
    }
    private void OnSelectInvestigationTarget(GamePlayer target)
    {
        if (target == null) return;

        InvestigationTargetNetId = target.netId;
        InvestigationTargetNickname = target.nickname;

        DestroyBtn();
        investigationResult.text = $"{target.nickname}를(을) 탐색하시겠습니까?";
        invCheckBtnGroup.SetActive(true);
    }

    public void ProgressInvestigation()
    {
        if (invCo != null) StopCoroutine(invCo);
        invCheckBtnGroup.SetActive(false);
        invCo = StartCoroutine(CoInvestigatePlayerTarget(InvestigationTargetNetId, InvestigationTargetNickname));
    }
    public void CancleInvestigation()
    {
        InitBtnGroup();
        investigationResult.text = string.Empty;
        invCheckBtnGroup.SetActive(false);
    }
    void InitBtnGroup()
    {
        var players = GameMamager.Instance.players;

        foreach (var p in players)
        {
            if (p == localPlayer) continue;

            var btnObj = Instantiate(uiButtonPrefab, investigationButtonGroup);
            var label = btnObj.GetComponentInChildren<TextMeshProUGUI>().text = p.nickname;

            var button = btnObj.GetComponent<Button>();
            button.onClick.AddListener(() => OnSelectInvestigationTarget(p));
        }
    }
    void DestroyBtn()
    {
        foreach (Transform child in investigationButtonGroup)
            Destroy(child.gameObject);
    }
    IEnumerator CoInvestigatePlayerTarget(uint netId, string nickname)
    {
        if (localPlayer == null) Debug.Log("[CoInvestigation] 로컬플레이어 Null");
        if (localPlayer.scanner.CurrentInvestigation == null) Debug.Log("[CoInvestigation] 탐색 옵젝 Null");

        DestroyBtn();

        investigationDescript.text = "탐색 진행중";
        invProgressPanel.gameObject.SetActive(true);
        invProgressPanel.transform.SetAsLastSibling();
        invProgressPanel.value = 0f;
        
        float t = 0f;

        while (t < 5)
        {
            t += Time.deltaTime;
            invProgressPanel.value = Mathf.Clamp01(t / 5);

            yield return null;
        }

        CancelInvestigationScanUI();

        GameMamager.Instance.RequestPlayerType(netId, nickname);
    }
    private void CancelInvestigationScanUI()
    {
        invProgressPanel.gameObject.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Modal);
        invProgressPanel.value = 0f;
        invCo = null;
    }

    public void ActiveInvestigationResult(uint targetNetId, string targetNickname, AnimalType targetType, bool isPredator)
    {
        var gameMamager = GameMamager.Instance;
        
        var rec = new InvestigationRecord
        {
            type = InvestigationType.PlayerIdentity,
            recordId = $"{targetNetId}",
            title = targetNickname,
            subtitle = $"탐색 결과: {AnimalNameMap.AnimalTypeToName[targetType]}",
            Day = gameMamager.currentRound,
            WasNight = gameMamager.IsNightPhase,
            ElapsedSec = gameMamager.timer,
            targetAnimalType = targetType,
            createdAtLocal = Time.time,
        };

        InvestigationLog.Instance.Add(rec);

        investigationDescript.text = "탐색 완료";
        investigationResult.text = "탐색 결과";
        investigationType.text = AnimalNameMap.AnimalTypeToName[targetType];
        investigationType.color = isPredator ? Color.red : Color.green;
    }
    public void DeActiveInvestigationResult()
    {
        investigationUI.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Modal);
    }
    private string TranslateTerritory(TerritoryType type)
    {
        return type switch
        {
            TerritoryType.Sky => "하늘",
            TerritoryType.River => "강",
            TerritoryType.Field => "들",
            TerritoryType.Forest => "숲",
            _ => "???"
        };
    }
    private string GetTimeName(RoundTime time)
    {
        return time == RoundTime.Day ? "낮" : "밤";
    }
    public void ActiveSkyBlock()
    {
        skyBlock.SetActive(true);       
    }

    public void DeActiveChatUI()
    {
        chatUI.SetActive(false);
    }
    public void DeActivePlayerSlotUI()
    {
        playerSlotUI.SetActive(false);
    }
    public void ActiveBlock()
    {
        block.SetActive(true);
    }
    public void DeActiveBlock()
    {
        block.SetActive(false);
    }
  
    public void ShowResult(bool isWin)
    {
        if (isWin)
            resultText.text = "승 리";
        else
            resultText.text = "패 배";

        chatUI.SetActive(false);
        resultUI.SetActive(true);
        resultAni.SetTrigger("ShowResult");
    }

    public void SetDisguiseOptions()
    {
        // 기존 버튼 제거
        foreach (Transform child in disguiseButtonGroup)
            Destroy(child.gameObject);

        foreach (var type in allAnimals)
        {
            GameObject btn = Instantiate(uiButtonPrefab, disguiseButtonGroup);
            string label = AnimalNameMap.AnimalTypeToName[type];
            btn.GetComponentInChildren<TextMeshProUGUI>().text = label;

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectType = type;
                checkText.text = $"정말로 {label}(으)로 \n위장하시겠습니까??";
                checkUI.SetActive(true);
            });
        }
        disguiseUI.SetActive(false);
    }

    public void SetPredictOptions()
    {
        // 기존 버튼 제거
        foreach (Transform child in predictButtonGroup)
            Destroy(child.gameObject);

        foreach (var type in allAnimals)
        {
            GameObject btn = Instantiate(uiButtonPrefab, predictButtonGroup);
            string label = AnimalNameMap.AnimalTypeToName[type];
            btn.GetComponentInChildren<TextMeshProUGUI>().text = label;

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectType = type;
                predictCheckText.text = $"{label}(을)를 승리자로\n예측 하시겠습니까??";
                predictCheckUI.SetActive(true);
            });
        }
        GameObject cancleBtn = Instantiate(uiButtonPrefab, predictButtonGroup);
        cancleBtn.GetComponentInChildren<TextMeshProUGUI>().text = "플레이어 예측";
        cancleBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            selectType = AnimalType.None;
            predictCheckText.text = $"동물이 아닌 플레이어로 승리자를 예측 하시겠습니까??\n플레이어 예측은 우측 상단의 프로필 > 우클릭 > 예측버튼을 통해 가능합니다.";
            predictCheckUI.SetActive(true);
        });
        predictUI.SetActive(false);
    }
    public void OnClickChatBtn()
    {
        chatUI.SetActive(!chatUI.activeSelf);
    }
    public void DeActiveCheckUI()
    {
        checkUI.SetActive(false);
        selectType = AnimalType.None;
        UIManager.Instance.Pop(UIPriority.Modal);
    }
    public void DeActiveDisguiseUI()
    {
        OnDisguiseSelected(selectType);
        checkUI.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Modal);
    }

    public void DeActivePredictCheckUI()
    {
        predictCheckUI.SetActive(false);
        selectType = AnimalType.None;
        UIManager.Instance.Pop(UIPriority.Modal);
    }
    public void DeActivePredictUI()
    {
        OnPredictSelected(selectType);
        predictCheckUI.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Modal);
    }

    public void OnClickMissionListUI()
    {
        missionListUI.SetActive(!missionListUI.activeSelf);
    }
    public void OnClickProfileSlotUI()
    {
        if (playerSlotUI.activeSelf)
        {
            playerSlotUI.SetActive(false);
            UIManager.Instance.Pop(UIPriority.Modal);
        }
        else
        {
            playerSlotUI.SetActive(true);
            UIManager.Instance.Push(UIPriority.Modal);
        }
    }
    public void AddPlayer(GamePlayer player, ZoneType zone)
    {
        if (iconMap.ContainsKey(player)) return;

        var icon = Instantiate(playerIconPrefab, GetZonePanel(zone));
        icon.GetComponentInChildren<TMP_Text>().text = player.nickname;
        iconMap[player] = icon;
    }

    public void MovePlayerIcon(GamePlayer player, ZoneType newZone)
    {
        if (!iconMap.ContainsKey(player)) return;

        iconMap[player].transform.SetParent(GetZonePanel(newZone), false);
    }

    public void RemovePlayer(GamePlayer player)
    {
        if (iconMap.TryGetValue(player, out var icon))
        {
            Destroy(icon);
            iconMap.Remove(player);
        }
    }
    AnimalType GetRandomAnimalType()
    {       
        int index = UnityEngine.Random.Range(0, allAnimals.Length);
        return allAnimals[index];
    }

    AnimalType[] allAnimals = new AnimalType[]
        {
            AnimalType.Wolf,
            AnimalType.Crocodile,
            AnimalType.Fox,
            AnimalType.Scorpion,
            AnimalType.Hawk,
            AnimalType.Plover,
            AnimalType.Crow,
            AnimalType.Hyena
        };
    Transform GetZonePanel(ZoneType zone) => zone switch
    {
        ZoneType.Field => fieldPanel,
        ZoneType.Forest => forestPanel,
        ZoneType.River => riverPanel,
        ZoneType.Sky => skyPanel,
        _ => null
    };
}
