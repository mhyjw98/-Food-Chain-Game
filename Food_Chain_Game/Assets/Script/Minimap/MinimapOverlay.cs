using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 전체 맵 오버레이를 여닫는다(Map 키, 코너 미니맵 클릭, 오버레이 배경 클릭).
// 이동을 막지 않도록 UIManager의 잠금은 걸지 않는다
public class MinimapOverlay : MonoBehaviour
{
    [Tooltip("여닫을 오버레이 전체(반투명 배경 + 전체 맵)")]
    [SerializeField] private GameObject panel;
    [Tooltip("누르면 오버레이를 여는 버튼(코너 미니맵)")]
    [SerializeField] private Button openButton;
    [Tooltip("누르면 오버레이를 닫는 버튼(오버레이 배경)")]
    [SerializeField] private Button closeButton;

    [Tooltip("오버레이가 열려 있는 동안 숨길 코너 미니맵")]
    [SerializeField] private GameObject cornerMinimap;
    [SerializeField] private bool hideCornerWhileOpen = true;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        SetOpen(false);
    }

    private void Update()
    {
        if (IsTypingText()) return;

        if (Input.GetKeyDown(KeySetting.GetKey(KeyAction.MAP)))
            SetOpen(!IsOpen);
    }

    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);

    private void SetOpen(bool open)
    {
        if (panel == null) return;

        if (panel.activeSelf != open) panel.SetActive(open);

        if (open) UIManager.RegisterEscape(this, Close);
        else UIManager.UnregisterEscape(this);

        if (cornerMinimap != null && hideCornerWhileOpen)
        {
            bool showCorner = !open;
            if (cornerMinimap.activeSelf != showCorner) cornerMinimap.SetActive(showCorner);
        }
    }

    // 채팅 등 글자를 입력하는 중이거나 키 설정 중에는 Map 키로 여닫지 않는다
    private static bool IsTypingText()
    {
        if (ChatInputFocus.IsFocused || SettingManager.isKeySetting) return true;

        var eventSystem = EventSystem.current;
        if (eventSystem == null) return false;

        var selected = eventSystem.currentSelectedGameObject;
        if (selected == null) return false;

        var input = selected.GetComponent<TMP_InputField>();
        return input != null && input.isFocused;
    }
}
