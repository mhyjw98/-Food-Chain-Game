using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nicknameDisplay;
    [SerializeField] private TextMeshProUGUI noticeText;
    [SerializeField] private GameObject nicknamePopup;
    [SerializeField] private GameObject quitPopup;
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancleButton;
    [SerializeField] private Button changeButton;
    [SerializeField] private GameObject CreateRoomUI;    
    [SerializeField] private RoomHost roomHost;
    [SerializeField] private RoomJoin roomJoin;
    [SerializeField] private InputField codeInputField;

    [SerializeField] private GameObject errorUI;
    [SerializeField] private TextMeshProUGUI errorText;

    public GameObject joinUI;

    public Button joinButton;
    public Button hostButton;
    public Button clientButton;
    public Button settingButton;

    public TMP_Dropdown playerCountDropdown;
    public static int SelectedPlayerCount = 1;
    public int maxPlayer = 0;

    [Header("Nickname / Room")]
    [SerializeField] private int maxNicknameLength = 8;
    [SerializeField] private int maxSelectablePlayerCount = 13;

    // 방 생성/입장 버튼 연타로 씬이 두 번 로드되는 것을 막는다
    private bool _enteringRoom;

    public GameObject activeUi;

    private void Awake()
    {
        roomHost = FindFirstObjectByType<RoomHost>();
        roomJoin = FindFirstObjectByType<RoomJoin>();
    }
    void Start()
    {
        PlayerColorPalette.ResetUsedColors();

        NicknameCheck();
        UISetting();
        ErrorCheck();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (SettingManager.isKeySetting == true) return;

            // 설정 창이 열려 있거나 방금 이 ESC로 닫혔으면 설정 창만 처리한다(종료 팝업까지 뜨지 않게)
            if (SettingManager.IsOpen || SettingManager.ClosedThisFrame) return;

            if (activeUi == null)
                OnQuitBtnClicked();
            else
            {               
                activeUi.SetActive(false);
                activeUi = null;
            }
        }
    }
    void ErrorCheck()
    {
        if (NetworkErrorManager.Instance != null && NetworkErrorManager.Instance.HasError)
        {
            ShowError(NetworkErrorManager.Instance.CurrentMessage);
            NetworkErrorManager.Instance.Clear();
        }
    }
    void NicknameCheck()
    {
        if (!NickNamemanager.HasNickname())
        {
            FirstShowPopup();
        }
        else
        {
            nicknameDisplay.text = $"닉네임: {NickNamemanager.GetNickname()}";
            changeButton.gameObject.SetActive(true);
        }
    }
    void UISetting()
    {
        playerCountDropdown.onValueChanged.AddListener(OnPlayerCountChanged);

        playerCountDropdown.ClearOptions();
        List<string> options = new();
        for (int i = 1; i <= maxSelectablePlayerCount; i++)
        {
            options.Add(i.ToString());
        }
        playerCountDropdown.AddOptions(options);

        // SelectedPlayerCount는 static이라 타이틀로 돌아와도 남는다. 드롭다운 표시와 maxPlayer를 거기에 맞춘다
        // (맞추지 않으면 드롭다운은 1, 실제 정원은 이전 값, 방 코드 서버에 등록되는 정원은 0이 된다)
        SelectedPlayerCount = Mathf.Clamp(SelectedPlayerCount, 1, maxSelectablePlayerCount);
        maxPlayer = SelectedPlayerCount;
        playerCountDropdown.SetValueWithoutNotify(SelectedPlayerCount - 1);
        playerCountDropdown.RefreshShownValue();
    }
    public void ShowError(string msg)
    {
        if (errorUI == null) return;

        errorUI.SetActive(true);
        activeUi = errorUI;

        if (errorText != null)
            errorText.text = msg;
    }

    void FirstShowPopup()
    {
        nicknamePopup.SetActive(true);
        activeUi = nicknamePopup;
    }
    public void ChangeShowPopup()
    {
        nicknamePopup.SetActive(true);
        nicknameInput.text = NickNamemanager.GetNickname();

        cancleButton.gameObject.SetActive(true);
        activeUi = nicknamePopup;
    }

    public void HidePopup()
    {
        nicknamePopup.SetActive(false);
        activeUi = null;
    }

    public void SaveNickname()
    {
        string nickname = nicknameInput.text.Trim();
        if (!string.IsNullOrEmpty(nickname))
        {
            if(nickname.Length > maxNicknameLength)
            {
                noticeText.text = $"닉네임은 {maxNicknameLength}글자 이하로 지어주세요.";
                return;
            }
                
            NickNamemanager.SetNickname(nickname);
            nicknameDisplay.text = $"닉네임: {nickname}";
            nicknamePopup.SetActive(false);
            changeButton.gameObject.SetActive(true);
            activeUi = null;
        }
        else
        {
            noticeText.text = "닉네임을 작성해주세요.";
        }
    }

    public void OnQuitBtnClicked()
    {
        quitPopup.SetActive(true);
        activeUi = quitPopup;
    }

    public void CacleQuitPopup()
    {
        quitPopup.SetActive(false);
        activeUi = null;
    }
    public void ShowCreateRoomUI()
    {
        CreateRoomUI.SetActive(true);
        activeUi = CreateRoomUI;
    }

    public void HideCreateRoomUI()
    {
        CreateRoomUI.SetActive(false);
        activeUi = null;
    }
    public void StartHost()
    {
        if (_enteringRoom) return;

        // 닉네임 팝업은 ESC로 닫을 수 있으므로 방에 들어가기 직전에 다시 확인한다
        if (!NickNamemanager.HasNickname())
        {
            FirstShowPopup();
            return;
        }

        var roomManager = RoomManager.singleton as RoomManager;
        if (roomManager == null || roomHost == null || NetworkCoordinator.Instance == null)
        {
            Debug.LogError("[TitleUI] RoomManager/RoomHost/NetworkCoordinator가 준비되지 않아 방을 만들 수 없습니다.");
            return;
        }

        _enteringRoom = true;

        Debug.Log("호스트 시작");
        maxPlayer = SelectedPlayerCount;
        roomManager.maxPlayerCount = SelectedPlayerCount;

        // Node 서버에 방이 등록된 것을 확인한 뒤에 호스트를 시작한다.
        // 등록에 실패한 채로 방을 열면 코드는 표시되지만 그 코드로는 아무도 들어올 수 없다
        roomHost.RegisterNewRoom(maxPlayer, (success, errorMessage) =>
        {
            // 응답을 기다리는 사이에 타이틀 씬이 바뀌었으면 아무것도 하지 않는다
            if (this == null) return;

            if (!success)
            {
                _enteringRoom = false;
                ShowError(errorMessage);
                return;
            }

            Debug.Log($"[방 생성 완료] 방 코드: {RoomSessionData.CurrentRoomCode}");
            RoomSessionData.ColorIndex = Random.Range(0, PlayerColorPalette.Count);

            if (NetworkCoordinator.Instance != null)
                NetworkCoordinator.Instance.RequestStartHost();
        });
    }

    public void StartClient()
    {
        ActiveJoinUI();
    }

    void OnPlayerCountChanged(int index)
    {
        if (int.TryParse(playerCountDropdown.options[index].text, out int result))
        {
            SelectedPlayerCount = result;
            maxPlayer = result;
            Debug.Log($"선택된 인원 수: {SelectedPlayerCount}");
        }
        else
        {
            Debug.LogWarning("드롭다운 값 파싱 실패");
        }
    }

    public void JoinByCode()
    {
        if (_enteringRoom) return;

        if (!NickNamemanager.HasNickname())
        {
            FirstShowPopup();
            return;
        }

        string code = codeInputField.text;
        if (string.IsNullOrEmpty(code))
        {
            Debug.Log("입력창이 빈 값");
            return;
        }

        if (roomJoin == null || NetworkCoordinator.Instance == null) return;

        _enteringRoom = true;

        roomJoin.CheckRoomBeforeJoin(code, (success, errorMessage) =>
        {
            // 응답을 기다리는 사이에 타이틀 씬이 바뀌었으면 아무것도 하지 않는다
            if (this == null) return;

            if (success)
            {
                // 씬을 넘기기 전에 색을 먼저 정한다(RoomPlayer가 생성되면서 이 값을 읽는다)
                RoomSessionData.ColorIndex = PlayerColorPalette.GetRandomExcludingIndex();
                NetworkCoordinator.Instance.RequestStartClient();
            }
            else
            {
                _enteringRoom = false;
                ShowError(errorMessage);
            }
        });
    }

    public void DeActiveErrorNotice()
    {
        errorUI.SetActive(false);
        activeUi = null;
    }

    private void ActiveJoinUI()
    {
        joinUI.SetActive(true);
        activeUi = joinUI;
    }

    public void SetDeactivateJoinUI()
    {
        joinUI.SetActive(false);
        activeUi = null;
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
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    } 
}
