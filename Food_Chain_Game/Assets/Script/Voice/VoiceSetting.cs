using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;

public class VoiceSetting : MonoBehaviour
{
    [Header("Output (Voice)")]
    [SerializeField] private Slider outputSlider;
    [SerializeField] private TMP_InputField outputValueField;
    [SerializeField] private TMP_Dropdown outputDeviceDropdown;

    [Header("Input (Mic)")]
    [SerializeField] private Slider inputSlider;
    [SerializeField] private TMP_InputField inputValueField;
    [SerializeField] private TMP_Dropdown inputDeviceDropdown;

    [Header("Optional")]
    [SerializeField] private Button refreshDevicesButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Defaults (0~100)")]
    [Range(0, 100)][SerializeField] private int defaultOutput = 70;
    [Range(0, 100)][SerializeField] private int defaultInput = 70;

    private const string PREF_OUTPUT = "Voice_Output_0_100";
    private const string PREF_INPUT = "Voice_Input_0_100";
    private const string PREF_OUTPUT_DEVICE_ID = "Voice_Output_DeviceId";
    private const string PREF_INPUT_DEVICE_ID = "Voice_Input_DeviceId";

    private const int DEFAULT = 70;
    private const int MAX_BOOST_VIVOX = 10;

    private const string LABEL_SCANNING = "장치 탐색중...";
    private const string LABEL_NONE = "장치 없음";

    private bool _uiLock;
    private Snapshot _snapshot;

    private readonly List<VivoxInputDevice> _cachedInputs = new();
    private readonly List<VivoxOutputDevice> _cachedOutputs = new();

    private struct Snapshot
    {
        public int outVol01;
        public int inVol01;
        public string outDevId;
        public string inDevId;
    }

    private void Awake()
    {
        SetupSlider(outputSlider);
        SetupSlider(inputSlider);

        SetupDropdownUI(outputDeviceDropdown);
        SetupDropdownUI(inputDeviceDropdown);

        SetDropdownSingleLabel(inputDeviceDropdown, LABEL_SCANNING, interactable: false);
        SetDropdownSingleLabel(outputDeviceDropdown, LABEL_SCANNING, interactable: false);

        HookEvents();
        LoadFromPrefsToUI();
        CaptureSnapshot();      
    }
    private void OnEnable()
    {
        if (VoiceManager.Instance != null)
        {
            VoiceManager.Instance.OnDeviceListsChanged += OnDeviceListsChanged;
            OnDeviceListsChanged();
        }             

        RefreshDeviceDropdownsFromManager();
        TryApplyAllToVivox();
    }

    private void OnDisable()
    {
        if (VoiceManager.Instance != null)
            VoiceManager.Instance.OnDeviceListsChanged -= OnDeviceListsChanged;
    }
    private void OnDeviceListsChanged()
    {
        RefreshDeviceDropdownsFromManager();
        ApplySavedDeviceSelection();
    }

    private void SetupSlider(Slider s)
    {
        if (s == null) return;
        s.minValue = 0;
        s.maxValue = 100;
        s.wholeNumbers = true;
    }
    private void SetupDropdownUI(TMP_Dropdown dd)
    {
        if (dd == null) return;

        if (dd.captionText == null)
        {
            dd.captionText = dd.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();

            if (dd.captionText == null)
                dd.captionText = dd.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault();
        }

        if (dd.itemText == null && dd.template != null)
        {
            var itemLabel =
                dd.template.Find("Viewport/Content/Item/Item Label") ??
                dd.template.Find("Viewport/Content/Item/ItemLabel");

            dd.itemText = itemLabel?.GetComponent<TextMeshProUGUI>();

            if (dd.itemText == null)
                dd.itemText = dd.template.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault();
        }

        if (dd.captionText != null)
        {
            dd.captionText.overflowMode = TextOverflowModes.Ellipsis;
            dd.captionText.enableWordWrapping = false;
        }
        if (dd.itemText != null)
        {
            dd.itemText.overflowMode = TextOverflowModes.Ellipsis;
            dd.itemText.enableWordWrapping = false;
        }

        if (dd.template != null)
        {
            var canvas = dd.template.GetComponent<Canvas>();
            if (canvas == null) canvas = dd.template.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 300;

            if (dd.template.GetComponent<GraphicRaycaster>() == null)
                dd.template.gameObject.AddComponent<GraphicRaycaster>();

            var viewport = dd.template.Find("Viewport");
            if (viewport != null)
            {
                if (viewport.GetComponent<RectMask2D>() == null && viewport.GetComponent<Mask>() == null)
                    viewport.gameObject.AddComponent<RectMask2D>();
            }
        }
    }
    private void HookEvents()
    {
        if (outputSlider != null) outputSlider.onValueChanged.AddListener(_ => OnOutputSliderChanged());
        if (outputValueField != null) outputValueField.onEndEdit.AddListener(_ => OnOutputInputChanged());

        if (inputSlider != null) inputSlider.onValueChanged.AddListener(_ => OnInputSliderChanged());
        if (inputValueField != null) inputValueField.onEndEdit.AddListener(_ => OnInputInputChanged());

        if (outputDeviceDropdown != null) outputDeviceDropdown.onValueChanged.AddListener(OnOutputDeviceSelected);
        if (inputDeviceDropdown != null) inputDeviceDropdown.onValueChanged.AddListener(OnInputDeviceSelected);

        if (refreshDevicesButton != null) refreshDevicesButton.onClick.AddListener(() =>
        {
            RefreshDeviceDropdownsFromManager();
            ApplySavedDeviceSelection();
        });
    }

