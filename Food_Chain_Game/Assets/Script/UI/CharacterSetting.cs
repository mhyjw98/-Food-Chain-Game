using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 로비의 커스터마이징 창: 의상과 색을 고른다. 미리보기에 고른 의상과 색을 함께 보여 준다
public class CharacterSetting : MonoBehaviour
{
    [Header("Preview")]
    [SerializeField] private Image playerImg;
    [Tooltip("의상의 덮는 그림(눈, 무늬 등)을 보여 줄 이미지. playerImg 위에 같은 크기로 둔다")]
    [SerializeField] private Image previewOverlayImg;

    [Header("Costume")]
    [Tooltip("의상 버튼을 만들어 넣을 곳")]
    [SerializeField] private RectTransform costumeButtonRoot;
    [Tooltip("복제할 의상 버튼(꺼 둔 오브젝트)")]
    [SerializeField] private CostumeButton costumeButtonTemplate;

    private RoomPlayer localRoomPlayer;
    private readonly List<CostumeButton> _costumeButtons = new();
    private Sprite _defaultPreviewSprite;
    private Color _previewColor = Color.white;

    private void Awake()
    {
        if (playerImg != null) _defaultPreviewSprite = playerImg.sprite;
        BuildCostumeButtons();
        RefreshPreview();
    }

    private void BuildCostumeButtons()
    {
        if (costumeButtonRoot == null || costumeButtonTemplate == null) return;

        costumeButtonTemplate.gameObject.SetActive(false);

        var catalog = CostumeCatalog.Instance;
        int count = catalog != null ? catalog.Count : 0;

        for (int i = 0; i < count; i++)
        {
            int index = i;
            CostumeButton button = Instantiate(costumeButtonTemplate, costumeButtonRoot);
            button.name = "Costume Btn " + i;
            button.gameObject.SetActive(true);
            button.Setup(CostumeCatalog.Get(i), () => OnCostumeButtonClicked(index));
            _costumeButtons.Add(button);
        }

        RefreshCostumeButtons();
    }

    public void SetLocalPlayer(RoomPlayer rp, int index)
    {
        localRoomPlayer = rp;
        Debug.Log("[CharacterSetting] 로컬 RoomPlayer 등록 완료");

        // 색이 아직 정해지지 않았으면(-1) 미리보기 색은 그대로 둔다
        if (index >= 0 && index < PlayerColorPalette.Count)
            _previewColor = PlayerColorPalette.GetByIndex(index).Color;

        RefreshPreview();
        RefreshCostumeButtons();
    }

    public void OnColorButtonClicked(int newIndex)
    {
        if (localRoomPlayer == null) return;
        if (newIndex < 0 || newIndex >= PlayerColorPalette.Count) return;

        int oldIndex = RoomSessionData.ColorIndex;
        PlayerColorPalette.UpdateColorIndex(oldIndex, newIndex);

        localRoomPlayer.CmdSetColor(newIndex);
        _previewColor = PlayerColorPalette.GetByIndex(newIndex).Color;
        RoomSessionData.ColorIndex = newIndex;

        RefreshPreview();
    }

    public void OnCostumeButtonClicked(int index)
    {
        if (!CostumeCatalog.IsValidIndex(index)) return;

        RoomSessionData.CostumeIndex = index;
        if (localRoomPlayer != null)
            localRoomPlayer.CmdSetCostume(index);

        RefreshPreview();
        RefreshCostumeButtons();
    }

    private void RefreshCostumeButtons()
    {
        int selected = RoomSessionData.CostumeIndex;
        for (int i = 0; i < _costumeButtons.Count; i++)
        {
            if (_costumeButtons[i] != null)
                _costumeButtons[i].SetSelected(i == selected);
        }
    }

    private void RefreshPreview()
    {
        CostumeData costume = CostumeCatalog.Get(RoomSessionData.CostumeIndex);

        if (playerImg != null)
        {
            Sprite body = costume != null && costume.bodySprite != null ? costume.bodySprite : _defaultPreviewSprite;
            playerImg.sprite = body;
            playerImg.preserveAspect = body != null;
            playerImg.color = _previewColor;
        }

        if (previewOverlayImg != null)
        {
            Sprite overlay = costume != null ? costume.overlaySprite : null;
            previewOverlayImg.sprite = overlay;
            previewOverlayImg.preserveAspect = true;
            previewOverlayImg.enabled = overlay != null;
            previewOverlayImg.color = costume != null && costume.tintOverlay ? _previewColor : Color.white;
        }
    }
}
