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
        Debug.Log("호스트 시작");
        NetworkManager.singleton.StartHost();
    }

    public void StartClient()
    {
        if (string.IsNullOrEmpty(ipInputField.text))
        {
            Debug.LogError("서버 주소를 입력해야 합니다!");
            return;
        }

        NetworkManager.singleton.networkAddress = ipInputField.text;
        Debug.Log($"클라이언트 접속 시도: {ipInputField.text}");
        NetworkManager.singleton.StartClient();
    }
    public void ShowStartGameButton()
    {
        startGameButton.gameObject.SetActive(true); // 방장일 때만 버튼 활성화
    }

    void OnStartGameClicked()
    {
        if (NetworkServer.active) // 서버(호스트)만 실행 가능
        {
            RoomManager roomManager = NetworkManager.singleton as RoomManager;
            if (roomManager != null)
            {
                roomManager.StartGame();
            }
            else
            {
                Debug.LogError("RoomManager 인스턴스를 찾을 수 없습니다!");
            }
        }
    }
    public void UpdateCharacterDisplay(string characterName)
    {
        if (characterText != null)
        {
            characterText.text = $"내 캐릭터: {characterName}";
        }
    }
}
