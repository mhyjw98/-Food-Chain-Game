using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    public GameObject colorUI;    

    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        roomCodeText.text = RoomSessionData.CurrentRoomCode;
        noticeText.text = "";
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
        }
    }

    
    public void CheckHostStatus(RoomPlayer player)
    {
        Debug.Log("RoomPlayer의 isHost 체크 로직 실행");

        if (player.isLocalPlayer)
        {
            startGameButton.SetActive(player.isHost);
            codeGroup.SetActive(player.isHost);
        }
        
    }

    public void OnStartGameClicked()
    {
        ((RoomManager)RoomManager.singleton).StartGame();       
    }

    public void ShowStartGameButton(bool isHost)
    {
        startGameButton.SetActive(isHost);
    }

    public void ShowCodeGroup(bool isHost)
    {
        codeGroup.SetActive(isHost);
    }

    public void OnClickColorUI()
    {
        colorUI.SetActive(!colorUI.activeSelf);
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
