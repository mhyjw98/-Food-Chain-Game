using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    [SerializeField] private RoomHost roomHost;
    [SerializeField] private RoomJoin roomJoin;
    [SerializeField] private InputField codeInputField;

    public GameObject joinUI;

    public Button joinButton;
    public Button hostButton;
    public Button clientButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        joinButton.onClick.AddListener(JoinByCode);
        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);
    }

    public void StartHost()
    {
        Debug.Log("호스트 시작");
        RoomManager.singleton.StartHost();


        // 방 만들기 함수 내부에서 사용
        string hostIp = NetworkUtils.GetLocalIPAddress(); // 로컬 IP 자동 획득
        string roomCode = CodeManager.Instance.RegisterRoom(hostIp);

        Debug.Log($"[방 생성 완료] 내 IP: {hostIp}, 방 코드: {roomCode}");
        RoomSessionData.CurrentRoomCode = roomCode;

        roomHost.RegisterRoom(roomCode, hostIp);
    }

    public void StartClient()
    {
        ActiveJoinUI();
    }

    public void JoinByCode()
    {
        string code = codeInputField.text;
        if (string.IsNullOrEmpty(code))
        {
            Debug.Log("입력창이 빈 값");
            return;
        }

        roomJoin.JoinRoom(code);
    }

    private void ActiveJoinUI()
    {
        joinUI.SetActive(true);
    }

    public void SetDeactivateJoinUI()
    {
        joinUI.SetActive(false);
    }
}
