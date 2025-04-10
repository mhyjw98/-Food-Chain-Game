using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private RoomHost roomHost;
    [SerializeField] private RoomJoin roomJoin;
    [SerializeField] private InputField codeInputField;

    public GameObject joinUI;

    public Button joinButton;
    public Button hostButton;
    public Button clientButton;

    public void StartHost()
    {
        Debug.Log("ȣ��Ʈ ����");
        RoomManager.singleton.StartHost();


        // �� ����� �Լ� ���ο��� ���
        string hostIp = NetworkUtils.GetLocalIPAddress(); // ���� IP �ڵ� ȹ��
        string roomCode = CodeManager.Instance.RegisterRoom(hostIp);

        Debug.Log($"[�� ���� �Ϸ�] �� IP: {hostIp}, �� �ڵ�: {roomCode}");
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
            Debug.Log("�Է�â�� �� ��");
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

    public void PasteCodeToInput()
    {
        string copied = GUIUtility.systemCopyBuffer;
        if (!string.IsNullOrEmpty(copied))
        {
            codeInputField.text = copied;
        }
    }

    public void ClearCodeInput()
    {
        codeInputField.text = "";
    }
}