    private void LoadFromPrefsToUI()
    {
        _uiLock = true;
        try
        {
            int outVol = PlayerPrefs.GetInt(PREF_OUTPUT, defaultOutput);
            int inVol = PlayerPrefs.GetInt(PREF_INPUT, defaultInput);

            SetOutputUI(outVol);
            SetInputUI(inVol);
        }
        finally
        {
            _uiLock = false;
        }
    }

    private void SetOutputUI(int vol01)
    {
        vol01 = Mathf.Clamp(vol01, 0, 100);
        if (outputSlider != null) outputSlider.value = vol01;
        if (outputValueField != null) outputValueField.text = vol01.ToString();
    }

    private void SetInputUI(int vol01)
    {
        vol01 = Mathf.Clamp(vol01, 0, 100);
        if (inputSlider != null) inputSlider.value = vol01;
        if (inputValueField != null) inputValueField.text = vol01.ToString();
    }

    private void CaptureSnapshot()
    {
        _snapshot = new Snapshot
        {
            outVol01 = GetOutput01(),
            inVol01 = GetInput01(),
            outDevId = PlayerPrefs.GetString(PREF_OUTPUT_DEVICE_ID, string.Empty),
            inDevId = PlayerPrefs.GetString(PREF_INPUT_DEVICE_ID, string.Empty),
        };
    }

    public void SaveVoiceSetting()
    {
        PlayerPrefs.SetInt(PREF_OUTPUT, GetOutput01());
        PlayerPrefs.SetInt(PREF_INPUT, GetInput01());

        PlayerPrefs.SetString(PREF_OUTPUT_DEVICE_ID, GetSelectedOutputDeviceIdSafe());
        PlayerPrefs.SetString(PREF_INPUT_DEVICE_ID, GetSelectedInputDeviceIdSafe());

        PlayerPrefs.Save();
        CaptureSnapshot();
    }

    public void RevertToSnapshot()
    {
        _uiLock = true;
        try
        {
            SetOutputUI(_snapshot.outVol01);
            SetInputUI(_snapshot.inVol01);

            SelectDeviceDropdownById(outputDeviceDropdown, _cachedOutputs.Select(d => d.DeviceID).ToList(), _snapshot.outDevId);
            SelectDeviceDropdownById(inputDeviceDropdown, _cachedInputs.Select(d => d.DeviceID).ToList(), _snapshot.inDevId);
        }
        finally
        {
            _uiLock = false;
        }

        TryApplyAllToVivox();
    }

    public void ResetToDefault()
    {
        _uiLock = true;
        try
        {
            SetOutputUI(defaultOutput);
            SetInputUI(defaultInput);
        }
        finally
        {
            _uiLock = false;
        }

        TryApplyAllToVivox();
    }

    private int GetOutput01() => outputSlider != null ? Mathf.RoundToInt(outputSlider.value) : defaultOutput;
    private int GetInput01() => inputSlider != null ? Mathf.RoundToInt(inputSlider.value) : defaultInput;

    private void OnOutputSliderChanged()
    {
        if (_uiLock) return;
        if (outputValueField != null) outputValueField.text = GetOutput01().ToString();
        TryApplyOutputToVivox();
    }

    private void OnOutputInputChanged()
    {
        if (_uiLock) return;
        if (outputValueField == null) return;

        if (!int.TryParse(outputValueField.text, out int v)) v = GetOutput01();
        v = Mathf.Clamp(v, 0, 100);

        _uiLock = true;
        outputSlider.value = v;
        outputValueField.text = v.ToString();
        _uiLock = false;

        TryApplyOutputToVivox();
    }

