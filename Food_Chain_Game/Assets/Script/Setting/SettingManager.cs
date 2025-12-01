using Mirror.BouncyCastle.Tsp;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingManager : MonoBehaviour
{
    [SerializeField] private GameObject settingUI;
    [SerializeField] private GameObject checkUI;
    [SerializeField] private TitleUI titleUI;

    [SerializeField] private ResolutionSetting resoultion;
    [SerializeField] private KeySetting keySet;
    [SerializeField] private SoundSetting soundSet;

    public static bool isKeySetting;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if (checkUI.activeSelf == true)
                checkUI.SetActive(false);
            else if (settingUI.activeSelf == true)
                OnClickCancleAfterCheck();
            else if (titleUI == null)
                ActiveSettingUI();
        }
    }
    public void OnClickApply()
    {
        resoultion.SaveResolution();
        keySet.SaveKeySetting();
        soundSet.SaveSoundSetting();
    }
    public void OnClickCompleteSet()
    {
        resoultion.SaveResolution();
        keySet.SaveKeySetting();
        soundSet.SaveSoundSetting();

        DeActiveSettingUI();
    }
    public void OnClickResetDefault()
    {
        keySet.ResetKeySetting();
        soundSet.ResetSoundSetting();
    }
    public void OnClickCancle()
    {        
        resoultion.RevertToSnapshot();
        keySet.RevertToSnapshot();
        soundSet.RevertToSnapshot();

        DeActiveSettingUI();
        checkUI.SetActive(false);
    }

    public void OnClickCancleAfterCheck()
    {
        bool isChange = resoultion.CheckDirty() || keySet.CheckDirty() || soundSet.CheckDirty();

        if (isChange)
        {
            ActiveCheckUI();
        }
        else
        {
            DeActiveSettingUI();
        }
    }
    public void ActiveSettingUI()
    {
        settingUI.SetActive(true);
        isKeySetting = true;
    }

    public void DeActiveSettingUI()
    {
        settingUI.SetActive(false);
        isKeySetting = false;
    }

    public void ActiveCheckUI()
    {
        checkUI.SetActive(true);
    }
    public void DeActiveCheckUI()
    {
        checkUI.SetActive(false);
    }

    public void OnClickReturnToTitle()
    {
        var rm = RoomManager.singleton as RoomManager;

        if (rm != null)
        {
            rm.CleanupAndLoadTitle(showError: false, errorMessage: null);
        }
        else
        {
            Debug.LogWarning("[SettingManager] RoomManager Null 직접 Title씬으로 이동");
            SceneManager.LoadScene("Title");
        }
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
