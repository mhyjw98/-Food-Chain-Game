using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RoomSessionData
{
    public static string CurrentRoomCode;
    public static string PreviousHostId;
    public static int ColorIndex;

    public static void Reset()
    {
        CurrentRoomCode = "";
        PreviousHostId = "";
        ColorIndex = -1;
    }
}
