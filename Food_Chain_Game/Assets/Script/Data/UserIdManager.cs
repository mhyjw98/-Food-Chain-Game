using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using ParrelSync;
#endif
public static class UserIdManager
{
    private static System.Random rng = new System.Random();
    private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    private static string keyPrefix
    {
        get
        {
#if UNITY_EDITOR
            if (ClonesManager.IsClone())
            {
                string cloneName = ClonesManager.GetCurrentProject().name;
                return $"user_id_{cloneName}";
            }
#endif
            return "user_id_main";
        }
    }
    /// <summary>
    /// 고유 UserId를 반환 없으면 생성해서 저장
    /// </summary>
    public static string GenerateUserId()
    {
        if (!PlayerPrefs.HasKey(keyPrefix))
        {
            string newId = GenerateRandomId(10);
            PlayerPrefs.SetString(keyPrefix, newId);
            PlayerPrefs.Save();
        }

        return PlayerPrefs.GetString(keyPrefix);
    }

    private static string GenerateRandomId(int length)
    {
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => chars[rng.Next(chars.Length)]).ToArray());
    }
}
