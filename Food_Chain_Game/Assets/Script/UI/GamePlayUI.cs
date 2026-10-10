using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    [Header("Scan Result Labels")]
    [Tooltip("결과 창의 항목 이름. 시체 조사 기록과 플레이어 탐색 기록이 같은 창을 쓰므로 기록 종류에 맞게 바꿔 표시한다")]
    [SerializeField] private TextMeshProUGUI scanTimeLabel;
    [SerializeField] private TextMeshProUGUI scanCorpseLabel;
    [SerializeField] private TextMeshProUGUI scanKillerLabel;
    [Tooltip("게임 도중 나간 플레이어의 시체를 조사했을 때 공격자 칸에 표시할 문구(임시)")]
    [SerializeField] private string leftDeathText = "퇴장";

    [Header("Timing")]
    [Tooltip("캐릭터 소개 연출 동안 조작을 막는 시간(초)")]
    [SerializeField] private float characterIntroSeconds = 5f;
    [SerializeField] private float characterIntroUnlockDelay = 0.1f;

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
    private bool isInvestigating;
    private bool isCorpseScanning;

    private Dictionary<GamePlayer, GameObject> iconMap = new();
    public TMP_InputField chatInputField;
    private bool _predictOpen;
    private bool _disguiseOpen;
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
        if (!CharacterConfig.Characters.TryGetValue(type, out CharacterInfoData info))
        {
            Debug.LogWarning($"[GamePlayUI] 캐릭터 정보가 없어 소개 연출을 건너뜁니다: {type}");
            yield break;
        }

        UIManager.Instance.Push(UIPriority.Modal);

        characterText.text = $"{info.DisplayName}";
        territoryText.text = $"거주지: {TranslateTerritory(info.HomeTerritory)}";
        descriptionText.text = info.WinConditionDescription;

        textAni.SetTrigger("ShowText");
        panelAni.SetTrigger("ShowPanel");
        territoryAni.SetTrigger("Territory");
        descriptionAni.SetTrigger("Description");
 
        yield return new WaitForSeconds(characterIntroSeconds);

        characterPanel.SetActive(false);

        yield return new WaitForSeconds(characterIntroUnlockDelay);

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
    // 위장 창. "위장 시간" 단계는 없어지고 위장 오브젝트로 대체될 예정이라 시간 제한(자동 무작위 선택)은 두지 않는다.
    // 이동 잠금(Modal)은 창을 열 때 한 번 걸고, 닫을 때(CloseDisguiseUI) 한 번만 푼다
    public void ShowDisguiseUI()
    {
        if (localPlayer == null || localPlayer.animalType != AnimalType.Fox) return;
        if (_disguiseOpen) return;

        _disguiseOpen = true;
        selectType = AnimalType.None;

        UIManager.Instance.Push(UIPriority.Modal);
        checkUI.SetActive(false);
        disguiseUI.SetActive(true);
        disguiseUI.transform.SetAsLastSibling();
    }

    // 선택하지 않고 닫을 때도 이 메서드를 쓴다(닫기 버튼/위장 오브젝트에서 벗어남 등)
    public void CloseDisguiseUI()
    {
        checkUI.SetActive(false);
        disguiseUI.SetActive(false);
        selectType = AnimalType.None;

        if (!_disguiseOpen) return;
        _disguiseOpen = false;
        UIManager.Instance.Pop(UIPriority.Modal);
    }

    public void OnDisguiseSelected(AnimalType type)
    {
        if (!_disguiseOpen || localPlayer == null) return;
        if (type == AnimalType.None || !AnimalNameMap.AnimalTypeToName.TryGetValue(type, out string animalName)) return;

        localPlayer.CmdSetDisguise(type);

        PlayerSlot slot = PlayerSlotUI.Instance != null ? PlayerSlotUI.Instance.GetSlotByPlayer(localPlayer) : null;
        if (slot != null) slot.UpdateNicknameWithAnimal($"여우 > {animalName}");

        CloseDisguiseUI();
    }
    // 예측 창(까마귀). "탐색 시간" 단계는 없어지고 예측 오브젝트로 대체될 예정이라 시간 제한(자동 무작위 선택)은 두지 않는다.
    // 이동 잠금(Modal)은 창을 열 때 한 번 걸고, 닫을 때(ClosePredictUI) 한 번만 푼다
    public void ShowPredictUI()
    {
        if (localPlayer == null || localPlayer.animalType != AnimalType.Crow) return;
        if (_predictOpen) return;

        _predictOpen = true;
        selectType = AnimalType.None;

        UIManager.Instance.Push(UIPriority.Modal);
        predictCheckUI.SetActive(false);
        predictUI.SetActive(true);
        predictUI.transform.SetAsLastSibling();
    }

    // 선택하지 않고 닫을 때도 이 메서드를 쓴다(닫기 버튼/예측 오브젝트에서 벗어남 등)
    public void ClosePredictUI()
    {
        predictCheckUI.SetActive(false);
        predictUI.SetActive(false);
        selectType = AnimalType.None;

        if (!_predictOpen) return;
        _predictOpen = false;
        UIManager.Instance.Pop(UIPriority.Modal);
    }

    // 서버가 조사 시작을 승인했을 때 호출됨(GamePlayer.TargetStartCorpseScan)
    public void BeginCorpseScan(Corpse corpse)
    {
        if (isCorpseScanning || corpse == null || localPlayer == null) return;

        isCorpseScanning = true;
        UIManager.Instance.Push(UIPriority.Scan);
        scanCo = StartCoroutine(CoScanCorpse(corpse, localPlayer.CorpseScanDuration, localPlayer.CorpseScanRange));
    }

    // 진행 바는 연출용. 실제 판정과 사망 정보 전송은 서버(GamePlayer.ServerCoScanCorpse)가 한다
    private IEnumerator CoScanCorpse(Corpse corpse, float duration, float range)
    {
        scanPanel.gameObject.SetActive(true);
        scanPanel.transform.SetAsLastSibling();
        scanPanel.value = 0f;
        SetScanPanelPositionOnce(localPlayer.transform);

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            scanPanel.value = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;

            bool outOfRange = corpse == null
                || ((Vector2)localPlayer.transform.position - (Vector2)corpse.transform.position).sqrMagnitude > range * range;

            if (outOfRange)
            {
                localPlayer.CmdCancelCorpseScan();
                CancelScanUI();
                yield break;
            }
            yield return null;
        }

        // 바가 다 찬 뒤에는 서버 결과(OnCorpseScanResult / OnCorpseScanCanceled)를 기다린다
        scanCo = null;
    }

    public void OnCorpseScanResult(uint corpseNetId, AnimalType deadType, AnimalType killerType, DeathReason deathReason, int day, bool wasNight, float elapsedSec)
    {
        if (!isCorpseScanning) return;
        CancelScanUI();

        int sec = Mathf.Max(0, Mathf.FloorToInt(elapsedSec));
        AnimalNameMap.AnimalTypeToName.TryGetValue(deadType, out string deadName);

        var rec = new InvestigationRecord
        {
            type = InvestigationType.Corpse,
            recordId = $"{corpseNetId}",
            corpseAnimalType = deadType,
            targetAnimalType = killerType,
            deathReason = deathReason,

            Day = day,
            WasNight = wasNight,
            ElapsedSec = elapsedSec,

            title = deadName ?? "???",
            subtitle = $"{day}일차 {(wasNight ? "밤" : "낮")} {sec}초 경과",
            createdAtLocal = Time.time
        };

        if (InvestigationLog.Instance != null)
            InvestigationLog.Instance.Add(rec);

        ActiveScanResult(rec);
    }

    public void OnCorpseScanCanceled()
    {
        if (!isCorpseScanning) return;
        CancelScanUI();
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
        // Push는 BeginCorpseScan에서 한 번만 하므로 조사 중일 때만 한 번 Pop
        if (!isCorpseScanning) return;
        isCorpseScanning = false;

        if (scanCo != null)
        {
            StopCoroutine(scanCo);
            scanCo = null;
        }

        scanPanel.gameObject.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Scan);
        scanPanel.value = 0f;
    }
    // 시체 조사 기록: 사망 시각 / 사망자 / 공격자
    public void ActiveScanResult(InvestigationRecord record)
    {
        SetScanResultLabels("사망 시각 :", "사망자 :", "공격자 :");

        scanDeathTime.text = FormatRecordTime(record);
        scanCorpseType.text = GetAnimalName(record.corpseAnimalType);
        // 게임 도중 나간 플레이어의 시체는 공격자 대신 "퇴장"으로 표시(임시)
        scanKillerType.text = record.deathReason == DeathReason.Left ? leftDeathText : GetAnimalName(record.targetAnimalType);

        OpenScanResultUI();
    }
    public void DeActiveScanResult()
    {
        if (!scanResultUI.activeSelf) return;

        scanResultUI.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Scan);
    }

    // 플레이어 탐색 기록: 탐색 시각 / 대상(닉네임) / 정체.
    // 시체 조사와 같은 창을 쓰므로 항목 이름을 바꿔 준다(그대로 두면 "사망자: 늑대"처럼 엉뚱하게 표시된다)
    public void ActivePlayerScanResult(InvestigationRecord record)
    {
        SetScanResultLabels("탐색 시각 :", "대상 :", "정체 :");

        scanDeathTime.text = FormatRecordTime(record);
        scanCorpseType.text = record.title;
        scanKillerType.text = GetAnimalName(record.targetAnimalType);

        OpenScanResultUI();
    }

    // 결과 창이 이미 열려 있을 때 다른 기록을 누르면 내용만 바뀐다.
    // Push는 창을 열 때 한 번만 해야 닫을 때의 Pop 한 번과 짝이 맞는다(중복 Push는 상호작용 잠금이 풀리지 않는 원인)
    private void OpenScanResultUI()
    {
        scanResultUI.transform.SetAsLastSibling();
        if (scanResultUI.activeSelf) return;

        scanResultUI.SetActive(true);
        UIManager.Instance.Push(UIPriority.Scan);
    }

    private void SetScanResultLabels(string time, string corpse, string killer)
    {
        if (scanTimeLabel != null) scanTimeLabel.text = time;
        if (scanCorpseLabel != null) scanCorpseLabel.text = corpse;
        if (scanKillerLabel != null) scanKillerLabel.text = killer;
    }

    private static string FormatRecordTime(InvestigationRecord record)
    {
        int sec = Mathf.Max(0, Mathf.FloorToInt(record.ElapsedSec));
        return $"{record.Day}일차 {(record.WasNight ? "밤" : "낮")} {sec}초 경과";
    }

    private static string GetAnimalName(AnimalType type)
    {
        return AnimalNameMap.AnimalTypeToName.TryGetValue(type, out string animalName) ? animalName : "???";
    }

    // type이 None이면 "플레이어 예측"을 고른 것: 서버에 보내지 않고 창만 닫는다(플레이어 목록에서 따로 예측)
    public void OnPredictSelected(AnimalType type)
    {
        if (!_predictOpen || localPlayer == null) return;

        if (type != AnimalType.None)
        {
            isSelected = true;
            localPlayer.CmdSetPredict(type);
        }

        ClosePredictUI();
    }

    public void CheckPredict()
    {
        if (localPlayer == null || localPlayer.animalType != AnimalType.Crow || isSelected || selectType != AnimalType.None) return;

        var type = GetRandomAnimalType();
        localPlayer.CmdSetPredict(type);
    }
    public void Startinvestigation()
    {
        if (investigationUI.activeSelf) return;

        ActiveInvestigationSelectUI();
    }

    void ActiveInvestigationSelectUI()
    {
        UIManager.Instance.Push(UIPriority.Modal);

        investigationUI.SetActive(true);
        investigationUI.transform.SetAsLastSibling();

        DestroyBtn();

        investigationDescript.text = string.Empty;
        investigationResult.text = string.Empty;
        investigationType.text = string.Empty;
        invCheckBtnGroup.SetActive(false);
        HideInvestigationProgress();
        InitBtnGroup();
    }
    // Modal은 창을 열 때 한 번 Push, 닫을 때 한 번만 Pop
    public void DeActiveInvestigationSelectUI()
    {
        if (!investigationUI.activeSelf) return;

        if (isInvestigating)
        {
            isInvestigating = false;
            if (localPlayer != null) localPlayer.CmdCancelInvestigation();
        }
        HideInvestigationProgress();

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
        if (isInvestigating || localPlayer == null) return;

        invCheckBtnGroup.SetActive(false);
        isInvestigating = true;
        localPlayer.CmdBeginInvestigation(InvestigationTargetNetId);

        if (invCo != null) StopCoroutine(invCo);
        invCo = StartCoroutine(CoInvestigationProgress(localPlayer.InvestigationDuration));
    }
    public void CancleInvestigation()
    {
        InitBtnGroup();
        investigationResult.text = string.Empty;
        invCheckBtnGroup.SetActive(false);
    }
    void InitBtnGroup()
    {
        if (GameMamager.Instance == null) return;

        var players = GameMamager.Instance.players;

        foreach (var p in players)
        {
            // 나간 플레이어는 목록에 null로 남을 수 있다
            if (p == null || p == localPlayer) continue;

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
    // 진행 바는 연출용. 실제 시간/조건 판정과 결과 전송은 서버(GamePlayer.ServerCoInvestigate)가 한다
    IEnumerator CoInvestigationProgress(float duration)
    {
        DestroyBtn();

        investigationDescript.text = "탐색 진행중";
        investigationResult.text = string.Empty;
        invProgressPanel.gameObject.SetActive(true);
        invProgressPanel.transform.SetAsLastSibling();
        invProgressPanel.value = 0f;

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            invProgressPanel.value = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;

            yield return null;
        }

        invCo = null;
    }
    private void HideInvestigationProgress()
    {
        if (invCo != null)
        {
            StopCoroutine(invCo);
            invCo = null;
        }

        invProgressPanel.gameObject.SetActive(false);
        invProgressPanel.value = 0f;
    }

    public void OnInvestigationCanceled()
    {
        if (!isInvestigating) return;
        isInvestigating = false;

        HideInvestigationProgress();

        investigationDescript.text = "탐색 실패";
        investigationResult.text = "탐색 장소를 벗어났거나 탐색할 수 없는 상태입니다.";
        investigationType.text = string.Empty;

        DestroyBtn();
        InitBtnGroup();
    }

    public void ActiveInvestigationResult(uint targetNetId, string targetNickname, AnimalType targetType, bool isPredator)
    {
        if (!isInvestigating) return;
        isInvestigating = false;

        HideInvestigationProgress();

        var gameMamager = GameMamager.Instance;

        var rec = new InvestigationRecord
        {
            type = InvestigationType.PlayerIdentity,
            recordId = $"{targetNetId}",
            title = targetNickname,
            subtitle = $"탐색 결과: {GetAnimalName(targetType)}",
            Day = gameMamager.currentRound,
            WasNight = gameMamager.IsNightPhase,
            ElapsedSec = gameMamager.PhaseElapsedSeconds,
            targetAnimalType = targetType,
            createdAtLocal = Time.time,
        };

        if (InvestigationLog.Instance != null)
            InvestigationLog.Instance.Add(rec);

        investigationDescript.text = "탐색 완료";
        investigationResult.text = "탐색 결과";
        investigationType.text = GetAnimalName(targetType);
        investigationType.color = isPredator ? Color.red : Color.green;
    }
    public void DeActiveInvestigationResult()
    {
        DeActiveInvestigationSelectUI();
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

        // 위장 후보는 모든 동물
        foreach (AnimalType type in Enum.GetValues(typeof(AnimalType)))
        {
            if (type == AnimalType.None) continue;
            if (!AnimalNameMap.AnimalTypeToName.TryGetValue(type, out string label)) continue;

            AnimalType candidate = type;
            GameObject btn = Instantiate(uiButtonPrefab, disguiseButtonGroup);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = label;

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectType = candidate;
                checkText.text = $"정말로 {label}(으)로 \n위장하시겠습니까??";
                checkUI.SetActive(true);
                checkUI.transform.SetAsLastSibling();
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
                predictCheckUI.transform.SetAsLastSibling();
            });
        }
        GameObject cancleBtn = Instantiate(uiButtonPrefab, predictButtonGroup);
        cancleBtn.GetComponentInChildren<TextMeshProUGUI>().text = "플레이어 예측";
        cancleBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            selectType = AnimalType.None;
            predictCheckText.text = $"동물이 아닌 플레이어로 승리자를 예측 하시겠습니까??\n플레이어 예측은 우측 상단의 프로필 > 우클릭 > 예측버튼을 통해 가능합니다.";
            predictCheckUI.SetActive(true);
            predictCheckUI.transform.SetAsLastSibling();
        });
        predictUI.SetActive(false);
    }
    public void OnClickChatBtn()
    {
        chatUI.SetActive(!chatUI.activeSelf);
    }
    // 위장 확인창 "아니요": 확인창만 닫고 선택 창으로 돌아간다(잠금은 그대로)
    public void DeActiveCheckUI()
    {
        checkUI.SetActive(false);
        selectType = AnimalType.None;
    }
    // 위장 확인창 "네": 선택을 확정하고 창을 닫는다
    public void DeActiveDisguiseUI()
    {
        checkUI.SetActive(false);
        OnDisguiseSelected(selectType);
    }

    // 예측 확인창 "아니요": 확인창만 닫고 선택 창으로 돌아간다(잠금은 그대로)
    public void DeActivePredictCheckUI()
    {
        predictCheckUI.SetActive(false);
        selectType = AnimalType.None;
    }
    // 예측 확인창 "네": 선택을 확정하고 창을 닫는다
    public void DeActivePredictUI()
    {
        AnimalType chosen = selectType;
        predictCheckUI.SetActive(false);
        OnPredictSelected(chosen);
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
