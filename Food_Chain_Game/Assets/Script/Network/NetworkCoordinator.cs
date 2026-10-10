using System.Collections;
using Mirror;
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
                Debug.Log("[Coordinator] GameRoom 로드됨 → StartHost()");
                StartCoroutine(StartHostNextFrame());
            }

            if (startClient)
            {
                startClient = false;
                Debug.Log("[Coordinator] GameRoom 로드됨 → StartClient()");
                StartCoroutine(StartClientNextFrame());
            }
        }       
    }

    private const int MaxStartAttempts = 5;
    private const float StartRetryDelaySeconds = 0.5f;

    private IEnumerator StartHostNextFrame()
    {
        yield return null;
        yield return EnsurePreviousSessionStopped();
        yield return StartWithRetry(() => RoomManager.singleton.StartHost(), "StartHost");
    }

    private IEnumerator StartClientNextFrame()
    {
        yield return null;
        yield return EnsurePreviousSessionStopped();
        yield return StartWithRetry(() => RoomManager.singleton.StartClient(), "StartClient");
    }

    // 이전 세션(같은 프로세스 안에서 정상적으로 종료되지 않은 호스트/클라이언트)이
    // 아직 살아있으면 트랜스포트 소켓이 포트를 계속 점유하고 있어, 이후 StartHost()가
    // "주소가 이미 사용 중" 소켓 예외로 실패할 수 있다. 새 세션을 시작하기 전에
    // 강제로 정리해서 이 문제를 방지한다.
    private IEnumerator EnsurePreviousSessionStopped()
    {
        if (!NetworkServer.active && !NetworkClient.active)
            yield break;

        Debug.LogWarning("[Coordinator] 이전 네트워크 세션이 아직 활성 상태라 먼저 정리합니다.");
        RoomManager.singleton.StopHost();

        // 트랜스포트가 소켓을 실제로 해제할 시간을 한 프레임 준다.
        yield return null;
    }

    // NetworkServer.active/NetworkClient.active가 이미 false여도, OS 소켓이
    // 실제로는 아직 해제되지 않아 바인드가 실패하는 경우가 있음(특히 Windows에서
    // 직전 세션 종료 직후). Mirror의 상태 플래그로는 이 케이스를 막을 수 없어서,
    // 짧은 지연을 두고 몇 차례 재시도한다.
    private IEnumerator StartWithRetry(System.Action startAction, string label)
    {
        for (int attempt = 1; attempt <= MaxStartAttempts; attempt++)
        {
            System.Exception caught = null;
            try
            {
                startAction();
            }
            catch (System.Exception ex)
            {
                caught = ex;
            }

            if (caught == null)
                yield break;

            Debug.LogWarning($"[Coordinator] {label}() 실패 (시도 {attempt}/{MaxStartAttempts}): {caught.Message}");
            yield return new WaitForSeconds(StartRetryDelaySeconds);
        }

        string message = $"네트워크 연결에 실패했습니다. 잠시 후 다시 시도해주세요. ({label})";
        Debug.LogError($"[Coordinator] {label}()가 {MaxStartAttempts}회 반복 실패했습니다.");
        NetworkErrorManager.Instance?.SetError(NetworkErrorReason.Unknown, message);

        if (SceneManager.GetActiveScene().name != "Title")
            SceneManager.LoadScene("Title");
    }
}
