using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 새 동작은 KEYCOUNT 바로 앞에 추가한다(번호가 버튼의 ChangeKey 인자·저장 순서와 묶여 있다)
public enum KeyAction { UP, DOWN, LEFT, RIGHT, INTERACT, VOICE_TALK, VOICE_MUTE, MAP, KEYCOUNT }
public class KeySetting : MonoBehaviour
{
    public static Dictionary<KeyAction, KeyCode> keys = new Dictionary<KeyAction, KeyCode>();
    static readonly KeyCode[] defaultKeys = new KeyCode[] { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.Space, KeyCode.V, KeyCode.M, KeyCode.Tab };

    // 설정 창이 아직 만들어지지 않은 씬에서도 저장된 키를 읽을 수 있게 한다(음성 키는 타이틀에서도 쓰인다)
    public static KeyCode GetKey(KeyAction action)
    {
        if (!keys.TryGetValue(action, out var key))
        {
            key = LoadKey(action);
            keys[action] = key;
        }
        return key;
    }

    // 저장값이 없거나 읽을 수 없으면(손상/이전 버전) 기본 키를 쓴다
    private static KeyCode LoadKey(KeyAction action)
    {
        string keyString = PlayerPrefs.GetString(action.ToString(), null);
        if (!string.IsNullOrEmpty(keyString)
            && Enum.TryParse(keyString, out KeyCode loadedKey)
            && loadedKey != KeyCode.None && loadedKey != KeyCode.Escape)
        {
            return loadedKey;
        }

        return defaultKeys[(int)action];
    }

    public GameObject[] keySettingBtn;
    public TextMeshProUGUI[] keyText;

    int codeKey = -1;

    [Header("Key Label")]
    [SerializeField] private float arrowKeyFontSize = 46f;
    [SerializeField] private float normalKeyFontSize = 25f;

    private Button[] _keyButtons;

    private void Awake()
    {
        _keyButtons = new Button[keySettingBtn != null ? keySettingBtn.Length : 0];
        for (int i = 0; i < _keyButtons.Length; i++)
            _keyButtons[i] = keySettingBtn[i] != null ? keySettingBtn[i].GetComponent<Button>() : null;

        InitKeySetting();
    }

    // 키 입력을 기다리는 중에 설정 창이 꺼지거나 씬이 바뀌면 대기 상태가 남지 않게 정리한다.
    // isKeySetting은 static이라 그대로 두면 이후 ESC가 계속 무시된다
    private void OnDisable()
    {
        if (codeKey != -1)
        {
            SetKeyButtonInteractable(codeKey, true);
            codeKey = -1;
        }
        SettingManager.isKeySetting = false;
    }

    private void OnGUI()
    {
        if (codeKey == -1) return;

        Event keyEvent = Event.current;

        if (keyEvent.type == EventType.KeyDown)
        {
            // 글자 입력 이벤트는 keyCode가 None으로 한 번 더 온다
            if (keyEvent.keyCode == KeyCode.None) return;

            // ESC는 메뉴 키라서 지정할 수 없다: 입력 대기만 취소
            if (keyEvent.keyCode == KeyCode.Escape)
            {
                CancelKeyChange();
                keyEvent.Use();
                return;
            }

            HandleKeyChange(keyEvent.keyCode);
            keyEvent.Use();
        }
        else if (keyEvent.type == EventType.MouseDown)
        {
            CancelKeyChange();
        }
    }
    private void HandleKeyChange(KeyCode newKey)
    {
        KeyAction existingAction = KeyAction.KEYCOUNT;

        foreach (var entry in keys)
        {
            if (entry.Value == newKey)
            {
                existingAction = entry.Key;
                break;
            }
        }

        // 다른 동작이 쓰던 키면 서로 맞바꾼다
        if (existingAction != KeyAction.KEYCOUNT)
        {
            keys[existingAction] = keys[(KeyAction)codeKey];
            UpdateKeyTextFor(existingAction);
        }

        keys[(KeyAction)codeKey] = newKey;
        UpdateKeyTextFor((KeyAction)codeKey);

        FinishKeyChange();
    }

    private void CancelKeyChange()
    {
        FinishKeyChange();
    }

