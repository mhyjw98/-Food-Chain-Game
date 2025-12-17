using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionRegistry : MonoBehaviour
{
    public static MissionRegistry Instance { get; private set; }

    private readonly List<MissionObject> _all = new();
    private readonly Dictionary<MissionType, List<MissionObject>> _byType = new();

    public int RegisteredCount => _all.Count;

    private void Awake()
    {
        Instance = this;
    }

    public void Register(MissionObject obj)
    {
        if (obj == null) return;
        _all.Add(obj);

        if (!_byType.TryGetValue(obj.MissionType, out var list))
        {
            list = new List<MissionObject>();
            _byType.Add(obj.MissionType, list);
        }
        list.Add(obj);
    }

    public void DisableAll()
    {
        for (int i = 0; i < _all.Count; i++)
            _all[i].SetEnabled(false);
    }

    public void DisableType(MissionType type)
    {
        if (!_byType.TryGetValue(type, out var list)) return;

        for (int i = 0; i < list.Count; i++)
            list[i].SetEnabled(false);
    }

    public void EnableOnly(IReadOnlyCollection<MissionType> types)
    {
        DisableAll();

        foreach (var t in types)
        {
            if (!_byType.TryGetValue(t, out var list)) continue;

            for (int i = 0; i < list.Count; i++)
                list[i].SetEnabled(true);
        }
    }
}
