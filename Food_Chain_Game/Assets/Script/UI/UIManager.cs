using System;
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

    // ESC로 닫을 수 있는 창 목록(나중에 연 창이 뒤). 창이 열릴 때 RegisterEscape, 실제로 닫힐 때 UnregisterEscape.
    // ESC는 가장 나중에 연 창의 onEscape만 실행한다. onEscape가 null이면 ESC를 막기만 한다(미션 중, 진행 바 등).
    // 채팅·프로필처럼 ESC로 닫지 않을 창은 등록하지 않는다
    private struct EscapeEntry
    {
        public object Key;
        public Action OnEscape;
    }
    private readonly List<EscapeEntry> _escapeStack = new();

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

    // 같은 key로 다시 등록하면 맨 위로 옮긴다(이미 열린 창에 내용만 바뀐 경우 등)
    public static void RegisterEscape(object key, Action onEscape)
    {
        if (Instance == null || key == null) return;

        Instance.RemoveEscape(key);
        Instance._escapeStack.Add(new EscapeEntry { Key = key, OnEscape = onEscape });
    }

    public static void UnregisterEscape(object key)
    {
        if (Instance == null || key == null) return;
        Instance.RemoveEscape(key);
    }

    private void RemoveEscape(object key)
    {
        for (int i = _escapeStack.Count - 1; i >= 0; i--)
        {
            if (Equals(_escapeStack[i].Key, key))
                _escapeStack.RemoveAt(i);
        }
    }

    // 열린 창을 한꺼번에 닫는다(사망 연출 전 등). 위에서부터 각 창의 onEscape를 실행하고, 한 번에 다 닫히지 않는 창
    // (확인창만 먼저 닫히는 창 등)은 다시 실행한다. onEscape가 null인 항목(진행 중 작업의 ESC 차단)은 그 작업을 먼저
    // 취소해 두어야 하며, 여기서는 목록에서만 뺀다. 창이 스스로 빠지지 않아도 멈추도록 횟수를 제한한다
    public void CloseAllEscapable()
    {
        const int maxSteps = 64;
        for (int step = 0; step < maxSteps && _escapeStack.Count > 0; step++)
        {
            int top = _escapeStack.Count - 1;
            EscapeEntry entry = _escapeStack[top];

            if ((entry.Key is UnityEngine.Object unityKey && unityKey == null) || entry.OnEscape == null)
            {
                _escapeStack.RemoveAt(top);
                continue;
            }

            entry.OnEscape.Invoke();
        }
    }

    // ESC 처리: 맨 위 창이 있으면 그 창의 onEscape를 실행하고 true. 없으면 false(설정 창을 열 차례)
    public bool HandleEscape()
    {
        for (int i = _escapeStack.Count - 1; i >= 0; i--)
        {
            EscapeEntry entry = _escapeStack[i];

            // 닫기 처리 없이 파괴된 창(씬 전환, Destroy)은 건너뛰고 지운다
            if (entry.Key is UnityEngine.Object unityKey && unityKey == null)
            {
                _escapeStack.RemoveAt(i);
                continue;
            }

            entry.OnEscape?.Invoke();
            return true;
        }
        return false;
    }
}
