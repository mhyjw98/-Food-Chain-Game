using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RoomSessionData
{
    public static string CurrentRoomCode;
    public static int ColorIndex;

    public static void Reset()
    {
        CurrentRoomCode = "";
        ColorIndex = -1;
    }
}
