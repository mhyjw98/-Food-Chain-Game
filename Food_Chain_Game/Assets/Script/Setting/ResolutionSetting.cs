using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ResolutionSetting : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown windowModeDropdown;

    private List<Resolution> availableResolutions = new();

    // 해상도는 목록의 순번이 아니라 가로/세로 크기로 저장한다.
    // 순번은 모니터나 PC가 바뀌면 다른 해상도를 가리킨다(ResolutionIndex는 예전 저장값을 읽는 용도로만 남김)
    private const string PREF_RES_INDEX = "ResolutionIndex";
    private const string PREF_RES_WIDTH = "ResolutionWidth";
    private const string PREF_RES_HEIGHT = "ResolutionHeight";
    private const string PREF_WINDOW_MODE = "WindowMode";

    // 설정 창은 씬마다 있어서 Awake가 씬을 넘어갈 때마다 불린다.
    // 저장된 해상도는 실행 후 한 번만 적용한다(매번 적용하면 씬 전환마다 화면 모드가 다시 설정된다)
    private static bool _appliedThisRun;

    private void Awake()
    {
        InitResolutions();
        InitWindowMode();

        if (!_appliedThisRun)
        {
            _appliedThisRun = true;
            ApplySavedSettings();
        }

        resolutionDropdown.onValueChanged.AddListener(_ =>
        {
            ApplyResolution(save: false);
        });

        windowModeDropdown.onValueChanged.AddListener(_ =>
        {
            ApplyResolution(save: false);
        });
    }

    private void InitResolutions()
    {
        resolutionDropdown.ClearOptions();
        availableResolutions.Clear();

        Resolution[] resolutions = Screen.resolutions;

        var distinct = resolutions
            .GroupBy(r => new { r.width, r.height})
            .Select(g => g.First())
            .OrderByDescending(r => r.width * r.height)
            .ToList();

        availableResolutions = distinct;

        List<string> options = new();
        foreach (var res in availableResolutions)
        {
            options.Add($"{res.width} x {res.height}");
        }

        resolutionDropdown.AddOptions(options);

        int savedWidth = PlayerPrefs.GetInt(PREF_RES_WIDTH, -1);
        int savedHeight = PlayerPrefs.GetInt(PREF_RES_HEIGHT, -1);
        int savedIndex = PlayerPrefs.GetInt(PREF_RES_INDEX, -1);

        int index = FindResolutionIndex(savedWidth, savedHeight);

        // 예전 방식(순번)으로 저장된 값
        if (index < 0 && savedWidth < 0 && savedIndex >= 0 && savedIndex < availableResolutions.Count)
            index = savedIndex;

        // 저장값이 없거나 이 모니터에서 쓸 수 없으면 현재 화면 해상도
        if (index < 0)
            index = FindResolutionIndex(Screen.currentResolution.width, Screen.currentResolution.height);

        resolutionDropdown.value = Mathf.Max(0, index);

        resolutionDropdown.RefreshShownValue();
    }

    private int FindResolutionIndex(int width, int height)
    {
        if (width <= 0 || height <= 0) return -1;

        for (int i = 0; i < availableResolutions.Count; i++)
        {
            if (availableResolutions[i].width == width && availableResolutions[i].height == height)
                return i;
        }
        return -1;
    }

    private void InitWindowMode()
    {
        int savedMode = PlayerPrefs.GetInt(PREF_WINDOW_MODE, 0);
        if (savedMode < 0 || savedMode > 1) savedMode = 0;

        windowModeDropdown.value = savedMode;
        windowModeDropdown.RefreshShownValue();
    }

    private void ApplySavedSettings()
    {
        ApplyResolution(save: false);
    }

    public void SaveResolution()
    {
        ApplyResolution(save: true);
    }

    public void ApplyResolution(bool save)
    {
        if (availableResolutions.Count == 0)
            return;

        int resIndex = Mathf.Clamp(resolutionDropdown.value, 0, availableResolutions.Count - 1);
        int modeIndex = Mathf.Clamp(windowModeDropdown.value, 0, windowModeDropdown.options.Count - 1);

        Resolution res = availableResolutions[resIndex];

        FullScreenMode mode =
            (modeIndex == 0) ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

        Screen.SetResolution(res.width, res.height, mode);

        if (save)
        {
            PlayerPrefs.SetInt(PREF_RES_WIDTH, res.width);
            PlayerPrefs.SetInt(PREF_RES_HEIGHT, res.height);
            PlayerPrefs.SetInt(PREF_WINDOW_MODE, modeIndex);
            PlayerPrefs.Save();

        }
    }

}
