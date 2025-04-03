using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameRoomUI : MonoBehaviour
{
    public TextMeshProUGUI roomCodeText;

    void Start()
    {
        roomCodeText.text = $"CODE : {RoomSessionData.CurrentRoomCode}";
    }
}
