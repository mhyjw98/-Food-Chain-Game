using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkCoordinator : MonoBehaviour
{
    public static NetworkCoordinator Instance;

    private bool startHost = false;
    private bool startClient = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RequestStartHost()
    {
        startHost = true;
        SceneManager.LoadScene("GameRoom");
    }

    public void RequestStartClient()
    {
        startClient = true;
        SceneManager.LoadScene("GameRoom");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "GameRoom") return;

        if (startHost)
        {
            startHost = false;
            Debug.Log("[Coordinator] GameRoom ·ÎµåµÊ ¡æ StartHost()");
            RoomManager.singleton.StartHost();
        }

        if (startClient)
        {
            startClient = false;
            Debug.Log("[Coordinator] GameRoom ·ÎµåµÊ ¡æ StartClient()");
            RoomManager.singleton.StartClient();
        }
    }
}
