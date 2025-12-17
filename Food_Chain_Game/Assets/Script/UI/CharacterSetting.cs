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

        if (playerImg != null)
        {
            playerImg.color = PlayerColorPalette.GetByIndex(index).Color;
        }
    }
    public void OnColorButtonClicked(int newIndex)
    {
        if (localRoomPlayer == null) return;

        int oldIndex = RoomSessionData.ColorIndex;
        PlayerColorPalette.UpdateColorIndex(oldIndex, newIndex);

        localRoomPlayer.CmdSetColor(newIndex);
        playerImg.color = PlayerColorPalette.GetByIndex(newIndex).Color;
        RoomSessionData.ColorIndex = newIndex;
    }   
}
