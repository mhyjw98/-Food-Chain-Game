using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class GameRoomUI : MonoBehaviour
{
    public static GameRoomUI Instance;

    public TMP_InputField chatInputField;

    public TextMeshProUGUI roomCodeText;
    public TextMeshProUGUI noticeText;
    public GameObject startGameButton;
    public GameObject exitRoomButton;
    public GameObject codeGroup;
    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        roomCodeText.text = RoomSessionData.CurrentRoomCode;
        noticeText.text = "";

        StartCoroutine(CheckHostStatus());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrWhiteSpace(chatInputField.text))
        {
            string msg = chatInputField.text.Trim();

            if (NetworkClient.connection != null && NetworkClient.connection.identity != null)
            {
                RoomPlayer player = NetworkClient.connection.identity.GetComponent<RoomPlayer>();
                player.CmdSendChatMessage(msg);
            }

            chatInputField.text = "";
            chatInputField.ActivateInputField();
        }
    }
    private IEnumerator CheckHostStatus()
    {
        yield return new WaitForSeconds(0.01f);
        Debug.Log("RoomPlayer의 isHost 체크 로직 실행");
        foreach (var player in FindObjectsOfType<RoomPlayer>())
        {
            if (player.isLocalPlayer)
            {
                startGameButton.SetActive(player.isHost);
                codeGroup.SetActive(player.isHost);
                break;
            }
        }
    }

    public void OnStartGameClicked()
    {
        var roomPlayer = NetworkClient.connection.identity.GetComponent<RoomPlayer>();
        if (roomPlayer != null)
        {
            roomPlayer.CmdChangeReadyState(true);
        }
    }

    public void OnClickExit()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            RoomManager.Instance.StopClient();
            RoomManager.Instance.StopHost();
        }
        else if (NetworkClient.isConnected)
        {            
            RoomManager.Instance.StopClient();
        }
    }

    public void ShowStartGameButton(bool isHost)
    {
        startGameButton.SetActive(isHost);
    }

    public void ShowCodeGroup(bool isHost)
    {
        codeGroup.SetActive(isHost);
    }

    public void CopyRoomCode()
    {
        if (!string.IsNullOrEmpty(roomCodeText.text))
        {
            GUIUtility.systemCopyBuffer = roomCodeText.text;
            StartCoroutine(NoticeCopyCode());
        }
    }

    IEnumerator NoticeCopyCode()
    {
        noticeText.text = "Copy completed.";

        yield return new WaitForSeconds(2);

        noticeText.text = "";
    }
}
