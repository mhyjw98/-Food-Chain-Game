using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CharacterData;

public class GamePlayUI : MonoBehaviour
{
    public static GamePlayUI Instance;

    

    public GameObject StartUI;
    public GameObject chatUI;
    public GameObject resultUI;
    public GameObject disguiseUI;
    public GameObject checkUI;
    public GameObject block;
    public GameObject skyBlock;
    public TMP_InputField chatInputField;
    public GameObject characterPanel;
    public TextMeshProUGUI characterText;
    public TextMeshProUGUI territoryText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI timerDisplay;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI checkText;

    [Header("Zone Panels")]
    public Transform fieldPanel;
    public Transform forestPanel;
    public Transform riverPanel;
    public Transform skyPanel;

    [Header("Prefab")]
    public GameObject disguiseButtonPrefab;
    public Transform disguiseButtonGroup;
    public GameObject playerIconPrefab;

    Animator textAni;
    Animator panelAni;
    Animator territoryAni;
    Animator descriptionAni;
    Animator resultAni;

    private Dictionary<GamePlayer, GameObject> iconMap = new();
    private float uiTimer = 10f;
    private bool isSelected = false;
    private GamePlayer localPlayer;
    private AnimalType selectType;
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
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrWhiteSpace(chatInputField.text))
        {
            string msg = chatInputField.text.Trim();

            if (NetworkClient.connection != null && NetworkClient.connection.identity != null)
            {
                GamePlayer player = NetworkClient.connection.identity.GetComponent<GamePlayer>();
                ChatManager chatManager = FindObjectOfType<ChatManager>();
                
                if (chatManager.currentChannel.type == ChatChannelType.Whisper && chatManager.currentChannel.targetPlayer != null)
                {
                    player.CmdSendWhisper(chatManager.currentChannel.targetPlayer.netId, msg);
                }
                else
                {
                    player.CmdSendChatMessage(msg);
                }               
            }

            chatInputField.text = "";
            chatInputField.ActivateInputField();
        }
    }

    public IEnumerator ShowCharacter(CharacterType type)
    {
        PlayerMove.isEvent = true;
        StartUI.SetActive(true);
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

        chatUI.SetActive(true);
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
        if (localPlayer.animalType == AnimalType.Chameleon)
        {
            disguiseUI.SetActive(true);
            isSelected = false;
            StartCoroutine(Countdown());
        }     
    }

    IEnumerator Countdown()
    {
        yield return new WaitForSeconds(uiTimer);
        if (!isSelected)
        {
            var randomType = GetRandomAnimalType(); // 구현 필요
            localPlayer.CmdSetDisguise(randomType);

            string updateText = $"카멜레온 > {AnimalNameMap.AnimalTypeToName[randomType]}";
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

        string updateText = $"카멜레온 > {AnimalNameMap.AnimalTypeToName[type]}";
        PlayerSlot slot = PlayerSlotUI.Instance.GetSlotByPlayer(localPlayer);
        slot.UpdateNicknameWithAnimal(updateText);
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
            GameObject btn = Instantiate(disguiseButtonPrefab, disguiseButtonGroup);
            string label = AnimalNameMap.AnimalTypeToName[type];
            btn.GetComponentInChildren<TextMeshProUGUI>().text = label;

            // 버튼 클릭 시 위장 적용
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectType = type;
                checkText.text = $"정말로 {label}(으)로 \n위장하시겠습니까??";
                checkUI.SetActive(true);
            });
        }
        disguiseUI.SetActive(false);
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
        int index = Random.Range(0, allAnimals.Length);
        return allAnimals[index];
    }

    AnimalType[] allAnimals = new AnimalType[]
        {
            AnimalType.Lion,
            AnimalType.Crocodile,
            AnimalType.Mouse,
            AnimalType.Snake,
            AnimalType.Eagle,
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
