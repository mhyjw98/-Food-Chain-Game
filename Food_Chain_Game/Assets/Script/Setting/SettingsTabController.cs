using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public interface ISettingsTab
{
    void OnShow();
    void OnHide();
}
public class SettingsTabController : MonoBehaviour
{
    public enum VisibilityMode
    {
        SetActive,
        CanvasGroup
    }
    [Serializable]
    public class TabEntry
    {
        public string name;
        public Button tabButton;
        public GameObject panelRoot;

        [Header("Optional UI")]
        public GameObject selectedIndicator;

        [Header("Optional Logic")]
        public MonoBehaviour tabLogic;
    }

    [SerializeField] private VisibilityMode visibilityMode = VisibilityMode.SetActive;
    [SerializeField] private TabEntry[] tabs;
    [SerializeField] private int defaultTabIndex = 0;
    [SerializeField] private bool rememberLastTab = true;

    private int _currentIndex = -1;

    void Awake()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int idx = i;
            if (tabs[i].tabButton != null)
            {
                tabs[i].tabButton.onClick.RemoveAllListeners();
                tabs[i].tabButton.onClick.AddListener(() => SelectTab(idx));
            }
        }
    }

    void OnEnable()
    {
        // 설정 UI가 켜질 때 기본 탭(또는 마지막 탭)으로 세팅
        if (!rememberLastTab || _currentIndex < 0)
            SelectTab(Mathf.Clamp(defaultTabIndex, 0, tabs.Length - 1));
        else
            SelectTab(_currentIndex);
    }

    public void SelectTab(int index)
    {
        if (tabs == null || tabs.Length == 0) return;
        if (index < 0 || index >= tabs.Length) return;
        if (_currentIndex == index) return;

        if (_currentIndex >= 0)
            SetTabVisible(_currentIndex, false);

        _currentIndex = index;
        SetTabVisible(_currentIndex, true);
    }

    private void SetTabVisible(int index, bool visible)
    {
        var t = tabs[index];

        // 패널 표시/숨김
        if (t.panelRoot != null)
        {
            if (visibilityMode == VisibilityMode.SetActive)
            {
                t.panelRoot.SetActive(visible);
            }
            else
            {
                var cg = t.panelRoot.GetComponent<CanvasGroup>();
                if (cg == null) cg = t.panelRoot.AddComponent<CanvasGroup>();

                cg.alpha = visible ? 1f : 0f;
                cg.interactable = visible;
                cg.blocksRaycasts = visible;
            }
        }

        // 선택 표시(하이라이트)
        if (t.selectedIndicator != null)
            t.selectedIndicator.SetActive(visible);

        // 선택된 탭 버튼 비활성(선택 상태 표현 용도)
        if (t.tabButton != null)
            t.tabButton.interactable = !visible;

        // 탭 로직 콜백
        if (t.tabLogic is ISettingsTab logic)
        {
            if (visible) logic.OnShow();
            else logic.OnHide();
        }
    }
}
