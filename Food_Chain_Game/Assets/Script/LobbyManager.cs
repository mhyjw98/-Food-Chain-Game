using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public Button hostButton;
    public Button joinButton;
    public InputField ipInputField;

    void Start()
    {
        hostButton.onClick.AddListener(StartHost);
        joinButton.onClick.AddListener(StartClient);
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
}
