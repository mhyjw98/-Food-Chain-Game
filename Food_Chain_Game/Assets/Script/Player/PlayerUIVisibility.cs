using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUIVisibility : MonoBehaviour
{
    [SerializeField] private Canvas nicknameCanvas;
    [SerializeField] private Canvas bubbleCanvas;

    private bool _visible = true;
    // 지금 내 시야 안에 있어 머리 위 UI가 보이는 상태인지
    public bool IsVisible => _visible;
    public void SetVisible(bool visible)
    {
        if (_visible == visible) return;
        _visible = visible;

        if (nicknameCanvas) nicknameCanvas.enabled = visible;
        if (bubbleCanvas) bubbleCanvas.enabled = visible;
    }
}
