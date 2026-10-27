using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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

    [Header("Connecting Overlay")]
    [Tooltip("접속이 끝나기 전까지 로비를 가리는 \"연결 중\" 화면")]
    [SerializeField] private GameObject connectingOverlay;
    [SerializeField] private TextMeshProUGUI connectingText;
    [SerializeField] private string connectingMessage = "연결 중";
    [Tooltip("문구 뒤의 점(...)이 하나씩 늘어나는 간격(초)")]
    [SerializeField] private float connectingDotInterval = 0.4f;
    [SerializeField] private int connectingMaxDots = 3;
    [Tooltip("내 플레이어가 생긴 뒤 닉네임·색 적용을 기다리는 최대 시간(초). 넘으면 준비가 덜 됐어도 로비를 보여 준다(빈 색이 없을 때 등)")]
    [SerializeField] private float connectingSetupTimeout = 5f;

    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        roomCodeText.text = RoomSessionData.CurrentRoomCode;
        noticeText.text = "";

        StartCoroutine(CoRefreshHostUI());
        StartCoroutine(CoShowConnectingOverlay());
    }

    // 방을 만들거나 코드로 입장하면 접속이 끝나기 전에 로비 씬이 먼저 열린다(입장자는 접속 직후 서버 지시로 로비 씬을 한 번 더 불러온다).
    // 내 플레이어가 스폰 위치에 생기고 서버가 닉네임·색을 정해 줄 때까지 "연결 중" 화면으로 가린 뒤 로비를 보여 준다.
    // 접속에 실패하면 RoomManager가 타이틀 씬을 다시 불러오므로 이 화면도 함께 사라진다
    private IEnumerator CoShowConnectingOverlay()
    {
        var roomManager = RoomManager.singleton as RoomManager;

        // 게임이 끝나고 로비로 돌아온 경우에는 이미 준비가 끝난 상태이므로 띄우지 않는다
        if (roomManager != null && roomManager.ClientLobbyReady)
        {
            if (connectingOverlay != null) connectingOverlay.SetActive(false);
            yield break;
        }

        if (connectingOverlay != null) connectingOverlay.SetActive(true);

        float interval = Mathf.Max(0.05f, connectingDotInterval);
        float elapsed = 0f;
        int dots = -1;

        _setupElapsed = 0f;
        while (!IsLobbySetupDone())
        {
            int nextDots = (int)(elapsed / interval) % (Mathf.Max(0, connectingMaxDots) + 1);
            if (nextDots != dots && connectingText != null)
            {
                dots = nextDots;
                connectingText.text = connectingMessage + new string('.', dots);
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (roomManager != null) roomManager.ClientLobbyReady = true;
        if (connectingOverlay != null) connectingOverlay.SetActive(false);
    }

    private float _setupElapsed;

    // 접속 → 내 RoomPlayer 생성 → 닉네임·색 적용 순서로 끝났는지. 플레이어가 생긴 뒤 너무 오래 걸리면 그대로 진행한다
    private bool IsLobbySetupDone()
    {
        if (!NetworkClient.isConnected || NetworkClient.localPlayer == null) return false;

        var roomPlayer = NetworkClient.localPlayer.GetComponent<RoomPlayer>();
        if (roomPlayer == null) return false;
        if (roomPlayer.IsLocalSetupComplete) return true;

        _setupElapsed += Time.unscaledDeltaTime;
        if (_setupElapsed < connectingSetupTimeout) return false;

        Debug.LogWarning("[GameRoomUI] 닉네임·색 적용을 기다리다 시간이 지나 로비를 그대로 보여 줍니다.");
        return true;
    }

    // 로비 씬이 열릴 때마다(최초 입장, 게임 후 복귀 모두) 방장 전용 UI를 스스로 갱신한다.
    // 기존에는 최초 입장 때 한 번만 켜져서, 게임 후 돌아오면 시작 버튼이 나타나지 않았다
    private IEnumerator CoRefreshHostUI()
    {
        while (!NetworkClient.active || NetworkClient.localPlayer == null)
            yield return null;

        // 이 구조에서 방장은 항상 서버를 겸하는 호스트다
        bool isRoomHost = NetworkServer.active;
        startGameButton.SetActive(isRoomHost);
        codeGroup.SetActive(isRoomHost);
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
        var roomManager = RoomManager.singleton as RoomManager;
        if (roomManager == null) return;

        if (!roomManager.TryStartGame(out string failReason) && !string.IsNullOrEmpty(failReason))
            ShowNotice(failReason);
    }

    // 로비의 "방 나가기" 버튼용. 먼저 확인창을 띄우고(방장 여부 무관), 확인하면 방을 나간다.
    // 방장이 나가면 남은 사람들에게 "방장이 방을 닫았습니다"가 표시된다
    public void OnExitRoomClicked()
    {
        if (ReturnToTitleConfirmUI.TryShow(LeaveRoom)) return;

        LeaveRoom();
    }

    private void LeaveRoom()
    {
        var roomManager = RoomManager.singleton as RoomManager;
        if (roomManager != null)
            roomManager.LeaveRoom();
    }

    private Coroutine _noticeCo;
    [SerializeField] private float noticeSeconds = 2f;
    [SerializeField] private string copyCompletedMessage = "Copy completed.";

    private void ShowNotice(string message)
    {
        if (_noticeCo != null) StopCoroutine(_noticeCo);
        _noticeCo = StartCoroutine(CoShowNotice(message));
    }

    private IEnumerator CoShowNotice(string message)
    {
        noticeText.text = message;

        yield return new WaitForSeconds(noticeSeconds);

        noticeText.text = "";
        _noticeCo = null;
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
        if (!colorUI.activeSelf)
        {
            colorUI.SetActive(true);
            UIManager.Instance.Push(UIPriority.Modal);
            UIManager.RegisterEscape(colorUI, CloseColorUI);
        }
        else
        {
            CloseColorUI();
        }
    }

    // 커스터마이징(의상·색상) 창 닫기. ESC로도 닫힌다
    public void CloseColorUI()
    {
        if (!colorUI.activeSelf) return;

        colorUI.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Modal);
        UIManager.UnregisterEscape(colorUI);
    }
    public void CopyRoomCode()
    {
        if (!string.IsNullOrEmpty(roomCodeText.text))
        {
            GUIUtility.systemCopyBuffer = roomCodeText.text;
            // 시작 실패 안내와 같은 문구 칸을 쓰므로 같은 경로로 표시한다(따로 돌리면 서로의 문구를 일찍 지운다)
            ShowNotice(copyCompletedMessage);
        }
    }

    
}
