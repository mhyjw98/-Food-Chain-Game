using System.Collections;
using System.Collections.Generic;
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
            // 이동 가능 여부 확인으로 매 프레임 불리므로 LINQ 없이 구한다(키는 오름차순이라 마지막이 최댓값)
            int max = 0;
            foreach (var pair in _activePriorities)
                max = pair.Key;

            return max;
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
