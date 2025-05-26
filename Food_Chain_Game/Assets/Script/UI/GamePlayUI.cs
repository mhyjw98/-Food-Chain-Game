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
        territoryText.text = $"������: {TranslateTerritory(info.HomeTerritory)}";
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
            roundText.text = "���� �ð�";
            timerDisplay.text = "���� �������";
        }
        else if (time == RoundTime.Exploration)
        {
            roundText.text = "Ž�� �ð�";
            timerDisplay.text = "Ž�� �������";
        }           
        else if (time == RoundTime.End)
        {
            roundText.text = "���� ����";
            timerDisplay.text = "";
        }
        else
        {
            roundText.text = $"{round}���� {GetTimeName(time)}";
            timerDisplay.text = $"{GetTimeName(time)} �������";
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
            var randomType = GetRandomAnimalType(); // ���� �ʿ�
            localPlayer.CmdSetDisguise(randomType);

            string updateText = $"ī�᷹�� > {AnimalNameMap.AnimalTypeToName[randomType]}";
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

        string updateText = $"ī�᷹�� > {AnimalNameMap.AnimalTypeToName[type]}";
        PlayerSlot slot = PlayerSlotUI.Instance.GetSlotByPlayer(localPlayer);
        slot.UpdateNicknameWithAnimal(updateText);
    }

    private string TranslateTerritory(TerritoryType type)
    {
        return type switch
        {
            TerritoryType.Sky => "�ϴ�",
            TerritoryType.River => "��",
            TerritoryType.Field => "��",
            TerritoryType.Forest => "��",
            _ => "???"
        };
    }
    private string GetTimeName(RoundTime time)
    {
        return time == RoundTime.Day ? "��" : "��";
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
            resultText.text = "�� ��";
        else
            resultText.text = "�� ��";

        chatUI.SetActive(false);
        resultUI.SetActive(true);
        resultAni.SetTrigger("ShowResult");
    }

    public void SetDisguiseOptions()
    {
        // ���� ��ư ����
        foreach (Transform child in disguiseButtonGroup)
            Destroy(child.gameObject);

        foreach (var type in allAnimals)
        {
            GameObject btn = Instantiate(disguiseButtonPrefab, disguiseButtonGroup);
            string label = AnimalNameMap.AnimalTypeToName[type];
            btn.GetComponentInChildren<TextMeshProUGUI>().text = label;

            // ��ư Ŭ�� �� ���� ����
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectType = type;
                checkText.text = $"������ {label}(��)�� \n�����Ͻðڽ��ϱ�??";
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
