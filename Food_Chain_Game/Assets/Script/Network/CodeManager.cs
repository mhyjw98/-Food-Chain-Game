using UnityEngine;

// 방 코드 생성. 중복 검사는 Node 서버에서(등록 시 409 → RoomHost가 새 코드로 다시 등록)
public static class CodeManager
{
    private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string GenerateCode(int length = 10)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < length; i++)
        {
            sb.Append(chars[Random.Range(0, chars.Length)]);
        }
        return sb.ToString();
    }
}
