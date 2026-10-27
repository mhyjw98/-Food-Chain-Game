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

    [Header("Mic Mode (Optional)")]
    [Tooltip("켜면 눌러서 말하기, 끄면 항상 켜짐")]
    [SerializeField] private Toggle pushToTalkToggle;
    [SerializeField] private Toggle micMuteToggle;

    [Header("Mic Test (Optional)")]
    [Tooltip("누르면 마이크 테스트를 시작/중지한다")]
    [SerializeField] private Button micTestButton;
    [SerializeField] private TMP_Text micTestButtonLabel;
    [Tooltip("마이크 입력 크기를 보여 주는 막대(Image Type = Filled)")]
    [SerializeField] private Image micLevelFill;
    [SerializeField] private string micTestStartText = "마이크 테스트";
    [SerializeField] private string micTestStopText = "테스트 중지";
    [SerializeField] private string micTestNoDeviceText = "마이크를 찾을 수 없습니다";
    [SerializeField] private string micTestRunningText = "마이크 테스트 중 (다른 플레이어에게는 들리지 않습니다)";
    [Tooltip("테스트 중 내 목소리를 스피커로 되돌려 들려준다")]
    [SerializeField] private bool micTestLoopback = true;
    [Tooltip("되돌려 들려주는 소리의 지연(초). 너무 작으면 소리가 끊긴다")]
    [SerializeField] private float micTestLoopbackDelay = 0.15f;
    [SerializeField] private int micTestSampleRate = 24000;
    [Tooltip("막대가 비어 보이는 입력 크기(dB)")]
    [SerializeField] private float micLevelMinDb = -50f;
    [Tooltip("막대가 가득 차 보이는 입력 크기(dB)")]
    [SerializeField] private float micLevelMaxDb = 0f;
    [Tooltip("막대가 내려가는 속도(초당 비율)")]
    [SerializeField] private float micLevelFallSpeed = 1.5f;
    [Tooltip("입력 크기를 재는 구간(샘플 수)")]
    [SerializeField] private int micLevelWindow = 512;

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

    private readonly List<VivoxInputDevice> _cachedInputs = new();
    private readonly List<VivoxOutputDevice> _cachedOutputs = new();

    private VoiceManager _hookedVoice;
    private bool _micTesting;
    private string _micTestDevice;
    private AudioClip _micTestClip;
    private AudioSource _micTestSource;
    private Coroutine _micTestPlayCo;
    private float[] _micLevelBuffer;
    private float _micLevel;

    private void Awake()
    {
        SetupSlider(outputSlider);
        SetupSlider(inputSlider);

        SetupDropdownUI(outputDeviceDropdown);
        SetupDropdownUI(inputDeviceDropdown);

        SetDropdownSingleLabel(inputDeviceDropdown, LABEL_SCANNING, interactable: false);
        SetDropdownSingleLabel(outputDeviceDropdown, LABEL_SCANNING, interactable: false);

        HookEvents();
        HookMicEvents();
        if (micTestButton != null) micTestButton.onClick.AddListener(ToggleMicTest);
        RefreshMicTestUI();
        LoadFromPrefsToUI();
    }
    private void OnEnable()
    {
        HookVoiceManager();

        RefreshDeviceDropdownsFromManager();
        TryApplyAllToVivox();
    }

    private void OnDisable()
    {
        StopMicTest();
        UnhookVoiceManager();
    }

    // VoiceManager가 이 창보다 늦게 만들어지는 경우에도 연결되도록 Update에서 다시 확인한다
    private void HookVoiceManager()
    {
        if (_hookedVoice == VoiceManager.Instance) return;

        UnhookVoiceManager();
        if (VoiceManager.Instance == null) return;

        _hookedVoice = VoiceManager.Instance;
        _hookedVoice.OnDeviceListsChanged += OnDeviceListsChanged;
        _hookedVoice.OnMicStateChanged += RefreshMicUI;
        OnDeviceListsChanged();
        RefreshMicUI();
    }

    private void UnhookVoiceManager()
    {
        if (_hookedVoice != null)
        {
            _hookedVoice.OnDeviceListsChanged -= OnDeviceListsChanged;
            _hookedVoice.OnMicStateChanged -= RefreshMicUI;
        }
        _hookedVoice = null;
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
            dd.captionText.textWrappingMode = TextWrappingModes.NoWrap;
        }
        if (dd.itemText != null)
        {
            dd.itemText.overflowMode = TextOverflowModes.Ellipsis;
            dd.itemText.textWrappingMode = TextWrappingModes.NoWrap;
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

    public void SaveVoiceSetting()
    {
        PlayerPrefs.SetInt(PREF_OUTPUT, GetOutput01());
        PlayerPrefs.SetInt(PREF_INPUT, GetInput01());

        PlayerPrefs.SetString(PREF_OUTPUT_DEVICE_ID, GetSelectedOutputDeviceIdSafe());
        PlayerPrefs.SetString(PREF_INPUT_DEVICE_ID, GetSelectedInputDeviceIdSafe());

        PlayerPrefs.Save();
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

        // 테스트 중에 장치를 바꾸면 새 장치로 다시 시작한다
        if (_micTesting)
        {
            StopMicTest();
            StartMicTest();
        }
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

        // 출력 장치 음량만 정한다. 채널 음량까지 같이 낮추면 같은 값이 두 번 적용되고,
        // 방을 나가는 중에는 이미 떠난 채널에 적용하려다 Vivox 에러가 났다
        VivoxService.Instance.SetOutputDeviceVolume(Map01ToVivox(GetOutput01()));
    }

    #region 마이크 테스트

    // 음성 채널에 들어가 있지 않아도(타이틀 등) 쓸 수 있도록, 음성 서비스가 아니라 장치에서 직접 입력을 읽는다
    private void ToggleMicTest()
    {
        if (_micTesting) StopMicTest();
        else StartMicTest();
    }

    private void StartMicTest()
    {
        if (_micTesting) return;

        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            SetStatus(micTestNoDeviceText);
            return;
        }

        _micTestDevice = ResolveUnityMicrophoneName();
        _micTestClip = Microphone.Start(_micTestDevice, true, 1, Mathf.Max(8000, micTestSampleRate));
        if (_micTestClip == null)
        {
            SetStatus(micTestNoDeviceText);
            return;
        }

        _micTesting = true;
        _micLevel = 0f;

        if (VoiceManager.Instance != null)
            VoiceManager.Instance.SetMicTestActive(true);

        if (micTestLoopback)
            _micTestPlayCo = StartCoroutine(CoPlayMicTestLoopback());

        SetStatus(micTestRunningText);
        RefreshMicTestUI();
    }

    private void StopMicTest()
    {
        if (!_micTesting) return;
        _micTesting = false;

        if (_micTestPlayCo != null)
        {
            StopCoroutine(_micTestPlayCo);
            _micTestPlayCo = null;
        }

        if (_micTestSource != null)
        {
            _micTestSource.Stop();
            _micTestSource.clip = null;
        }

        Microphone.End(_micTestDevice);
        _micTestClip = null;
        _micLevel = 0f;

        if (VoiceManager.Instance != null)
            VoiceManager.Instance.SetMicTestActive(false);

        SetStatus(string.Empty);
        RefreshMicTestUI();
    }

    // 설정 창에서 고른 입력 장치와 이름이 같은 장치를 찾는다. 못 찾으면 null(기본 장치)
    private string ResolveUnityMicrophoneName()
    {
        string selected = null;
        if (inputDeviceDropdown != null)
        {
            int idx = inputDeviceDropdown.value;
            if (idx >= 0 && idx < _cachedInputs.Count && _cachedInputs[idx] != null)
                selected = _cachedInputs[idx].DeviceName;
        }

        if (string.IsNullOrEmpty(selected)) return null;

        foreach (var device in Microphone.devices)
        {
            if (device == selected || device.Contains(selected) || selected.Contains(device))
                return device;
        }
        return null;
    }

    // 녹음이 조금 쌓인 뒤에 재생을 시작해야 소리가 끊기지 않는다
    private IEnumerator CoPlayMicTestLoopback()
    {
        int delaySamples = Mathf.RoundToInt(Mathf.Max(0.02f, micTestLoopbackDelay) * _micTestClip.frequency);
        while (_micTesting && Microphone.GetPosition(_micTestDevice) < delaySamples)
            yield return null;

        if (!_micTesting || _micTestClip == null) yield break;

        if (_micTestSource == null)
        {
            _micTestSource = gameObject.AddComponent<AudioSource>();
            _micTestSource.playOnAwake = false;
            _micTestSource.spatialBlend = 0f;
        }

        _micTestSource.clip = _micTestClip;
        _micTestSource.loop = true;
        _micTestSource.volume = Mathf.Clamp01(Mathf.Pow(10f, Map01ToVivox(GetInput01()) / 20f));
        _micTestSource.Play();
        _micTestPlayCo = null;
    }

    private void Update()
    {
        if (_hookedVoice != VoiceManager.Instance) HookVoiceManager();

        if (!_micTesting || _micTestClip == null) return;

        // 설정 창을 닫거나 다른 탭으로 넘어가면 테스트를 끝낸다.
        // (창을 닫을 때 꺼지는 것은 자식 패널이라, 이 컴포넌트의 OnDisable은 불리지 않는다)
        if (micTestButton != null && !micTestButton.gameObject.activeInHierarchy)
        {
            StopMicTest();
            return;
        }

        // 장치가 뽑히면 녹음이 멈춘다
        if (!Microphone.IsRecording(_micTestDevice))
        {
            StopMicTest();
            SetStatus(micTestNoDeviceText);
            return;
        }

        float gainDb = Map01ToVivox(GetInput01());
        if (_micTestSource != null)
            _micTestSource.volume = Mathf.Clamp01(Mathf.Pow(10f, gainDb / 20f));

        float target = Mathf.InverseLerp(micLevelMinDb, micLevelMaxDb, ReadMicLevelDb() + gainDb);

        // 올라갈 때는 바로, 내려갈 때는 천천히
        _micLevel = target >= _micLevel ? target : Mathf.Max(target, _micLevel - micLevelFallSpeed * Time.unscaledDeltaTime);

        if (micLevelFill != null) micLevelFill.fillAmount = _micLevel;
    }

    // 가장 최근 구간의 입력 크기(dB). 소리가 없으면 micLevelMinDb보다 작은 값
    private float ReadMicLevelDb()
    {
        int window = Mathf.Clamp(micLevelWindow, 64, _micTestClip.samples);
        if (_micLevelBuffer == null || _micLevelBuffer.Length != window)
            _micLevelBuffer = new float[window];

        int position = Microphone.GetPosition(_micTestDevice);
        int start = position - window;
        if (start < 0) start += _micTestClip.samples;

        // 끝을 넘어가는 구간은 처음으로 이어서 읽힌다
        if (!_micTestClip.GetData(_micLevelBuffer, start)) return -100f;

        float sum = 0f;
        for (int i = 0; i < _micLevelBuffer.Length; i++)
            sum += _micLevelBuffer[i] * _micLevelBuffer[i];

        float rms = Mathf.Sqrt(sum / _micLevelBuffer.Length);
        return rms > 0.00001f ? 20f * Mathf.Log10(rms) : -100f;
    }

    private void RefreshMicTestUI()
    {
        if (micTestButtonLabel != null)
            micTestButtonLabel.text = _micTesting ? micTestStopText : micTestStartText;

        if (micLevelFill != null && !_micTesting)
            micLevelFill.fillAmount = 0f;
    }

    #endregion

    // 마이크 방식/음소거 UI(선택 사항): VoiceManager의 상태를 보여 주고 바꾼다
    private void HookMicEvents()
    {
        if (pushToTalkToggle != null)
            pushToTalkToggle.onValueChanged.AddListener(isOn =>
            {
                if (_uiLock || VoiceManager.Instance == null) return;
                VoiceManager.Instance.SetMicMode(isOn ? VoiceManager.MicMode.PushToTalk : VoiceManager.MicMode.Open);
            });

        if (micMuteToggle != null)
            micMuteToggle.onValueChanged.AddListener(isOn =>
            {
                if (_uiLock || VoiceManager.Instance == null) return;
                VoiceManager.Instance.SetMicMuted(isOn);
            });
    }

    private void RefreshMicUI()
    {
        if (VoiceManager.Instance == null) return;

        _uiLock = true;
        if (pushToTalkToggle != null) pushToTalkToggle.isOn = VoiceManager.Instance.Mode == VoiceManager.MicMode.PushToTalk;
        if (micMuteToggle != null) micMuteToggle.isOn = VoiceManager.Instance.IsMicMuted;
        _uiLock = false;
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