    private void FinishKeyChange()
    {
        SetKeyButtonInteractable(codeKey, true);
        codeKey = -1;

        // 같은 프레임의 ESC/키 입력이 설정 창에 전달되지 않도록 한 프레임 뒤에 해제한다
        if (isActiveAndEnabled)
            StartCoroutine(EndKeySetting());
        else
            SettingManager.isKeySetting = false;
    }

    private void SetKeyButtonInteractable(int index, bool interactable)
    {
        if (_keyButtons == null || index < 0 || index >= _keyButtons.Length) return;
        if (_keyButtons[index] != null)
            _keyButtons[index].interactable = interactable;
    }

    IEnumerator EndKeySetting()
    {
        yield return null;

        if (codeKey == -1)
            SettingManager.isKeySetting = false;
    }
    public void ChangeKey(int num)
    {
        if (num < 0 || num >= (int)KeyAction.KEYCOUNT) return;

        // 다른 키를 기다리던 중이면 그쪽 버튼을 되돌린다
        if (codeKey != -1)
            SetKeyButtonInteractable(codeKey, true);

        codeKey = num;
        SettingManager.isKeySetting = true;
        SetKeyButtonInteractable(codeKey, false);
    }

    void InitKeySetting()
    {
        foreach (KeyAction keyAction in Enum.GetValues(typeof(KeyAction)))
        {
            if (keyAction == KeyAction.KEYCOUNT) continue;

            keys[keyAction] = LoadKey(keyAction);
        }
        UpdateAllKeyTexts();
    }

    void UpdateAllKeyTexts()
    {
        for (int index = 0; index < keyText.Length; index++)
        {
            UpdateKeyTextFor((KeyAction)index);
        }
    }

    void UpdateKeyTextFor(KeyAction action)
    {
        int index = (int)action;
        if (index < 0 || index >= keyText.Length) return;
        if (!keys.TryGetValue(action, out var key)) return;

        string keyString = key.ToUserFriendlyString();
        var label = keyText[index];
        if (label == null) return;

        label.text = keyString;

        if (key == KeyCode.UpArrow || key == KeyCode.DownArrow ||
            key == KeyCode.LeftArrow || key == KeyCode.RightArrow)
        {
            label.fontSize = arrowKeyFontSize;
        }
        else
        {
            label.fontSize = normalKeyFontSize;
        }
    }
    public void ResetKeySetting()
    {
        foreach (KeyAction keyAction in Enum.GetValues(typeof(KeyAction)))
        {
            if (keyAction == KeyAction.KEYCOUNT) continue;

            keys[keyAction] = defaultKeys[(int)keyAction];            
        }
        UpdateAllKeyTexts();
    }

    public void SaveKeySetting()
    {
        foreach (var entry in keys)
        {
            PlayerPrefs.SetString(entry.Key.ToString(), entry.Value.ToString());
        }

        PlayerPrefs.Save();
    }

}
public static class KeyCodeExtensions
{
    public static string ToUserFriendlyString(this KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.Alpha0: return "0";
            case KeyCode.Alpha1: return "1";
            case KeyCode.Alpha2: return "2";
            case KeyCode.Alpha3: return "3";
            case KeyCode.Alpha4: return "4";
            case KeyCode.Alpha5: return "5";
            case KeyCode.Alpha6: return "6";
            case KeyCode.Alpha7: return "7";
            case KeyCode.Alpha8: return "8";
            case KeyCode.Alpha9: return "9";
            case KeyCode.Space: return "SpaceBar";
            case KeyCode.Return: return "Enter";
            case KeyCode.BackQuote: return "`";
            case KeyCode.LeftControl: return "Left Ctrl";
            case KeyCode.RightControl: return "Right Ctrl";
            case KeyCode.Backslash: return "\\";
            case KeyCode.UpArrow: return "↑";
            case KeyCode.DownArrow: return "↓";
            case KeyCode.LeftArrow: return "←";
            case KeyCode.RightArrow: return "→";
            case KeyCode.LeftBracket: return "[";
            case KeyCode.RightBracket: return "]";
            case KeyCode.Minus: return "-";
            case KeyCode.Equals: return "=";
            case KeyCode.Semicolon: return ";";
            case KeyCode.Quote: return "'";
            case KeyCode.Comma: return ",";
            case KeyCode.Period: return ".";
            case KeyCode.Slash: return "/";

            default: return keyCode.ToString();
        }
    }
}
