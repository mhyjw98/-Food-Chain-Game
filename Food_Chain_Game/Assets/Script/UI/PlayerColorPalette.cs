using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class PlayerColorPalette : MonoBehaviour
{
    [SerializeField] private Button[] colorButtonsInScene;
    public static Button[] colorBtns;
    public struct ColorEntry
    {
        public int Index;
        public string Name;
        public Color32 Color;

        public ColorEntry(int index, string name, Color32 color)
        {
            Index = index;
            Name = name;
            Color = color;
        }
    }

    // 실제 색 데이터 배열
    private static readonly ColorEntry[] _colors =
    {
        new ColorEntry( 0, "Lavender",    new Color32(138,  43, 226, 255)),
        new ColorEntry( 1, "LightYellow", new Color32(250, 255, 180, 255)),
        new ColorEntry( 2, "Navy",        new Color32(  0,   0, 128, 255)),
        new ColorEntry( 3, "Pink",        new Color32(255,   0, 140, 255)),
        new ColorEntry( 4, "Mint",        new Color32( 40, 225, 255, 255)),
        new ColorEntry( 5, "LightGreen",  new Color32( 84, 184,   0, 255)),
        new ColorEntry( 6, "Red",         new Color32(255,  45,  45, 255)),
        new ColorEntry( 7, "Black",       new Color32( 60,  60,  60, 255)),
        new ColorEntry( 8, "White",       new Color32(254, 255, 255, 255)),
        new ColorEntry( 9, "Blue",        new Color32( 45,  95, 255, 255)),
        new ColorEntry(10, "Green",       new Color32( 45, 190,  55, 255)),
        new ColorEntry(11, "Orange",      new Color32(255, 135,  75, 255)),
        new ColorEntry(12, "Gray",        new Color32(180, 180, 180, 255)),
        new ColorEntry(13, "DarkBlue",    new Color32( 10,  50, 235, 255)),
        new ColorEntry(14, "Purple",      new Color32(120,  30, 240, 255)),
        new ColorEntry(15, "Yellow",      new Color32(255, 255,  50, 255)),
    };

    private static readonly Dictionary<string, int> _nameToIndex =
        BuildNameToIndex();

    private static readonly HashSet<int> _usedIndices = new HashSet<int>();
    private static Dictionary<string, int> BuildNameToIndex()
    {
        var dict = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var e in _colors)
            dict[e.Name] = e.Index;
        return dict;
    }

    public static int Count => _colors.Length;

    private void Awake()
    {
        colorBtns = colorButtonsInScene;

        InitColorBtn();
    }

    // 게임이 끝나고 로비로 돌아오면 버튼은 새로 만들어지지만 RoomPlayer(와 그 색)는 그대로다.
    // 색이 바뀔 때만 버튼을 갱신하면 복귀 직후에는 사용 중인 색도 눌리므로, 씬이 열릴 때 한 번 맞춘다
    private void Start()
    {
        SyncButtonsWithPlayers();
    }

    public static void SyncButtonsWithPlayers()
    {
        if (colorBtns == null || colorBtns.Length == 0) return;

        for (int i = 0; i < colorBtns.Length; i++)
            SetButtonInteractable(i, true);

        foreach (var p in FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None))
            SetButtonInteractable(p.colorIndex, false);
    }

    // 타이틀로 돌아오면 이전 방의 색 사용 기록은 의미가 없다(다음 방에서 고를 수 있는 색이 줄어드는 것을 막는다)
    public static void ResetUsedColors()
    {
        _usedIndices.Clear();
    }

    private void InitColorBtn()
    {
        if (colorBtns == null)        
            return;
        

        int count = Mathf.Min(colorBtns.Length, _colors.Length);

        for (int i = 0; i < count; i++)
        {
            var img = colorBtns[i].GetComponent<Image>();
            if (img != null)
                img.color = _colors[i].Color;
        }
    }
    public static ColorEntry GetByIndex(int index)
    {
        if (index < 0 || index >= _colors.Length)
        {
            Debug.Log("index : " + index);
            throw new System.IndexOutOfRangeException($"Color index {index} out of range");
        }
            
        return _colors[index];
    }

    public static bool TryGetByName(string name, out ColorEntry entry)
    {
        entry = default;
        if (string.IsNullOrEmpty(name)) return false;

        if (_nameToIndex.TryGetValue(name, out int idx))
        {
            entry = _colors[idx];
            return true;
        }
        return false;
    }

    public static Color32 GetColorByIndex(int index)
        => GetByIndex(index).Color;

    public static int GetIndexByName(string name)
    {
        if (_nameToIndex.TryGetValue(name, out int idx))
            return idx;
        return -1;
    }

    // colorBtns는 로비 씬의 버튼을 가리키는 static 배열이라, 게임 씬에서는 파괴된 버튼을 가리킨다.
    // 색상 SyncVar 훅은 게임 중(플레이어 퇴장 등)에도 불리므로 버튼 접근은 전부 이 메서드를 거친다
    private static void SetButtonInteractable(int index, bool interactable)
    {
        if (colorBtns == null || index < 0 || index >= colorBtns.Length) return;
        if (colorBtns[index] == null) return;

        colorBtns[index].interactable = interactable;
    }

    public static void InitColorIndex(int index)
    {
        _usedIndices.Add(index);
        SetButtonInteractable(index, false);
    }

    public static void UpdateColorIndex(int oldIndex, int newIndex)
    {
        if (oldIndex >= 0 && oldIndex < Count)
        {
            _usedIndices.Remove(oldIndex);
            SetButtonInteractable(oldIndex, true);
        }
        if (newIndex >= 0 && newIndex < Count)
        {
            _usedIndices.Add(newIndex);
            SetButtonInteractable(newIndex, false);
        }           
    }

    public static void OnUnSelectColor(int index)
    {
        if (colorBtns == null || index < 0 || index >= colorBtns.Length) return;

        _usedIndices.Remove(index);
        if (colorBtns[index] != null)
            colorBtns[index].interactable = true;
    }
    public static void RefreshButtons(int oldIndex, int newIndex)
    {
        if (colorBtns == null) return;

        if(newIndex == -1)
        {
            SetButtonInteractable(oldIndex, true);
        }
        else
        {
            for (int i = 0; i < colorBtns.Length; i++)
                SetButtonInteractable(i, true);

            foreach (var p in FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None))
                SetButtonInteractable(p.colorIndex, false);
        }        
    }
    public static ColorEntry GetRandomExcluding()
    {
        var candidates = new List<int>();

        for (int i = 0; i < _colors.Length; i++)
        {
            if (!_usedIndices.Contains(i))
                candidates.Add(i);
        }

        int chosenIndex;

        if (candidates.Count > 0)
        {
            chosenIndex = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }
        else
        {
            chosenIndex = UnityEngine.Random.Range(0, _colors.Length);
        }

        _usedIndices.Add(chosenIndex);
        return _colors[chosenIndex];
    }

    public static int GetRandomExcludingIndex()
    {
        var candidates = new List<int>();

        for (int i = 0; i < _colors.Length; i++)
        {
            if (!_usedIndices.Contains(i))
                candidates.Add(i);
        }

        int chosenIndex;

        if (candidates.Count > 0)
        {
            chosenIndex = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }
        else
        {
            chosenIndex = UnityEngine.Random.Range(0, _colors.Length);
        }

        _usedIndices.Add(chosenIndex);
        return chosenIndex;
    }
}
