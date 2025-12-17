using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum KeyAction { UP, DOWN, LEFT, RIGHT, INTERACT, KEYCOUNT }
public class KeySetting : MonoBehaviour
{
    public static Dictionary<KeyAction, KeyCode> keys = new Dictionary<KeyAction, KeyCode>();
    KeyCode[] defaultKeys = new KeyCode[] { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.Space };

    public GameObject[] keySettingBtn;
    public TextMeshProUGUI[] keyText;

    public bool HasUnsavedChanges { get; private set; }
    public event Action<bool> OnDirtyChanged;

    private Dictionary<KeyAction, KeyCode> savedSnapshot = new();

    int codeKey = -1;

    private void Awake()
    {
        InitKeySetting();
        CaptureSnapshot();
        MarkDirty(false);
    }

    private void OnGUI()
    {
        Event keyEvent = Event.current;

        if (codeKey != -1)
        {
            SettingManager.isKeySetting = true;
            keySettingBtn[codeKey].GetComponent<Button>().interactable = false;
            if (keyEvent.isKey)
            {
                KeyCode newKey = keyEvent.keyCode;
                HandleKeyChange(newKey);
            }
            else if (keyEvent.type == EventType.MouseDown)
            {
                keySettingBtn[codeKey].GetComponent<Button>().interactable = true;
                codeKey = -1;
                EndKeySetting();
            }
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

        if (existingAction != KeyAction.KEYCOUNT)
        {
            keys[existingAction] = keys[(KeyAction)codeKey];
            UpdateKeyTextFor(existingAction);
        }

        keys[(KeyAction)codeKey] = newKey;
        UpdateKeyTextFor((KeyAction)codeKey);
        MarkDirty(true);

        StartCoroutine(EndKeySetting());
        keySettingBtn[codeKey].GetComponent<Button>().interactable = true;
        codeKey = -1;
    }
    IEnumerator EndKeySetting()
    {
        yield return null;

        SettingManager.isKeySetting = false;
    }
    public void ChangeKey(int num)
    {
        codeKey = num;
    }

    void InitKeySetting()
    {
        foreach (KeyAction keyAction in Enum.GetValues(typeof(KeyAction)))
        {
            if (keyAction == KeyAction.KEYCOUNT) continue;

            string keyString = PlayerPrefs.GetString(keyAction.ToString(), null);
            if (!string.IsNullOrEmpty(keyString))
            {
                KeyCode loadedKey = (KeyCode)Enum.Parse(typeof(KeyCode), keyString);
                keys[keyAction] = loadedKey;
            }
            else
            {
                keys[keyAction] = defaultKeys[(int)keyAction];
            }
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

        label.text = keyString;

        if (key == KeyCode.UpArrow || key == KeyCode.DownArrow ||
            key == KeyCode.LeftArrow || key == KeyCode.RightArrow)
        {
            label.fontSize = 46;
        }
        else
        {
            label.fontSize = 25;
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
        MarkDirty(true);
    }

    public void SaveKeySetting()
    {
        foreach (var entry in keys)
        {
            PlayerPrefs.SetString(entry.Key.ToString(), entry.Value.ToString());
        }

        PlayerPrefs.Save();
        CaptureSnapshot();
    }

    public void CaptureSnapshot()
    {
        savedSnapshot.Clear();
        foreach (var kv in keys)
            savedSnapshot[kv.Key] = kv.Value;

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
        foreach (var kv in keys)
        {
            if (!savedSnapshot.TryGetValue(kv.Key, out var savedValue))
                return true;
            if (savedValue != kv.Value)
                return true;
        }
        return false;
    }

    public void RevertToSnapshot()
    {
        foreach (var kv in savedSnapshot)
        {
            keys[kv.Key] = kv.Value;
        }

        UpdateAllKeyTexts();
        MarkDirty(false);
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
            case KeyCode.Backslash: return "|";
            case KeyCode.UpArrow: return "ก่";
            case KeyCode.DownArrow: return "ก้";
            case KeyCode.LeftArrow: return "ก็";
            case KeyCode.RightArrow: return "กๆ";
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
