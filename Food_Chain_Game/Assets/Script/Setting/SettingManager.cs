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

    // 설정 창이 열려 있는지 / 이번 프레임에 ESC로 닫혔는지.
    // 타이틀 화면도 ESC를 처리하므로(종료 팝업), 같은 ESC 한 번이 두 군데서 처리되지 않게 하는 데 쓴다
    public static bool IsOpen { get; private set; }
    private static int _closedFrame = -1;
    public static bool ClosedThisFrame => _closedFrame == Time.frameCount;

    private void OnDisable()
    {
        IsOpen = false;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (isKeySetting)
            return;

        // 나가기 확인창이 떠 있으면 ESC는 확인창만 닫는다
        if (ReturnToTitleConfirmUI.Instance != null && ReturnToTitleConfirmUI.Instance.IsOpen)
        {
            ReturnToTitleConfirmUI.Instance.OnClickCancel();
            return;
        }

        if (settingUI != null && settingUI.activeSelf)
        {
            CloseSettingUI();
        }
        // 열려 있는 창이 있으면 가장 나중에 연 창을 닫는다(미션 중·진행 중이면 아무것도 하지 않음)
        else if (UIManager.Instance != null && UIManager.Instance.HandleEscape())
        {
        }
        else if (titleUI == null)
        {
            ActiveSettingUI();
        }

    }
    public void ActiveSettingUI()
    {
        // 이미 열려 있으면 잠금(Modal)을 한 번 더 걸지 않는다(닫을 때 한 번만 풀기 때문에 이동이 막힌 채 남는다)
        if (settingUI == null || settingUI.activeSelf) return;

        settingUI.SetActive(true);
        IsOpen = true;

        if (UIManager.Instance != null)
            UIManager.Instance.Push(UIPriority.Modal);
    }
    public void CloseSettingUI()
    {
        DeActiveSettingUI();
    }

    // 닫기(X) 버튼과 ESC가 같은 동작을 한다: 저장하고 닫는다.
    // (예전에는 X 버튼만 저장하지 않아, 바꾼 값이 다음 씬에서 조용히 되돌아갔다)
    public void DeActiveSettingUI()
    {
        if (settingUI == null || !settingUI.activeSelf) return;

        SaveAllToPrefs();

        settingUI.SetActive(false);
        IsOpen = false;
        _closedFrame = Time.frameCount;

        if (UIManager.Instance != null)
            UIManager.Instance.Pop(UIPriority.Modal);
    }

    private void SaveAllToPrefs()
    {
        if (resoultion != null) resoultion.SaveResolution();
        if (keySet != null) keySet.SaveKeySetting();
        if (soundSet != null) soundSet.SaveSoundSetting();
        if (voiceSet != null) voiceSet.SaveVoiceSetting();
    }
    public void OnClickReturnToTitle()
    {
        // 실수로 나가지 않도록 먼저 확인창을 띄운다(방장 여부 무관). 확인창이 없는 씬에서는 바로 이동
        if (ReturnToTitleConfirmUI.TryShow(ReturnToTitle)) return;

        ReturnToTitle();
    }

    private void ReturnToTitle()
    {
        SaveAllToPrefs();

        var rm = RoomManager.singleton as RoomManager;

        if (rm != null) rm.LeaveRoom();       
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
