using Michsky.MUIP;
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

    Resolution[] resolutions;
    int currentIndex;

    public bool HasUnsavedChanges { get; private set; }
    public event Action<bool> OnDirtyChanged;

    private struct Snapshot
    {
        public int resolutionIndex;
        public int windowModeIndex;
    }

    private Snapshot savedSnapshot;

    private List<Resolution> availableResolutions = new();

    private const string PREF_RES_INDEX = "ResolutionIndex";
    private const string PREF_WINDOW_MODE = "WindowMode";

    private void Awake()
    {
        InitResolutions();
        InitWindowMode();
        ApplySavedSettings();
        CaptureSnapshot();
        MarkDirty(false);

        resolutionDropdown.onValueChanged.AddListener(_ => MarkDirty(true));
        windowModeDropdown.onValueChanged.AddListener(_ => MarkDirty(true));
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

        int savedIndex = PlayerPrefs.GetInt(PREF_RES_INDEX, -1);

        if (savedIndex >= 0 && savedIndex < availableResolutions.Count)
        {
            resolutionDropdown.value = savedIndex;
        }
        else
        {
            int currentIndex = 0;
            for (int i = 0; i < availableResolutions.Count; i++)
            {
                if (availableResolutions[i].width == Screen.currentResolution.width &&
                    availableResolutions[i].height == Screen.currentResolution.height)
                {
                    currentIndex = i;
                    break;
                }
            }
            resolutionDropdown.value = currentIndex;
        }

        resolutionDropdown.RefreshShownValue();
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
            PlayerPrefs.SetInt(PREF_RES_INDEX, resIndex);
            PlayerPrefs.SetInt(PREF_WINDOW_MODE, modeIndex);
            PlayerPrefs.Save();

            CaptureSnapshot();
        }
    }

    public void CaptureSnapshot()
    {
        savedSnapshot = new Snapshot
        {
            resolutionIndex = resolutionDropdown.value,
            windowModeIndex = windowModeDropdown.value
        };
        MarkDirty(false);
    }
    private void MarkDirty(bool dirty)
    {
        if (dirty)
            dirty = CheckDirty();

        if (HasUnsavedChanges == dirty) return;

        HasUnsavedChanges = dirty;
        OnDirtyChanged?.Invoke(HasUnsavedChanges);
    }

    public bool CheckDirty()
    {
        if (savedSnapshot.resolutionIndex != resolutionDropdown.value) return true;
        if (savedSnapshot.windowModeIndex != windowModeDropdown.value) return true;

        return false;
    }

    public void RevertToSnapshot()
    {
        resolutionDropdown.value = Mathf.Clamp(
            savedSnapshot.resolutionIndex, 0,
            Mathf.Max(0, availableResolutions.Count - 1));

        resolutionDropdown.RefreshShownValue();

        windowModeDropdown.value = Mathf.Clamp(
            savedSnapshot.windowModeIndex, 0,
            Mathf.Max(0, windowModeDropdown.options.Count - 1));

        windowModeDropdown.RefreshShownValue();

        ApplyResolution(save: false);

        MarkDirty(false);
    }
}
