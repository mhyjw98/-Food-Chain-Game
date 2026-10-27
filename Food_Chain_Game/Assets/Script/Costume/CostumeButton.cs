using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 커스터마이징 창의 의상 선택 버튼 하나. CharacterSetting이 의상 목록만큼 복제해 쓴다
public class CostumeButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;

    [SerializeField] private Color normalColor = new Color(0.18f, 0.18f, 0.18f, 1f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.78f, 0.2f, 1f);

    public void Setup(CostumeData data, Action onClick)
    {
        if (nameText != null) nameText.text = data != null ? data.displayName : "";

        Sprite sprite = data != null ? data.ButtonIcon : null;
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (onClick != null) button.onClick.AddListener(() => onClick());
        }
    }

    public void SetSelected(bool selected)
    {
        if (background != null) background.color = selected ? selectedColor : normalColor;
    }
}
