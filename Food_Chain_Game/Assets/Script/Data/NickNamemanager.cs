using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NickNamemanager
{
    private const string NicknameKey = "PlayerNickname";

    public static string GetNickname()
    {
        return PlayerPrefs.GetString(NicknameKey, "");
    }

    public static void SetNickname(string nickname)
    {
        PlayerPrefs.SetString(NicknameKey, nickname);
        PlayerPrefs.Save();
    }

    public static bool HasNickname()
    {
        return !string.IsNullOrEmpty(GetNickname());
    }
}
