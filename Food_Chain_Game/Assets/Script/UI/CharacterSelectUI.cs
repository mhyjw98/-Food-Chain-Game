using Mirror;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 테스트용 직업 선택(로비). '직업' 버튼 → 동물 목록 → 고르면 게임 시작 때 그 캐릭터로 배정된다.
// 고른 값은 서버(RoomPlayer.requestedCharacter)와 본인만 알고, 다른 플레이어에게는 보내지 않는다.
// RoomManager.CharacterSelectEnabled가 꺼져 있으면(정식 빌드 등) 버튼과 표시를 숨긴다
public class CharacterSelectUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform buttonGroup;
    [SerializeField] private GameObject buttonPrefab;
    [Tooltip("로비 화면의 '직업' 버튼(기능이 꺼져 있으면 숨김)")]
    [SerializeField] private GameObject openButton;
    [Tooltip("선택한 직업을 보여 주는 글자")]
    [SerializeField] private TextMeshProUGUI selectedText;
    [SerializeField] private string selectedFormat = "직업 : {0}";
    [SerializeField] private string randomLabel = "무작위";

    private bool _open;

    private void Start()
    {
        bool enabledForTest = IsFeatureEnabled();
        if (openButton != null) openButton.SetActive(enabledForTest);
        if (selectedText != null) selectedText.gameObject.SetActive(enabledForTest);
        if (panel != null) panel.SetActive(false);

        if (!enabledForTest) return;

        BuildButtons();
        RefreshSelectedText();
    }

    private void OnDestroy()
    {
        // 창을 연 채로 씬이 바뀌어도 이동 잠금이 남지 않게 한다
        if (_open && UIManager.Instance != null) UIManager.Instance.Pop(UIPriority.Modal);
    }

    private static bool IsFeatureEnabled()
    {
        var roomManager = NetworkManager.singleton as RoomManager;
        return roomManager != null && roomManager.CharacterSelectEnabled;
    }

    private void BuildButtons()
    {
        if (buttonGroup == null || buttonPrefab == null) return;

        for (int i = buttonGroup.childCount - 1; i >= 0; i--)
            Destroy(buttonGroup.GetChild(i).gameObject);

        AddButton(randomLabel, string.Empty);

        foreach (AnimalType type in Enum.GetValues(typeof(AnimalType)))
        {
            if (type == AnimalType.None) continue;
            if (!AnimalNameMap.AnimalTypeToName.TryGetValue(type, out string label)) continue;

            string characterName = type.ToString();
            if (!CharacterZoneData.CharacterHomeZone.ContainsKey(characterName)) continue;

            AddButton(label, characterName);
        }
    }

    private void AddButton(string label, string characterName)
    {
        GameObject btnObj = Instantiate(buttonPrefab, buttonGroup);
        var text = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null) text.text = label;

        var button = btnObj.GetComponent<Button>();
        if (button != null) button.onClick.AddListener(() => OnSelect(characterName));
    }

    public void Toggle()
    {
        if (_open) Close();
        else Open();
    }

    public void Open()
    {
        if (_open || panel == null) return;

        _open = true;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        if (UIManager.Instance != null) UIManager.Instance.Push(UIPriority.Modal);
        UIManager.RegisterEscape(panel, Close);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);

        if (!_open) return;
        _open = false;
        if (UIManager.Instance != null) UIManager.Instance.Pop(UIPriority.Modal);
        UIManager.UnregisterEscape(panel);
    }

    private void OnSelect(string characterName)
    {
        RoomSessionData.RequestedCharacter = characterName;

        var localPlayer = NetworkClient.localPlayer != null ? NetworkClient.localPlayer.GetComponent<RoomPlayer>() : null;
        if (localPlayer != null) localPlayer.CmdSetRequestedCharacter(characterName);

        RefreshSelectedText();
        Close();
    }

    private void RefreshSelectedText()
    {
        if (selectedText == null) return;

        string label = randomLabel;
        string selected = RoomSessionData.RequestedCharacter;
        if (!string.IsNullOrEmpty(selected)
            && Enum.TryParse(selected, out AnimalType type)
            && AnimalNameMap.AnimalTypeToName.TryGetValue(type, out string animalName))
        {
            label = animalName;
        }

        selectedText.text = string.Format(selectedFormat, label);
    }
}
