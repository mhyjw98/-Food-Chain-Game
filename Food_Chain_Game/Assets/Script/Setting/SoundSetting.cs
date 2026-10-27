using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SoundSetting : MonoBehaviour
{
    [Header("Master")]
    public Slider masterSlider;
    public TMP_InputField masterValueField;
    public Toggle masterToggle;
    public Image masterHandle;
    public Image masterSliderBackground;
    public Image masterFillBackground;

    [Header("BGM")]
    public Slider bgmSlider;
    public TMP_InputField bgmValueField;
    public Toggle bgmToggle;
    public Image bgmHandle;
    public Image bgmSliderBackground;
    public Image bgmFillBackground;

    [Header("Effect")]
    public Slider effectSlider;
    public TMP_InputField effectValueField;
    public Toggle effectToggle;
    public Image effectHandle;
    public Image effectSliderBackground;
    public Image effectFillBackground;

    [Header("UI")]
    public Slider uiSlider;
    public TMP_InputField uiValueField;
    public Toggle uiToggle;
    public Image uiHandle;
    public Image uiSliderBackground;
    public Image uiFillBackground;

    [Header("Defaults")]
    [Range(0, 100)] public int defaultMaster = 70;
    [Range(0, 100)] public int defaultBgm = 70;
    [Range(0, 100)] public int defaultEffect = 70;
    [Range(0, 100)] public int defaultUi = 70;

    private const string PREF_MASTER_VOLUME = "MasterVolume";
    private const string PREF_BGM_VOLUME = "BgmVolume";
    private const string PREF_EFFECT_VOLUME = "EffectVolume";
    private const string PREF_UI_VOLUME = "UiVolume";

    private const string PREF_MUTE_MASTER = "MuteMaster";
    private const string PREF_MUTE_BGM = "MuteBgm";
    private const string PREF_MUTE_EFFECT = "MuteEffect";
    private const string PREF_MUTE_UI = "MuteUi";
    private class Channel
    {
        public string name;

        public Slider slider;
        public TMP_InputField valueField;
        public Toggle muteToggle;
        public Image handle;
        public Image background;
        public Image fill;

        public string volumePrefKey;
        public string mutePrefKey;

        public int defaultVolume;

        public Action<float> applyVolume;
    }

    private Channel masterChannel;
    private List<Channel> subChannels = new();

    private void Awake()
    {
        BuildChannels();
        LoadFromPrefs();
        HookEvents();
        ApplyAllVolumes();
    }

    private void BuildChannels()
    {
        masterChannel = new Channel
        {
            name = "Master",
            slider = masterSlider,
            valueField = masterValueField,
            muteToggle = masterToggle,
            handle = masterHandle,
            background = masterSliderBackground,
            fill = masterFillBackground,
            volumePrefKey = PREF_MASTER_VOLUME,
            mutePrefKey = PREF_MUTE_MASTER,
            defaultVolume = defaultMaster,

            applyVolume = v => AudioListener.volume = v
        };

        var bgm = new Channel
        {
            name = "BGM",
            slider = bgmSlider,
            valueField = bgmValueField,
            muteToggle = bgmToggle,
            handle = bgmHandle,
            background = bgmSliderBackground,
            fill = bgmFillBackground,
            volumePrefKey = PREF_BGM_VOLUME,
            mutePrefKey = PREF_MUTE_BGM,
            defaultVolume = defaultBgm,

            applyVolume = v => { if (AudioManager.Instance != null) AudioManager.Instance.BGMVolumeSetting(v); }
        };

        var effect = new Channel
        {
            name = "Effect",
            slider = effectSlider,
            valueField = effectValueField,
            muteToggle = effectToggle,
            handle = effectHandle,
            background = effectSliderBackground,
            fill = effectFillBackground,
            volumePrefKey = PREF_EFFECT_VOLUME,
            mutePrefKey = PREF_MUTE_EFFECT,
            defaultVolume = defaultEffect,

            applyVolume = v => { if (AudioManager.Instance != null) AudioManager.Instance.EffectVolumeSetting(v); }
        };

        var ui = new Channel
        {
            name = "UI",
            slider = uiSlider,
            valueField = uiValueField,
            muteToggle = uiToggle,
            handle = uiHandle,
            background = uiSliderBackground,
            fill = uiFillBackground,
            volumePrefKey = PREF_UI_VOLUME,
            mutePrefKey = PREF_MUTE_UI,
            defaultVolume = defaultUi,

            applyVolume = v => { if (AudioManager.Instance != null) AudioManager.Instance.UIVolumeSetting(v); }
        };

        subChannels.Clear();
        subChannels.Add(bgm);
        subChannels.Add(effect);
        subChannels.Add(ui);
    }

    private void LoadFromPrefs()
    {
        int masterVol = PlayerPrefs.GetInt(masterChannel.volumePrefKey, masterChannel.defaultVolume);
        bool muteMaster = PlayerPrefs.GetInt(masterChannel.mutePrefKey, 0) == 1;

        masterChannel.slider.value = masterVol;
        masterChannel.valueField.text = masterVol.ToString();
        masterChannel.muteToggle.isOn = muteMaster;

        foreach (var ch in subChannels)
        {
            int vol = PlayerPrefs.GetInt(ch.volumePrefKey, ch.defaultVolume);

            vol = Mathf.Min(vol, masterVol);

            bool mute = PlayerPrefs.GetInt(ch.mutePrefKey, 0) == 1;

            ch.slider.value = vol;
            ch.valueField.text = vol.ToString();
            ch.muteToggle.isOn = mute;
        }

        UpdateChannelVisual(masterChannel, !muteMaster);
        foreach (var ch in subChannels)
        {
            bool enabled = !muteMaster && !ch.muteToggle.isOn;
            UpdateChannelVisual(ch, enabled);
        }
    }

    private void HookEvents()
    {
        masterChannel.slider.onValueChanged.AddListener(_ =>
        {
            OnMasterSliderChanged();
        });
        masterChannel.valueField.onEndEdit.AddListener(_ =>
        {
            OnMasterInputChanged();
        });
        masterChannel.muteToggle.onValueChanged.AddListener(_ =>
        {
            OnMasterMuteToggled();
        });

        foreach (var ch in subChannels)
        {
            ch.slider.onValueChanged.AddListener(_ =>
            {
                OnSubSliderChanged(ch);
            });
            ch.valueField.onEndEdit.AddListener(_ =>
            {
                OnSubInputChanged(ch);
            });
            ch.muteToggle.onValueChanged.AddListener(_ =>
            {
                OnSubMuteToggled(ch);
            });
        }
    }

    private void OnMasterSliderChanged()
    {
        int v = Mathf.RoundToInt(masterChannel.slider.value);
        masterChannel.valueField.text = v.ToString();

        foreach (var ch in subChannels)
        {
            if (ch.slider.value > v)
            {
                ch.slider.value = v;
                ch.valueField.text = Mathf.RoundToInt(ch.slider.value).ToString();
            }
        }

        ApplyAllVolumes();
    }

    public void OnMasterInputChanged()
    {
        if (!int.TryParse(masterChannel.valueField.text, out int v))
            v = masterChannel.defaultVolume;

        v = Mathf.Clamp(v, 0, 100);
        masterChannel.slider.value = v;
        masterChannel.valueField.text = v.ToString();

        foreach (var ch in subChannels)
        {
            if (ch.slider.value > v)
            {
                ch.slider.value = v;
                ch.valueField.text = Mathf.RoundToInt(ch.slider.value).ToString();
            }
        }

        ApplyAllVolumes();
    }

    public void OnMasterMuteToggled()
    {
        bool muted = masterChannel.muteToggle.isOn;

        UpdateChannelVisual(masterChannel, !muted);
        foreach (var ch in subChannels)
        {
            UpdateChannelVisual(ch, !muted && !ch.muteToggle.isOn);
        }

        ApplyAllVolumes();
    }

    private void OnSubSliderChanged(Channel ch)
    {
        // 마스터보다 크지 않도록
        if (ch.slider.value > masterChannel.slider.value)
        {
            ch.slider.value = masterChannel.slider.value;
        }

        ch.valueField.text = Mathf.RoundToInt(ch.slider.value).ToString();

        ApplyAllVolumes();
    }

    private void OnSubInputChanged(Channel ch)
    {
        if (!int.TryParse(ch.valueField.text, out int v))
            v = ch.defaultVolume;

        v = Mathf.Clamp(v, 0, Mathf.RoundToInt(masterChannel.slider.value));
        ch.slider.value = v;
        ch.valueField.text = v.ToString();

        ApplyAllVolumes();
    }

    private void OnSubMuteToggled(Channel ch)
    {
        bool muted = ch.muteToggle.isOn;
        bool masterMuted = masterChannel.muteToggle.isOn;

        UpdateChannelVisual(ch, !masterMuted && !muted);
        ApplyAllVolumes();
    }

    private void ApplyAllVolumes()
    {
        float masterVol = masterChannel.slider.value / 100f;
        bool masterMuted = masterChannel.muteToggle.isOn;

        // 마스터 볼륨
        masterChannel.applyVolume(masterMuted ? 0f : masterVol);

        // 서브 채널 볼륨.
        // 전체 음량은 AudioListener가 모든 소리에 이미 곱한다. 개별 음량(전체 음량 이하로만 올릴 수 있음)이
        // 그대로 실제 크기가 되도록, 재생기에는 전체 음량으로 나눈 값을 넘긴다.
        // (그대로 넘기면 전체 70 / 배경음 70일 때 실제 크기가 49%로 두 번 줄어든다)
        foreach (var ch in subChannels)
        {
            float v = ch.slider.value / 100f;
            bool muted = masterMuted || ch.muteToggle.isOn;

            float sourceVolume = masterVol > 0f ? Mathf.Clamp01(v / masterVol) : 0f;
            ch.applyVolume(muted ? 0f : sourceVolume);
        }
    }

    private void UpdateChannelVisual(Channel ch, bool enabled)
    {
        if (ch.slider == null) return;

        Color enabledColor = Color.white;
        Color disabledColor = new Color32(80, 80, 80, 255);

        if (ch.handle != null)        
            ch.handle.color = enabled ? enabledColor : disabledColor;
        
        if (ch.background != null)
            ch.background.color = enabled ? enabledColor : disabledColor;

        if (ch.fill != null)
            ch.fill.color = enabled ? enabledColor : disabledColor;
    }

    /// <summary>
    /// 현재 UI 값을 PlayerPrefs에 저장
    /// </summary>
    public void SaveSoundSetting()
    {
        PlayerPrefs.SetInt(PREF_MASTER_VOLUME, Mathf.RoundToInt(masterChannel.slider.value));
        PlayerPrefs.SetInt(PREF_BGM_VOLUME, Mathf.RoundToInt(bgmSlider.value));
        PlayerPrefs.SetInt(PREF_EFFECT_VOLUME, Mathf.RoundToInt(effectSlider.value));
        PlayerPrefs.SetInt(PREF_UI_VOLUME, Mathf.RoundToInt(uiSlider.value));

        PlayerPrefs.SetInt(PREF_MUTE_MASTER, masterChannel.muteToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt(PREF_MUTE_BGM, bgmToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt(PREF_MUTE_EFFECT, effectToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt(PREF_MUTE_UI, uiToggle.isOn ? 1 : 0);

        PlayerPrefs.Save();

        ApplyAllVolumes();
    }

    public void ResetSoundSetting()
    {
        masterChannel.slider.value = defaultMaster;
        masterChannel.valueField.text = defaultMaster.ToString();
        masterChannel.muteToggle.isOn = false;

        foreach (var ch in subChannels)
        {
            int def =
                ch == subChannels[0] ? defaultBgm :
                ch == subChannels[1] ? defaultEffect :
                defaultUi;

            ch.slider.value = def;
            ch.valueField.text = def.ToString();
            ch.muteToggle.isOn = false;
        }

        ApplyAllVolumes();
    }
}

