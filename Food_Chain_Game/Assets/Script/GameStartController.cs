using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameStartController : MonoBehaviour
{
    public Button startGameButton;
    void Start()
    {
        if (NetworkServer.active)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
            startGameButton.gameObject.SetActive(true);
        }
        else
        {
            startGameButton.gameObject.SetActive(false);
        }
    }

    void OnStartGameClicked()
    {
        RoomManager roomManager = NetworkManager.singleton as RoomManager;
        if (roomManager != null)
        {
            Debug.Log("게임 시작 버튼 클릭");
            //roomManager.StartGame();
        }
        else
        {
            Debug.LogError("RoomManager 인스턴스를 찾을 수 없습니다!");
        }
    }
}
