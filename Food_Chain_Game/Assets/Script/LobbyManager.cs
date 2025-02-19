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
}
