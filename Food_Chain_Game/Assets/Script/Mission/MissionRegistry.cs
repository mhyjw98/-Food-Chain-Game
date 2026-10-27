using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionRegistry : MonoBehaviour
{
    public static MissionRegistry Instance { get; private set; }

    private readonly List<MissionObject> _all = new();
    private readonly Dictionary<MissionType, List<MissionObject>> _byType = new();

    public int RegisteredCount => _all.Count;
    public IReadOnlyList<MissionObject> All => _all;

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

    // 서버 전용: 미션 종류별로 지금 사용 중인 플레이어(netId). 한 번에 한 명만 사용할 수 있다
    private readonly Dictionary<MissionType, uint> _lockedBy = new();

    public bool IsLockedByOther(MissionType type, uint playerNetId)
    {
        return _lockedBy.TryGetValue(type, out uint owner) && owner != playerNetId;
    }

    public void Lock(MissionType type, uint playerNetId)
    {
        _lockedBy[type] = playerNetId;
    }

    public void Unlock(MissionType type, uint playerNetId)
    {
        if (_lockedBy.TryGetValue(type, out uint owner) && owner == playerNetId)
            _lockedBy.Remove(type);
    }

    // 해당 종류 미션의 최소 소요 시간(초). 등록된 오브젝트가 없으면 0
    public float GetMinDuration(MissionType type)
    {
        if (!_byType.TryGetValue(type, out var list)) return 0f;

        float min = float.MaxValue;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null)
                min = Mathf.Min(min, list[i].MinDurationSeconds);
        }
        return min == float.MaxValue ? 0f : min;
    }

    public bool IsAnyInRange(MissionType type, Vector2 position)
    {
        if (!_byType.TryGetValue(type, out var list)) return false;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].IsInRange(position))
                return true;
        }
        return false;
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
