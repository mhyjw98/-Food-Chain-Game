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
            nicknameDisplay.text = $"�г���: {NickNamemanager.GetNickname()}";
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
                noticeText.text = "�г����� 8���� ���Ϸ� �����ּ���.";
                return;
            }
                
            NickNamemanager.SetNickname(nickname);
            nicknameDisplay.text = $"�г���: {nickname}";
            nicknamePopup.SetActive(false);
        }
        else
        {
            noticeText.text = "�г����� �ۼ����ּ���.";
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
        Debug.Log("ȣ��Ʈ ����");
        RoomManager.Instance.maxPlayerCount = SelectedPlayerCount;
        RoomManager.singleton.StartHost();

        // �� ����� �Լ� ���ο��� ���
        string hostIp = NetworkUtils.GetLocalIPAddress(); // ���� IP �ڵ� ȹ��
        string roomCode = CodeManager.Instance.RegisterRoom(hostIp);

        Debug.Log($"[�� ���� �Ϸ�] �� IP: {hostIp}, �� �ڵ�: {roomCode}");
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
            Debug.Log($"���õ� �ο� ��: {SelectedPlayerCount}");
        }
        else
        {
            Debug.LogWarning("��Ӵٿ� �� �Ľ� ����");
        }
    }

    public void JoinByCode()
    {
        string code = codeInputField.text;
        if (string.IsNullOrEmpty(code))
        {
            Debug.Log("�Է�â�� �� ��");
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
