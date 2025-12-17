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
    
    public GameObject characterPanel;
    public TextMeshProUGUI characterText;
    public TextMeshProUGUI territoryText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI timerDisplay;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI checkText;
    public TextMeshProUGUI predictCheckText;

    [Header("Zone Panels")]
    public Transform fieldPanel;
    public Transform forestPanel;
    public Transform riverPanel;
    public Transform skyPanel;

    [Header("Prefab")]
    public GameObject uiButtonPrefab;
    public Transform disguiseButtonGroup;
    public Transform predictButtonGroup;
    public GameObject playerIconPrefab;

    Animator textAni;
    Animator panelAni;
    Animator territoryAni;
    Animator descriptionAni;
    Animator resultAni;

    private Dictionary<GamePlayer, GameObject> iconMap = new();
    private static TMP_InputField[] inputFields;
    public TMP_InputField chatInputField;
    private float disguiseTimer = 10f;
    private float predictTimer = 5f;
    private bool isSelected = false;
    private GamePlayer localPlayer;
    private AnimalType selectType;
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        inputFields = FindObjectsOfType<TMP_InputField>(true);
        textAni = characterText.GetComponent<Animator>();
        panelAni = characterPanel.GetComponent<Animator>();
        territoryAni = territoryText.GetComponent<Animator>();
        descriptionAni = descriptionText.GetComponent<Animator>();
        resultAni = resultUI.GetComponent<Animator>();
    }
    void Update()
    {       
        PlayerMove.isStop = false;

        foreach (var input in inputFields)
        {
            if (input.isFocused)
            {
                PlayerMove.isStop = true;
                break;
            }
        }

        if (Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrWhiteSpace(chatInputField.text))
        {
            string msg = chatInputField.text.Trim();

            if (NetworkClient.connection != null && NetworkClient.connection.identity != null)
            {
                ChatManager chatManager = FindObjectOfType<ChatManager>();

                if (chatManager.currentChannel.type == ChatChannelType.Whisper && chatManager.currentChannel.targetPlayer != null)
                {
                    localPlayer.CmdSendWhisper(chatManager.currentChannel.targetPlayer.netId, msg);
                }
                else
                {
                    localPlayer.CmdSendChatMessage(msg);
                }
            }

            chatInputField.text = "";
        }
    }

    public IEnumerator ShowCharacter(AnimalType type)
    {
        PlayerMove.isEvent = true;
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

        PlayerMove.isEvent = false;
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
        localPlayer = NetworkClient.localPlayer.GetComponent<GamePlayer>();
        if (localPlayer.animalType == AnimalType.Fox)
        {
            disguiseUI.SetActive(true);
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
        }
    }

    public void OnDisguiseSelected(AnimalType type)
    {
        if (isSelected) return;
        isSelected = true;
        localPlayer.CmdSetDisguise(type);
        disguiseUI.SetActive(false);

        string updateText = $"여우 > {AnimalNameMap.AnimalTypeToName[type]}";
        PlayerSlot slot = PlayerSlotUI.Instance.GetSlotByPlayer(localPlayer);
        slot.UpdateNicknameWithAnimal(updateText);
    }
    public void ShowPredictUI()
    {
        localPlayer = NetworkClient.localPlayer.GetComponent<GamePlayer>();
        if (localPlayer.animalType == AnimalType.Crow)
        {
            predictUI.SetActive(true);
            isSelected = false;
            StartCoroutine(CountdownPredict());
        }
    }

    IEnumerator CountdownPredict()
    {
        yield return new WaitForSeconds(predictTimer);
        if (!isSelected)
        {
            var randomType = GetRandomAnimalType();
            localPlayer.CmdSetPredict(randomType);

            predictUI.SetActive(false);
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
    }

    public void CheckPredict()
    {
        if (localPlayer.animalType != AnimalType.Crow || isSelected || selectType != AnimalType.None) return;

        var type = GetRandomAnimalType();
        localPlayer.CmdSetPredict(type);       
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
    }
    public void DeActiveDisguiseUI()
    {
        OnDisguiseSelected(selectType);
        checkUI.SetActive(false);
    }

    public void DeActivePredictCheckUI()
    {
        predictCheckUI.SetActive(false);
        selectType = AnimalType.None;
    }
    public void DeActivePredictUI()
    {
        OnPredictSelected(selectType);
        predictCheckUI.SetActive(false);
    }

    public void OnClickMissionListUI()
    {
        missionListUI.SetActive(!missionListUI.activeSelf);
    }
    public void OnClickProfileSlotUI()
    {
        playerSlotUI.SetActive(!playerSlotUI.activeSelf);
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
    public static void RegisterInputField()
    {
        inputFields = FindObjectsOfType<TMP_InputField>();
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