    private void OnInputSliderChanged()
    {
        if (_uiLock) return;
        if (inputValueField != null) inputValueField.text = GetInput01().ToString();
        TryApplyInputToVivox();
    }

    private void OnInputInputChanged()
    {
        if (_uiLock) return;
        if (inputValueField == null) return;

        if (!int.TryParse(inputValueField.text, out int v)) v = GetInput01();
        v = Mathf.Clamp(v, 0, 100);

        _uiLock = true;
        inputSlider.value = v;
        inputValueField.text = v.ToString();
        _uiLock = false;

        TryApplyInputToVivox();
    }

    private void OnOutputDeviceSelected(int index)
    {
        if (_uiLock) return;
        if (index < 0 || index >= _cachedOutputs.Count) return;

        var dev = _cachedOutputs[index];
        PlayerPrefs.SetString(PREF_OUTPUT_DEVICE_ID, dev.DeviceID);
        _ = SafeSetActiveOutput(dev);
    }

    private void OnInputDeviceSelected(int index)
    {
        if (_uiLock) return;
        if (index < 0 || index >= _cachedInputs.Count) return;

        var dev = _cachedInputs[index];
        PlayerPrefs.SetString(PREF_INPUT_DEVICE_ID, dev.DeviceID);
        _ = SafeSetActiveInput(dev);
    }

    private void RefreshDeviceDropdownsFromManager() // [CHANGED]
    {
        SetDropdownSingleLabel(inputDeviceDropdown, LABEL_SCANNING, interactable: false);
        SetDropdownSingleLabel(outputDeviceDropdown, LABEL_SCANNING, interactable: false);
        SetStatus(LABEL_SCANNING);

        if (VoiceManager.Instance == null || !VoiceManager.Instance.IsLoggedIn)
        {
            SetStatus("장치 목록 탐색중...");
            return;
        }

        _cachedInputs.Clear();
        _cachedOutputs.Clear();

        _cachedInputs.AddRange(VoiceManager.Instance.CachedInputs);
        _cachedOutputs.AddRange(VoiceManager.Instance.CachedOutputs);

        if (_cachedInputs.Count == 0)
        {
            SetDropdownSingleLabel(inputDeviceDropdown, LABEL_NONE, interactable: false);
        }
        else
        {
            BuildDropdown(inputDeviceDropdown, _cachedInputs.Select(d => d.DeviceName).ToList());
        }

        if (_cachedOutputs.Count == 0)
        {
            SetDropdownSingleLabel(outputDeviceDropdown, LABEL_NONE, interactable: false);
        }
        else
        {
            BuildDropdown(outputDeviceDropdown, _cachedOutputs.Select(d => d.DeviceName).ToList());
        }

        SetStatus($"입력 {_cachedInputs.Count}개 / 출력 {_cachedOutputs.Count}개 디바이스 로드됨");
    }
    private void ApplySavedDeviceSelection()
    {
        string savedIn = PlayerPrefs.GetString(PREF_INPUT_DEVICE_ID, string.Empty);
        if (!string.IsNullOrEmpty(savedIn) && _cachedInputs != null)
        {
            int idx = _cachedInputs.FindIndex(d => d != null && d.DeviceID == savedIn);
            if (idx >= 0 && inputDeviceDropdown != null && inputDeviceDropdown.options.Count > idx)
            {
                _uiLock = true;
                inputDeviceDropdown.value = idx;
                inputDeviceDropdown.RefreshShownValue();
                _uiLock = false;

                _ = SafeSetActiveInput(_cachedInputs[idx]);
            }
        }

        string savedOut = PlayerPrefs.GetString(PREF_OUTPUT_DEVICE_ID, string.Empty);
        if (!string.IsNullOrEmpty(savedOut) && _cachedOutputs != null)
        {
            int idx = _cachedOutputs.FindIndex(d => d != null && d.DeviceID == savedOut);
            if (idx >= 0 && outputDeviceDropdown != null && outputDeviceDropdown.options.Count > idx)
            {
                _uiLock = true;
                outputDeviceDropdown.value = idx;
                outputDeviceDropdown.RefreshShownValue();
                _uiLock = false;

                _ = SafeSetActiveOutput(_cachedOutputs[idx]);
            }
        }
    }
    private void BuildDropdown(TMP_Dropdown dd, List<string> names)
    {
        if (dd == null) return;

        _uiLock = true;
        dd.ClearOptions();

        if (names == null || names.Count == 0)
        {
            dd.AddOptions(new List<string> { LABEL_NONE });
            dd.value = 0;
            dd.interactable = false;
        }
        else
        {
            dd.AddOptions(names);
            dd.value = 0;
            dd.interactable = true;
        }

        dd.RefreshShownValue();
        _uiLock = false;
    }

