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
    [Tooltip("음성 채널에 들어가 다른 플레이어의 소리를 들을 수 있는 상태일 때 앞에 붙는 문구")]
    [SerializeField] private string joinedText = "음성 채널 참여중";
    [Tooltip("{0} = 참여 문구, {1} = 마이크 상태")]
    [SerializeField] private string joinedFormat = "{0} · {1}";
    [Tooltip("아직 음성 채널에 들어가지 못했을 때의 문구(비우면 아무것도 표시하지 않는다)")]
    [SerializeField] private string notJoinedText = "";
    [SerializeField] private string mutedText = "마이크 꺼짐";
    [SerializeField] private string openText = "마이크 켜짐";
    [Tooltip("{0} = 음소거 키")]
    [SerializeField] private string openModeText = "항상 켜짐 (음소거 [{0}])";
    [Tooltip("{0} = 음소거 키, {1} = 눌러서 말하기 키")]
    [SerializeField] private string pushToTalkModeFormat = "눌러서 말하기 [{1}] (음소거 [{0}])";

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
        // 채널 입장이 끝나 다른 플레이어의 소리를 들을 수 있는 상태인지
        bool connected = voice != null && voice.IsLoggedIn && voice.CurrentContext != VoiceManager.VoiceContext.None;

        if (muteButton != null) muteButton.interactable = connected;
        if (modeButton != null) modeButton.interactable = voice != null;

        if (!connected)
        {
            SetStatus(notJoinedText, idleColor);
        }
        else if (voice.IsMicMuted)
        {
            SetJoinedStatus(mutedText, mutedColor);
        }
        else if (voice.IsMicOpen)
        {
            SetJoinedStatus(openText, activeColor);
        }
        else
        {
            // 눌러서 말하기: 키를 누르고 있는 동안만 켜진 것으로 표시한다
            SetJoinedStatus(mutedText, mutedColor);
        }

        if (modeLabel != null && voice != null)
        {
            string format = voice.Mode == VoiceManager.MicMode.PushToTalk ? pushToTalkModeFormat : openModeText;
            string text = string.Format(format, voice.MuteToggleKey.ToUserFriendlyString(), voice.PushToTalkKey.ToUserFriendlyString());
            if (modeLabel.text != text) modeLabel.text = text;
        }
    }

    private void SetJoinedStatus(string micState, Color color)
    {
        SetStatus(string.IsNullOrEmpty(joinedText) ? micState : string.Format(joinedFormat, joinedText, micState), color);
    }

    private void SetStatus(string text, Color color)
    {
        if (statusLabel == null) return;

        if (statusLabel.text != text) statusLabel.text = text;
        if (statusLabel.color != color) statusLabel.color = color;
    }
}
