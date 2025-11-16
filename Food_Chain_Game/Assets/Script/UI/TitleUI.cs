using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

    public TMP_Dropdown playerCountDropdown;
    public static int SelectedPlayerCount = 1;
    public int maxPlayer = 0;

    void Start()
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

        playerCountDropdown.onValueChanged.AddListener(OnPlayerCountChanged);

        playerCountDropdown.ClearOptions();
        List<string> options = new();
        for (int i = 1; i <= 13; i++)
        {
            options.Add(i.ToString());
        }
        playerCountDropdown.AddOptions(options);
    }

    void FirstShowPopup()
    {
        nicknamePopup.SetActive(true);
    }
    public void ChangeShowPopup()
    {
        nicknamePopup.SetActive(true);
        nicknameInput.text = NickNamemanager.GetNickname();

        cancleButton.gameObject.SetActive(true);
    }

    public void HidePopup()
    {
        nicknamePopup.SetActive(false);
    }

    public void SaveNickname()
    {
        string nickname = nicknameInput.text.Trim();
        if (!string.IsNullOrEmpty(nickname))
        {
            if(nickname.Length > 8)
            {
                noticeText.text = "닉네임은 8글자 이하로 지어주세요.";
                return;
            }
                
            NickNamemanager.SetNickname(nickname);
            nicknameDisplay.text = $"닉네임: {nickname}";
            nicknamePopup.SetActive(false);
        }
        else
        {
            noticeText.text = "닉네임을 작성해주세요.";
        }
    }

    public void OnQuitBtnClicked()
    {
        quitPopup.SetActive(true);
    }

    public void CacleQuitPopup()
    {
        quitPopup.SetActive(false);
    }
    public void ShowCreateRoomUI()
    {
        CreateRoomUI.SetActive(true);
    }

    public void HideCreateRoomUI()
    {
        CreateRoomUI.SetActive(false);
    }
    public void StartHost()
    {
        Debug.Log("호스트 시작");
        RoomManager.Instance.maxPlayerCount = SelectedPlayerCount;
        RoomManager.singleton.StartHost();

        string hostIp = ConfigManager.Config.IP;
        string roomCode = CodeManager.Instance.RegisterRoom(hostIp);

        Debug.Log($"[방 생성 완료] 내 IP: {hostIp}, 방 코드: {roomCode}");
        RoomSessionData.CurrentRoomCode = roomCode;

        roomHost.RegisterRoom(roomCode, hostIp, maxPlayer);
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
        string code = codeInputField.text;
        if (string.IsNullOrEmpty(code))
        {
            Debug.Log("입력창이 빈 값");
            return;
        }

        roomJoin.CheckRoomBeforeJoin(code, (success, errorMessage) =>
        {
            if (success)
            {
                RoomManager.singleton.StartClient();
            }
            else
            {
                ActiveErrorNotice(errorMessage);
            }
        });
    }

    void ActiveErrorNotice(string message)
    {
        errorUI.SetActive(true);
        errorText.text = message;
    }

    public void DeActiveErrorNotice()
    {
        errorUI.SetActive(false);
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

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }


}
