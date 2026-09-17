using Mirror.BouncyCastle.Tsp;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingManager : MonoBehaviour
{
    [SerializeField] private GameObject settingUI;
    [SerializeField] private TitleUI titleUI;

    [SerializeField] private ResolutionSetting resoultion;
    [SerializeField] private KeySetting keySet;
    [SerializeField] private SoundSetting soundSet;
    [SerializeField] private VoiceSetting voiceSet;

    public static bool isKeySetting;

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (isKeySetting)
            return;

        if (settingUI != null && settingUI.activeSelf)
        {
            CloseSettingUI();
        }
        else if (titleUI == null)
        {
            ActiveSettingUI();
        }

    }
    public void ActiveSettingUI()
    {
        settingUI.SetActive(true);
        UIManager.Instance.Push(UIPriority.Modal);
    }
    public void CloseSettingUI()
    {
        SaveAllToPrefs();
        DeActiveSettingUI();
    }

    public void DeActiveSettingUI()
    {
        settingUI.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Modal);
    }

    private void SaveAllToPrefs()
    {
        resoultion.SaveResolution();
        keySet.SaveKeySetting();
        soundSet.SaveSoundSetting();
        voiceSet.SaveVoiceSetting();
    }
    public void OnClickReturnToTitle()
    {
        SaveAllToPrefs();

        var rm = RoomManager.singleton as RoomManager;

        if (rm != null) rm.CleanupAndLoadTitle(showError: false, errorMessage: null);       
        else SceneManager.LoadScene("Title");
        
    }

    public void QuitGame()
    {
        SaveAllToPrefs();
        VoiceManager.Instance?.LeaveAndLogout();

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
