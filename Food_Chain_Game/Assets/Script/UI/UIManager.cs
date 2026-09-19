using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum UIPriority
{
    None = 0,               // 제한 없음
    Scan = 10,              // 상호작용 불가
    Modal = 100             // 이동, 상호작용 불가
}
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private readonly SortedDictionary<int, int> _activePriorities = new();

    private void Awake()
    {
        Instance = this;
    }

    public void Push(UIPriority priority)
    {
        int p = (int)priority;
        _activePriorities.TryGetValue(p, out int count);
        _activePriorities[p] = count + 1;
    }

    public void Pop(UIPriority priority)
    {
        int p = (int)priority;
        if (!_activePriorities.ContainsKey(p)) return;

        _activePriorities[p]--;
        if (_activePriorities[p] <= 0)
            _activePriorities.Remove(p);
    }

    public int CurrentPriorityValue
    {
        get
        {
            if (_activePriorities.Count == 0)
                return 0;

            return _activePriorities.Keys.Max();
        }
    }

    public bool CanMove()
    {
        return CurrentPriorityValue < 100;
    }

    public bool CanInteract()
    {
        return CurrentPriorityValue < 10;
    }
}