    private void SelectDeviceDropdownById(TMP_Dropdown dd, List<string> ids, string targetId)
    {
        if (dd == null) return;
        if (ids == null || ids.Count == 0) return;
        if (string.IsNullOrEmpty(targetId)) return;

        int idx = ids.IndexOf(targetId);
        if (idx < 0) return;

        _uiLock = true;
        dd.value = idx;
        dd.RefreshShownValue();
        _uiLock = false;
    }

    private string GetSelectedInputDeviceIdSafe()
    {
        if (inputDeviceDropdown == null) return PlayerPrefs.GetString(PREF_INPUT_DEVICE_ID, "");
        int idx = inputDeviceDropdown.value;
        if (idx < 0 || idx >= _cachedInputs.Count) return PlayerPrefs.GetString(PREF_INPUT_DEVICE_ID, "");
        return _cachedInputs[idx].DeviceID;
    }

    private string GetSelectedOutputDeviceIdSafe()
    {
        if (outputDeviceDropdown == null) return PlayerPrefs.GetString(PREF_OUTPUT_DEVICE_ID, "");
        int idx = outputDeviceDropdown.value;
        if (idx < 0 || idx >= _cachedOutputs.Count) return PlayerPrefs.GetString(PREF_OUTPUT_DEVICE_ID, "");
        return _cachedOutputs[idx].DeviceID;
    }

    private bool IsVivoxInitialized()
    {
        try
        {
            return VivoxService.Instance != null
                && VoiceManager.Instance != null
                && VoiceManager.Instance.IsLoggedIn;
        }
        catch
        {
            return false;
        }
    }

    private void TryApplyAllToVivox()
    {
        TryApplyInputToVivox();
        TryApplyOutputToVivox();
    }

    private void TryApplyInputToVivox()
    {
        if (!IsVivoxInitialized()) return;

        VivoxService.Instance.SetInputDeviceVolume(Map01ToVivox(GetInput01()));
    }

    private void TryApplyOutputToVivox()
    {
        if (!IsVivoxInitialized()) return;

        int v = Map01ToVivox(GetOutput01());
        VivoxService.Instance.SetOutputDeviceVolume(v);

        string ch = null;
        if (VoiceManager.Instance != null) ch = VoiceManager.Instance.CurrentChannel;
        if (!string.IsNullOrEmpty(ch))
        {
            _ = VivoxService.Instance.SetChannelVolumeAsync(ch, v);
        }
    }

    private async System.Threading.Tasks.Task SafeSetActiveInput(VivoxInputDevice dev)
    {
        if (!IsVivoxInitialized()) return;
        try
        {
            await VivoxService.Instance.SetActiveInputDeviceAsync(dev); // :contentReference[oaicite:6]{index=6}
        }
        catch (Exception e)
        {
            SetStatus($"입력 디바이스 변경 실패: {e.Message}");
        }
    }

    private async System.Threading.Tasks.Task SafeSetActiveOutput(VivoxOutputDevice dev)
    {
        if (!IsVivoxInitialized()) return;
        try
        {
            await VivoxService.Instance.SetActiveOutputDeviceAsync(dev);
        }
        catch (Exception e)
        {
            SetStatus($"출력 디바이스 변경 실패: {e.Message}");
        }
    }

    private int Map01ToVivox(int v01)
    {
        v01 = Mathf.Clamp(v01, 0, 100);

        if (v01 <= DEFAULT)
        {
            float t = v01 / (float)DEFAULT;
            return Mathf.RoundToInt(Mathf.Lerp(-50, 0, t));
        }
        else
        {
            float t = (v01 - DEFAULT) / (float)(100 - DEFAULT);
            return Mathf.RoundToInt(Mathf.Lerp(0, MAX_BOOST_VIVOX, t));
        }
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;

    }

    private void SetDropdownSingleLabel(TMP_Dropdown dd, string label, bool interactable)
    {
        if (dd == null) return;

        _uiLock = true;
        dd.ClearOptions();
        dd.AddOptions(new List<string> { label });
        dd.value = 0;
        dd.interactable = interactable;
        dd.RefreshShownValue();
        _uiLock = false;
    }
}
