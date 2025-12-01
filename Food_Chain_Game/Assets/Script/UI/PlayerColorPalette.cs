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
        new ColorEntry( 0, "Red",         new Color32(255,  45,  45, 255)),
        new ColorEntry( 1, "Orange",      new Color32(255, 135,  75, 255)),
        new ColorEntry( 2, "Yellow",      new Color32(255, 255,  50, 255)),
        new ColorEntry( 3, "LightGreen",  new Color32(100, 255,  45, 255)),
        new ColorEntry( 4, "Blue",        new Color32( 45,  95, 255, 255)),
        new ColorEntry( 5, "Navy",        new Color32( 80,  75, 230, 255)),
        new ColorEntry( 6, "Lavender",    new Color32(140,  60, 240, 255)),
        new ColorEntry( 7, "Black",       new Color32( 60,  60,  60, 255)),
        new ColorEntry( 8, "White",       new Color32(254, 255, 255, 255)),
        new ColorEntry( 9, "Pink",        new Color32(255,  45, 245, 255)),
        new ColorEntry(10, "Green",       new Color32( 45, 190,  55, 255)),
        new ColorEntry(11, "Mint",        new Color32( 45, 255, 210, 255)),
        new ColorEntry(12, "Gray",        new Color32(180, 180, 180, 255)),
        new ColorEntry(13, "DarkBlue",    new Color32( 10,  50, 235, 255)),
        new ColorEntry(14, "Purple",      new Color32(120,  30, 240, 255)),
        new ColorEntry(15, "LightYellow", new Color32(250, 255, 180, 255)),
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

    public static void InitColorIndex(int index)
    {
        _usedIndices.Add(index);
        colorBtns[index].interactable = false;
    }

    public static void UpdateColorIndex(int oldIndex, int newIndex)
    {
        _usedIndices.Remove(oldIndex);
        colorBtns[oldIndex].interactable = true;

        _usedIndices.Add(newIndex);
        colorBtns[newIndex].interactable = false;
    }

    /// <summary>
    /// 이미 사용 중인 인덱스를 제외하고 랜덤 색 하나 선택.
    /// </summary>
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
