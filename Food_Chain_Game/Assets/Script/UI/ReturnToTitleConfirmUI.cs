using System;
using TMPro;
using UnityEngine;

// "타이틀 화면으로 돌아가시겠습니까?" 확인창. 방장 여부와 무관하게 방을 나가기 전에 한 번 묻는다.
// 씬에 하나만 두고(로비/게임 씬), 나가기 버튼들은 TryShow로 띄운다. 씬에 없으면 TryShow가 false를 돌려준다
public class ReturnToTitleConfirmUI : MonoBehaviour
{
    public static ReturnToTitleConfirmUI Instance { get; private set; }

    [SerializeField] private GameObject window;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private string message = "타이틀 화면으로 돌아가시겠습니까?";

    private Action _onConfirm;
    private bool _pushedModal;

    public bool IsOpen => window != null && window.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (window != null) window.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // 확인창을 띄웠으면 true. 확인을 누르면 onConfirm이 실행된다
    public static bool TryShow(Action onConfirm)
    {
        if (Instance == null || Instance.window == null) return false;

        Instance.Show(onConfirm);
        return true;
    }

    private void Show(Action onConfirm)
    {
        _onConfirm = onConfirm;

        if (messageText != null) messageText.text = message;
        if (IsOpen) return;

        window.SetActive(true);

        // 확인창이 떠 있는 동안 이동/상호작용을 막는다
        if (UIManager.Instance != null)
        {
            UIManager.Instance.Push(UIPriority.Modal);
            _pushedModal = true;
        }
    }

    public void OnClickConfirm()
    {
        Action onConfirm = _onConfirm;
        Close();
        onConfirm?.Invoke();
    }

    public void OnClickCancel()
    {
        Close();
    }

    private void Close()
    {
        _onConfirm = null;
        if (window != null) window.SetActive(false);

        if (_pushedModal)
        {
            _pushedModal = false;
            if (UIManager.Instance != null) UIManager.Instance.Pop(UIPriority.Modal);
        }
    }
}
