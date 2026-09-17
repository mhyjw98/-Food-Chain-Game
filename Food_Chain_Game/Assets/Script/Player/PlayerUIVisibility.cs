using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUIVisibility : MonoBehaviour
{
    [SerializeField] private Canvas nicknameCanvas;
    [SerializeField] private Canvas bubbleCanvas;

    private bool _visible = true;
    public void SetVisible(bool visible)
    {
        if (_visible == visible) return;
        _visible = visible;

        if (nicknameCanvas) nicknameCanvas.enabled = visible;
        if (bubbleCanvas) bubbleCanvas.enabled = visible;
    }
}
