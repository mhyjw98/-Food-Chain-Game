using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RoomSessionData
{
    public static string CurrentRoomCode;
    // 방을 만든 호스트만 갖는 값. Node 서버가 등록 때 발급하며 인원수 보고/삭제/하트비트에 첨부한다
    public static string RoomToken;
    public static int ColorIndex;
    // 테스트용 직업 선택: 로비에서 고른 캐릭터 이름(비어 있으면 무작위). 로비에 다시 들어와도 유지된다
    public static string RequestedCharacter = "";

    // 고른 의상 번호(CostumeCatalog). 방이 바뀌어도, 게임을 다시 켜도 유지되도록 PlayerPrefs에 저장한다
    private const string CostumePrefsKey = "CostumeIndex";
    private static int _costumeIndex = -1;
    public static int CostumeIndex
    {
        get
        {
            if (_costumeIndex < 0) _costumeIndex = PlayerPrefs.GetInt(CostumePrefsKey, 0);
            if (!CostumeCatalog.IsValidIndex(_costumeIndex)) _costumeIndex = 0;
            return _costumeIndex;
        }
        set
        {
            _costumeIndex = Mathf.Max(0, value);
            PlayerPrefs.SetInt(CostumePrefsKey, _costumeIndex);
        }
    }

    public static void Reset()
    {
        CurrentRoomCode = "";
        RoomToken = "";
        ColorIndex = -1;
        RequestedCharacter = "";
    }
}
