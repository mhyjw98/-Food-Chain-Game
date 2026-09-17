using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;

public class LobbyVoiceParticipantRow : MonoBehaviour
{
    [SerializeField] TMP_Text nameText;
    [SerializeField] Slider volumeSlider;
    [SerializeField] TMP_Text volumeValueText;
    [SerializeField] Toggle muteToggle;

    const string PREF_KEY_PREFIX = "Vivox_ParticipantVol_";
    const string PREF_MUTE_PREFIX = "Vivox_ParticipantMute_";
    const int DEFAULT_PLAYER_VOL_01 = 70;

    VivoxParticipant _participant;
    bool _uiLock;
    bool? _appliedMute;
    public string PlayerId => _participant != null ? _participant.PlayerId : null;

    public void Bind(VivoxParticipant participant)
    {
        _participant = participant;      

        string nickOnly = VoiceManager.GetNicknameOnly(participant.DisplayName);

        if (nameText != null)
            nameText.text = nickOnly;

        SetupSlider();

        int saved01 = PlayerPrefs.GetInt(VoiceManager.ParticipantVolPrefPrefix + participant.PlayerId, 70);
        SetVolume01(saved01, applyToVivox: true, writePrefs: false);

        if (muteToggle != null)
        {
            muteToggle.onValueChanged.RemoveListener(OnMuteToggleChanged);

            bool savedMute = PlayerPrefs.GetInt(PREF_MUTE_PREFIX + participant.PlayerId, 0) == 1;

            _uiLock = true;          
            muteToggle.isOn = savedMute;
            _uiLock = false;

            muteToggle.onValueChanged.AddListener(OnMuteToggleChanged);

            if (_appliedMute == null || _appliedMute.Value != savedMute)
            {
                _appliedMute = savedMute;
                if (savedMute) participant.MutePlayerLocally();
                else participant.UnmutePlayerLocally();
            }
        }
    }

    void SetupSlider()
    {
        if (volumeSlider == null) return;

        volumeSlider.minValue = 0;
        volumeSlider.maxValue = 100;
        volumeSlider.wholeNumbers = true;

        volumeSlider.onValueChanged.RemoveAllListeners();
        volumeSlider.onValueChanged.AddListener(_ => OnSliderChanged());
    }

    public void ApplyDefaultVolume()
    {
        SetVolume01(DEFAULT_PLAYER_VOL_01, applyToVivox: true, writePrefs: true);
    }
    public void SetVolume01(int v01, bool applyToVivox, bool writePrefs)
    {
        v01 = Mathf.Clamp(v01, 0, 100);

        _uiLock = true;
        if (volumeSlider != null) volumeSlider.value = v01;
        if (volumeValueText != null) volumeValueText.text = v01.ToString();
        _uiLock = false;

        if (applyToVivox)
            ApplyLocalVolume(v01);

        if (writePrefs && _participant != null && !string.IsNullOrEmpty(_participant.PlayerId))
        {
            PlayerPrefs.SetInt(PREF_KEY_PREFIX + _participant.PlayerId, v01);
        }
    }   
    void OnSliderChanged()
    {
        if (_uiLock) return;
        int v01 = volumeSlider != null ? Mathf.RoundToInt(volumeSlider.value) : 50;

        if (volumeValueText != null)
            volumeValueText.text = v01.ToString();

        ApplyLocalVolume(v01);

        if (_participant != null && !string.IsNullOrEmpty(_participant.PlayerId))
        {
            PlayerPrefs.SetInt(PREF_KEY_PREFIX + _participant.PlayerId, v01);
        }
    }

    void ApplyLocalVolume(int v01)
    {
        if (_participant == null) return;

        v01 = Mathf.Clamp(v01, 0, 100);


        if (v01 <= 70)
        {
            float t = v01 / 70f;
            _participant.SetLocalVolume(Mathf.RoundToInt(Mathf.Lerp(-50, 0, t)));
        }
        else
        {
            float t = (v01 - 70f) / 30f;
            _participant.SetLocalVolume(Mathf.RoundToInt(Mathf.Lerp(0, 10, t)));
        }
       
    }

    void OnMuteToggleChanged(bool isMuted)
    {
        if (_uiLock) return;
        if (_participant == null) return;

        _appliedMute = isMuted;

        PlayerPrefs.SetInt(PREF_MUTE_PREFIX + _participant.PlayerId, isMuted ? 1 : 0);
        PlayerPrefs.Save();

        if (isMuted) _participant.MutePlayerLocally();
        else _participant.UnmutePlayerLocally();
    }
}
