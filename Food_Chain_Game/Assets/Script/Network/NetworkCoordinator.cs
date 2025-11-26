using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkCoordinator : MonoBehaviour
{
    public static NetworkCoordinator Instance;

    [SerializeField] private RoomManager roomManagerPrefab;

    private bool startHost = false;
    private bool startClient = false;

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "Start")
            SceneManager.LoadScene("Title");
    }
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
        if (RoomManager.singleton == null)
        {
            var rm = Instantiate(roomManagerPrefab);
        }
        if (scene.name == "GameRoom")
        {           
            if (startHost)
            {
                startHost = false;
                Debug.Log("[Coordinator] GameRoom ·ÎµåµÊ ¡æ StartHost()");
                StartCoroutine(StartHostNextFrame());
            }

            if (startClient)
            {
                startClient = false;
                Debug.Log("[Coordinator] GameRoom ·ÎµåµÊ ¡æ StartClient()");
                StartCoroutine(StartClientNextFrame());
            }
        }       
    }

    private IEnumerator StartHostNextFrame()
    {
        yield return null;
        RoomManager.singleton.StartHost();
    }

    private IEnumerator StartClientNextFrame()
    {
        yield return null;
        RoomManager.singleton.StartClient();
    }
}
