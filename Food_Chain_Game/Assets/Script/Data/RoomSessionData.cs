using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RoomSessionData
{
    public static string CurrentRoomCode;
    // 방을 만든 호스트만 갖는 값. Node 서버가 등록 때 발급하며 인원수 보고/삭제/하트비트에 첨부한다
    public static string RoomToken;
    public static int ColorIndex;

    public static void Reset()
    {
        CurrentRoomCode = "";
        RoomToken = "";
        ColorIndex = -1;
    }
}
