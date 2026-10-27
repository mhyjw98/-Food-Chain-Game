using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 설정 창의 플레이어 음성 목록 한 줄: 닉네임, 개별 음량, 개별 음소거, 사망/말하는 중 표시.
// 음성 채널이 아니라 플레이어(음성 ID) 기준이라, 그 사람이 지금 어느 채널에 있든 같은 설정이 적용된다
public class LobbyVoiceParticipantRow : MonoBehaviour
{
    [SerializeField] TMP_Text nameText;
    [SerializeField] Slider volumeSlider;
    [SerializeField] TMP_Text volumeValueText;
    [SerializeField] Toggle muteToggle;
    [SerializeField] string unknownName = "Player";

    [Header("State")]
    [Tooltip("죽은 플레이어일 때 어둡게 바꿀 프로필 배경")]
    [SerializeField] Image profileImage;
    [Tooltip("말하는 동안 켜지는 초록 테두리")]
    [SerializeField] GameObject speakingBorder;
    [SerializeField] Color deadProfileColor = new Color(0.22f, 0.22f, 0.22f, 1f);
    [SerializeField] Color deadNameColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    string _voiceId;
    bool _uiLock;
    bool _listenersHooked;
    bool _colorsCaptured;
    Color _aliveProfileColor;
    Color _aliveNameColor;
    bool? _dead;
    bool? _speaking;

    public string VoiceId => _voiceId;

    public void Bind(string voiceId, string nickname)
    {
        HookListenersOnce();

        string shownName = string.IsNullOrEmpty(nickname) ? unknownName : nickname;
        if (nameText != null && nameText.text != shownName)
            nameText.text = shownName;

        if (_voiceId == voiceId) return;
        _voiceId = voiceId;

        _uiLock = true;

        int volume = PlayerPrefs.GetInt(VoiceManager.ParticipantVolPrefPrefix + voiceId, VoiceManager.DefaultParticipantVolume);
        if (volumeSlider != null) volumeSlider.value = volume;
        if (volumeValueText != null) volumeValueText.text = volume.ToString();

        if (muteToggle != null) muteToggle.isOn = VoiceManager.IsParticipantMutedByMe(voiceId);

        _uiLock = false;
    }

    // dead: 내가 죽음을 알고 있는 플레이어인지 / speaking: 말하는 중으로 표시해도 되는지
    public void SetState(bool dead, bool speaking)
    {
        CaptureColorsOnce();

        if (_dead != dead)
        {
            _dead = dead;
            if (profileImage != null) profileImage.color = dead ? deadProfileColor : _aliveProfileColor;
            if (nameText != null) nameText.color = dead ? deadNameColor : _aliveNameColor;
        }

        if (_speaking != speaking)
        {
            _speaking = speaking;
            if (speakingBorder != null) speakingBorder.SetActive(speaking);
        }
    }

    public void ApplyDefaultVolume()
    {
        _uiLock = true;
        if (volumeSlider != null) volumeSlider.value = VoiceManager.DefaultParticipantVolume;
        _uiLock = false;

        ApplyVolume(VoiceManager.DefaultParticipantVolume);
    }

    void HookListenersOnce()
    {
        if (_listenersHooked) return;
        _listenersHooked = true;

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0;
            volumeSlider.maxValue = 100;
            volumeSlider.wholeNumbers = true;
            volumeSlider.onValueChanged.AddListener(OnSliderChanged);
        }

        if (muteToggle != null)
            muteToggle.onValueChanged.AddListener(OnMuteToggleChanged);
    }

    void CaptureColorsOnce()
    {
        if (_colorsCaptured) return;
        _colorsCaptured = true;

        if (profileImage != null) _aliveProfileColor = profileImage.color;
        if (nameText != null) _aliveNameColor = nameText.color;
    }

    void OnSliderChanged(float value)
    {
        if (_uiLock) return;
        ApplyVolume(Mathf.RoundToInt(value));
    }

    void ApplyVolume(int volume01)
    {
        volume01 = Mathf.Clamp(volume01, 0, 100);
        if (volumeValueText != null) volumeValueText.text = volume01.ToString();

        if (string.IsNullOrEmpty(_voiceId)) return;

        if (VoiceManager.Instance != null)
            VoiceManager.Instance.SetParticipantVolume(_voiceId, volume01);
    }

    void OnMuteToggleChanged(bool isMuted)
    {
        if (_uiLock || string.IsNullOrEmpty(_voiceId)) return;

        if (VoiceManager.Instance != null)
            VoiceManager.Instance.SetParticipantMuted(_voiceId, isMuted);
    }
}
