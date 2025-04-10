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
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancleButton;
    [SerializeField] private Button changeButton;

    // Start is called before the first frame update
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
            NickNamemanager.SetNickname(nickname);
            nicknameDisplay.text = $"닉네임: {nickname}";
            nicknamePopup.SetActive(false);
        }
        else
        {
            noticeText.text = "닉네임을 작성해주세요.";
        }
    }
}
