using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class RoomJoin : MonoBehaviour
{
    // 방 코드 형식: 영문 대문자·숫자 10자 (CodeManager.GenerateCode, Node 서버와 동일)
    private static readonly Regex CodePattern = new Regex("^[A-Z0-9]{10}$");

    [Tooltip("Node 서버 응답을 기다리는 최대 시간(초)")]
    [SerializeField] private int requestTimeoutSeconds = 10;

    private string nodeServerUrl;
    private bool isConfigValid;

    private void Awake()
    {
        isConfigValid = !string.IsNullOrWhiteSpace(ConfigManager.Config.IP);
        nodeServerUrl = "http://" + ConfigManager.Config.IP + ":3000";

        if (!isConfigValid)
            Debug.LogError("[RoomJoin] ConfigManager.Config.IP가 비어있어 Node 서버와 통신할 수 없습니다. Assets/StreamingAssets/config.json을 확인해주세요.");
    }
    public void CheckRoomBeforeJoin(string code, Action<bool, string> onResult)
    {
        if (!isConfigValid)
        {
            onResult(false, "서버 설정(IP)이 올바르지 않습니다.");
            return;
        }

        // 앞뒤 공백·소문자 입력은 보정하고, 형식이 다르면 서버에 묻지 않는다
        code = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (!CodePattern.IsMatch(code))
        {
            onResult(false, "코드가 유효하지 않습니다.");
            return;
        }

        StartCoroutine(GetIpFromCode(code, (ip, error) =>
        {
            if (!string.IsNullOrEmpty(error))
            {
                onResult(false, error);
                return;
            }

            RoomSessionData.CurrentRoomCode = code;

            Debug.Log($"[RoomJoin] 조회된 IP: {ip}");
            RoomManager.singleton.networkAddress = ip;
            onResult(true, null); // 입장 가능
        }));
    }
    IEnumerator GetIpFromCode(string code, Action<string, string> callback)
    {
        string ip = null;
        string error = null;

        using (UnityWebRequest request = UnityWebRequest.Get($"{nodeServerUrl}/lookup/{UnityWebRequest.EscapeURL(code)}"))
        {
            request.timeout = requestTimeoutSeconds;
            yield return request.SendWebRequest();

            if (request.responseCode == 200)
            {
                ip = request.downloadHandler.text.Trim();
                if (string.IsNullOrEmpty(ip))
                    error = "문제가 발생했습니다. 나중에 다시 시도해주세요.";
            }
            else if (request.responseCode == 404)
            {
                error = "코드가 유효하지 않습니다.";
            }
            else if (request.responseCode == 403) // 인원수 초과
            {
                error = "방 인원이 가득 찼습니다.";
            }
            else if (request.responseCode == 0) // 서버에 닿지 못함(연결 실패, 시간 초과)
            {
                error = "서버와 연결할 수 없습니다.";
            }
            else
            {
                error = "문제가 발생했습니다. 나중에 다시 시도해주세요.";
            }
        }

        callback(ip, error);
    }
}
