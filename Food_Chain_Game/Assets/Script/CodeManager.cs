using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CodeManager : MonoBehaviour
{
    public static CodeManager Instance;
    private Dictionary<string, string> codeToIp = new Dictionary<string, string>();
    private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public string RegisterRoom(string ipAddress)
    {
        string code;
        do
        {
            code = GenerateCode();
        } while (codeToIp.ContainsKey(code));

        codeToIp[code] = ipAddress;
        Debug.Log($"ÄÚµå : {code} IP : {ipAddress}");
        return code;
    }

    public string GetIpFromCode(string code)
    {
        return codeToIp.TryGetValue(code, out var ip) ? ip : null;
    }

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
