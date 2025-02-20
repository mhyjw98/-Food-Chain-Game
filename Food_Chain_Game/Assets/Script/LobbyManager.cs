using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;
    public Button startGameButton;

    public Button hostButton;
    public Button joinButton;
    public InputField ipInputField;

    public Text characterText;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }        
    }

    void Start()
    {
        hostButton.onClick.AddListener(StartHost);
        joinButton.onClick.AddListener(StartClient);
        startGameButton.onClick.AddListener(OnStartGameClicked);
        startGameButton.gameObject.SetActive(false);
    }

    public void StartHost()
    {
        Debug.Log("ȣ��Ʈ ����");
        NetworkManager.singleton.StartHost();
    }

    public void StartClient()
    {
        if (string.IsNullOrEmpty(ipInputField.text))
        {
            Debug.LogError("���� �ּҸ� �Է��ؾ� �մϴ�!");
            return;
        }

        NetworkManager.singleton.networkAddress = ipInputField.text;
        Debug.Log($"Ŭ���̾�Ʈ ���� �õ�: {ipInputField.text}");
        NetworkManager.singleton.StartClient();
    }
    public void ShowStartGameButton()
    {
        startGameButton.gameObject.SetActive(true); // ������ ���� ��ư Ȱ��ȭ
    }

    void OnStartGameClicked()
    {
        if (NetworkServer.active) // ����(ȣ��Ʈ)�� ���� ����
        {
            RoomManager roomManager = NetworkManager.singleton as RoomManager;
            if (roomManager != null)
            {
                roomManager.StartGame();
            }
            else
            {
                Debug.LogError("RoomManager �ν��Ͻ��� ã�� �� �����ϴ�!");
            }
        }
    }
    public void UpdateCharacterDisplay(string characterName)
    {
        if (characterText != null)
        {
            characterText.text = $"�� ĳ����: {characterName}";
        }
    }
}
