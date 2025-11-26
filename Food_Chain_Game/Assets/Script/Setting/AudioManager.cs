using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    public static AudioManager Instance => instance;

    [Header("BGM")]
    public AudioClip bgmClip;
    public float bgmVolume;
    AudioSource bgmPlayer;
    AudioHighPassFilter bgmEffect;

    [Header("EFFECT SFX")]
    public AudioClip[] effectSfxClips;
    public float effectSfxVolume;
    public int effectChannels;
    AudioSource[] effectSfxPlayers;
    int effectChannelIndex;

    [Header("UI SFX")]
    public AudioClip[] uiSfxClips;
    public float uiSfxVolume;
    public int uiChannels;
    AudioSource[] uiSfxPlayers;
    int uiChannelIndex;

    bool isMuteTotalSound;
    bool isMuteBgmSound;
    bool isMuteEffectSound;
    bool isMuteUiSound;
    public enum EffectSfx { getItem, hitEnemy, playerAttack }
    public enum UISfx { uiList, dungeonList, characterInfo }

    const string KEY_MUTE_TOTAL = "MuteMaster";
    const string KEY_MUTE_BGM = "MuteBgm";
    const string KEY_MUTE_EFFECT = "MuteEffect";
    const string KEY_MUTE_UI = "MuteUi";

    const string KEY_VOL_BGM = "BgmVolume";
    const string KEY_VOL_EFFECT = "EffectVolume";
    const string KEY_VOL_UI = "UiVolume";
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        CheckMuteSound();
        Init();
    }

    void CheckMuteSound()
    {
        isMuteTotalSound = PlayerPrefs.GetInt(KEY_MUTE_TOTAL, 0) == 1;
        isMuteBgmSound = PlayerPrefs.GetInt(KEY_MUTE_BGM, 0) == 1;
        isMuteEffectSound = PlayerPrefs.GetInt(KEY_MUTE_EFFECT, 0) == 1;
        isMuteUiSound = PlayerPrefs.GetInt(KEY_MUTE_UI, 0) == 1;
    }
    void Init()
    {
        float loadedBgm = PlayerPrefs.GetInt(KEY_VOL_BGM, 70) / 100f;
        float loadedEffect = PlayerPrefs.GetInt(KEY_VOL_EFFECT, 70) / 100f;
        float loadedUi = PlayerPrefs.GetInt(KEY_VOL_UI, 70) / 100f;

        if (isMuteTotalSound)
        {
            bgmVolume = 0f;
            effectSfxVolume = 0f;
            uiSfxVolume = 0f;
        }
        else
        {
            bgmVolume = isMuteBgmSound ? 0f : loadedBgm;
            effectSfxVolume = isMuteEffectSound ? 0f : loadedEffect;
            uiSfxVolume = isMuteUiSound ? 0f : loadedUi;
        }

        GameObject bgmObject = new GameObject("BgmPlayer");
        bgmObject.transform.parent = transform;
        bgmPlayer = bgmObject.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop = true;
        bgmPlayer.volume = bgmVolume;
        bgmPlayer.clip = bgmClip;

        if (Camera.main != null)
            bgmEffect = Camera.main.GetComponent<AudioHighPassFilter>();

        // Effect Sfx 초기화
        GameObject effectSfxObject = new GameObject("EffectSfxPlayer");
        effectSfxObject.transform.parent = transform;
        effectSfxPlayers = new AudioSource[effectChannels];

        for (int i = 0; i < effectChannels; i++)
        {
            effectSfxPlayers[i] = effectSfxObject.AddComponent<AudioSource>();
            effectSfxPlayers[i].playOnAwake = false;
            effectSfxPlayers[i].bypassListenerEffects = true;
            effectSfxPlayers[i].volume = effectSfxVolume;
        }

        // UI Sfx 초기화
        GameObject uiSfxObject = new GameObject("UiSfxPlayer");
        uiSfxObject.transform.parent = transform;
        uiSfxPlayers = new AudioSource[uiChannels];

        for (int i = 0; i < uiChannels; i++)
        {
            uiSfxPlayers[i] = uiSfxObject.AddComponent<AudioSource>();
            uiSfxPlayers[i].playOnAwake = false;
            uiSfxPlayers[i].bypassListenerEffects = true;
            uiSfxPlayers[i].volume = uiSfxVolume;
        }
    }
    public void PlayBgm(bool isPlay)
    {
        if (bgmPlayer == null) return;

        if (isPlay)       
            bgmPlayer.Play();      
        else
            bgmPlayer.Stop();
    }

    public void EffectBgm(bool isPlay)
    {
        if (bgmEffect != null)
            bgmEffect.enabled = isPlay;
    }
    public void PlayEffectSfx(EffectSfx sfx)
    {
        if (effectSfxClips == null || effectSfxClips.Length == 0)
            return;

        for (int i = 0; i < effectChannels; i++)
        {
            int loopIndex = (i + effectChannelIndex) % effectChannels;

            if (effectSfxPlayers[loopIndex].isPlaying)
                continue;

            int ranIndex = 0;

            effectChannelIndex = loopIndex;
            effectSfxPlayers[loopIndex].clip = effectSfxClips[(int)sfx + ranIndex];
            effectSfxPlayers[loopIndex].Play();
            break;
        }
    }    
    public void PlayUISfx(UISfx sfx)
    {
        for (int i = 0; i < uiChannels; i++)
        {
            int loopIndex = (i + uiChannelIndex) % uiChannels;

            if (uiSfxPlayers[loopIndex].isPlaying)
                continue;

            int ranIndex = 0;

            uiChannelIndex = loopIndex;
            uiSfxPlayers[loopIndex].clip = uiSfxClips[(int)sfx + ranIndex];
            uiSfxPlayers[loopIndex].Play();
            break;
        }
    }

    public void BGMVolumeSetting(float volume)
    {
        bgmPlayer.volume = volume;
    }
    public void EffectVolumeSetting(float volume)
    {
        for (int index = 0; index < effectChannels; index++)
        {
            effectSfxPlayers[index].volume = volume;
        }
    }
    public void UIVolumeSetting(float volume)
    {
        for (int index = 0; index < uiChannels; index++)
        {
            uiSfxPlayers[index].volume = volume;
        }
    }
}
