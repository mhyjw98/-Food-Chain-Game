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
            Debug.Log("���� ���� ��ư Ŭ��");
            //roomManager.StartGame();
        }
        else
        {
            Debug.LogError("RoomManager �ν��Ͻ��� ã�� �� �����ϴ�!");
        }
    }
}
