using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSetting : MonoBehaviour
{    
    [SerializeField] private Image playerImg;
    private RoomPlayer localRoomPlayer;

    public void SetLocalPlayer(RoomPlayer rp, int index)
    {
        localRoomPlayer = rp;
        Debug.Log("[CharacterSetting] 로컬 RoomPlayer 등록 완료");

        // 색이 아직 정해지지 않았으면(-1) 미리보기는 그대로 둔다
        if (playerImg != null && index >= 0 && index < PlayerColorPalette.Count)
        {
            playerImg.color = PlayerColorPalette.GetByIndex(index).Color;
        }
    }
    public void OnColorButtonClicked(int newIndex)
    {
        if (localRoomPlayer == null) return;
        if (newIndex < 0 || newIndex >= PlayerColorPalette.Count) return;

        int oldIndex = RoomSessionData.ColorIndex;
        PlayerColorPalette.UpdateColorIndex(oldIndex, newIndex);

        localRoomPlayer.CmdSetColor(newIndex);
        if (playerImg != null)
            playerImg.color = PlayerColorPalette.GetByIndex(newIndex).Color;
        RoomSessionData.ColorIndex = newIndex;
    }   
}
