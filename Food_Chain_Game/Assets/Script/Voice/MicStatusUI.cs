using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 화면에 마이크 상태를 보여 주고, 눌러서 음소거·마이크 방식을 바꾸는 작은 표시.
//  - 상태 버튼: 마이크 음소거 켜기/끄기
//  - 방식 버튼: 항상 켜짐 ↔ 눌러서 말하기
public class MicStatusUI : MonoBehaviour
{
    [SerializeField] private Button muteButton;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private Button modeButton;
    [SerializeField] private TMP_Text modeLabel;

    [Header("Text")]
    [SerializeField] private string connectingText = "음성 연결 중";
    [SerializeField] private string mutedText = "마이크 꺼짐";
    [SerializeField] private string openText = "마이크 켜짐";
    [SerializeField] private string talkingText = "말하는 중";
    [Tooltip("{0} = 눌러서 말하기 키")]
    [SerializeField] private string pushToTalkWaitingText = "[{0}] 누르고 말하기";
    [Tooltip("{0} = 음소거 키")]
    [SerializeField] private string openModeText = "항상 켜짐 (음소거 [{0}])";
    [Tooltip("{0} = 음소거 키")]
    [SerializeField] private string pushToTalkModeText = "눌러서 말하기 (음소거 [{0}])";

    [Header("Color")]
    [SerializeField] private Color activeColor = new Color(0.45f, 0.9f, 0.45f);
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color mutedColor = new Color(0.95f, 0.4f, 0.4f);

    private VoiceManager _hooked;

    private void Awake()
    {
        if (muteButton != null) muteButton.onClick.AddListener(OnClickMute);
        if (modeButton != null) modeButton.onClick.AddListener(OnClickMode);
    }

    private void OnEnable()
    {
        TryHook();
        Refresh();
    }

    private void OnDisable()
    {
        Unhook();
    }

    private void Update()
    {
        // VoiceManager가 나중에 만들어지거나 로그인 상태가 바뀌는 경우를 따라간다
        if (_hooked != VoiceManager.Instance)
        {
            Unhook();
            TryHook();
        }

        Refresh();
    }

    private void TryHook()
    {
        if (VoiceManager.Instance == null) return;

        _hooked = VoiceManager.Instance;
        _hooked.OnMicStateChanged += Refresh;
    }

    private void Unhook()
    {
        if (_hooked != null) _hooked.OnMicStateChanged -= Refresh;
        _hooked = null;
    }

    private void OnClickMute()
    {
        if (VoiceManager.Instance != null) VoiceManager.Instance.ToggleMicMuted();
    }

    private void OnClickMode()
    {
        if (VoiceManager.Instance != null) VoiceManager.Instance.ToggleMicMode();
    }

    private void Refresh()
    {
        var voice = VoiceManager.Instance;
        bool connected = voice != null && voice.IsLoggedIn && voice.CurrentContext != VoiceManager.VoiceContext.None;

        if (muteButton != null) muteButton.interactable = connected;
        if (modeButton != null) modeButton.interactable = voice != null;

        if (!connected)
        {
            SetStatus(connectingText, idleColor);
        }
        else if (voice.IsMicMuted)
        {
            SetStatus(mutedText, mutedColor);
        }
        else if (voice.Mode == VoiceManager.MicMode.PushToTalk)
        {
            if (voice.IsMicOpen) SetStatus(talkingText, activeColor);
            else SetStatus(string.Format(pushToTalkWaitingText, voice.PushToTalkKey.ToUserFriendlyString()), idleColor);
        }
        else
        {
            SetStatus(openText, activeColor);
        }

        if (modeLabel != null && voice != null)
        {
            string format = voice.Mode == VoiceManager.MicMode.PushToTalk ? pushToTalkModeText : openModeText;
            string text = string.Format(format, voice.MuteToggleKey.ToUserFriendlyString());
            if (modeLabel.text != text) modeLabel.text = text;
        }
    }

    private void SetStatus(string text, Color color)
    {
        if (statusLabel == null) return;

        if (statusLabel.text != text) statusLabel.text = text;
        if (statusLabel.color != color) statusLabel.color = color;
    }
}
