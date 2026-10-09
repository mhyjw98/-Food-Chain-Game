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
    public enum EffectSfx { fishBite, fishPull, fishSuccess, fishFail }
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
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;            
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

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
        // 카메라는 씬마다 새로 만들어지므로 필요할 때 다시 찾는다
        if (bgmEffect == null && Camera.main != null)
            bgmEffect = Camera.main.GetComponent<AudioHighPassFilter>();

        if (bgmEffect != null)
            bgmEffect.enabled = isPlay;
    }
    public void PlayEffectSfx(EffectSfx sfx)
    {
        PlaySfx(effectSfxPlayers, effectSfxClips, (int)sfx, ref effectChannelIndex);
    }
    public void PlayUISfx(UISfx sfx)
    {
        PlaySfx(uiSfxPlayers, uiSfxClips, (int)sfx, ref uiChannelIndex);
    }

    // 비어 있는 채널을 찾아 재생한다. 클립이 등록되지 않았거나 채널이 없으면 아무것도 하지 않는다
    private static void PlaySfx(AudioSource[] players, AudioClip[] clips, int clipIndex, ref int channelIndex)
    {
        if (players == null || players.Length == 0) return;
        if (clips == null || clipIndex < 0 || clipIndex >= clips.Length || clips[clipIndex] == null) return;

        for (int i = 0; i < players.Length; i++)
        {
            int loopIndex = (i + channelIndex) % players.Length;

            if (players[loopIndex] == null || players[loopIndex].isPlaying)
                continue;

            channelIndex = loopIndex;
            players[loopIndex].clip = clips[clipIndex];
            players[loopIndex].Play();
            break;
        }
    }

    public void BGMVolumeSetting(float volume)
    {
        bgmVolume = volume;
        if (bgmPlayer != null)
            bgmPlayer.volume = volume;
    }
    public void EffectVolumeSetting(float volume)
    {
        effectSfxVolume = volume;
        SetVolume(effectSfxPlayers, volume);
    }
    public void UIVolumeSetting(float volume)
    {
        uiSfxVolume = volume;
        SetVolume(uiSfxPlayers, volume);
    }

    private static void SetVolume(AudioSource[] players, float volume)
    {
        if (players == null) return;

        for (int index = 0; index < players.Length; index++)
        {
            if (players[index] != null)
                players[index].volume = volume;
        }
    }
}
