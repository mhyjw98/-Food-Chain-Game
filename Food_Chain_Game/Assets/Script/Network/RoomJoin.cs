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

    // 설정은 Awake에서 한 번만 읽지 않고 요청할 때마다 확인한다
    private static string NodeServerUrl => ConfigManager.NodeServerUrl;

    private void Awake()
    {
        if (!ConfigManager.IsValid)
            Debug.LogError("[RoomJoin] ConfigManager.Config.IP가 비어있어 Node 서버와 통신할 수 없습니다. Assets/StreamingAssets/config.json을 확인해주세요.");
    }
    public void CheckRoomBeforeJoin(string code, Action<bool, string> onResult)
    {
        if (!ConfigManager.EnsureValid())
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

        StartCoroutine(GetIpFromCode(code, (ip, port, error) =>
        {
            if (!string.IsNullOrEmpty(error))
            {
                onResult(false, error);
                return;
            }

            if (!ConfigManager.TrySetTransportPort(port))
            {
                onResult(false, "문제가 발생했습니다. 나중에 다시 시도해주세요.");
                return;
            }

            RoomSessionData.CurrentRoomCode = code;

            Debug.Log($"[RoomJoin] 조회된 주소: {ip}:{port}");
            RoomManager.singleton.networkAddress = ip;
            onResult(true, null); // 입장 가능
        }));
    }
    IEnumerator GetIpFromCode(string code, Action<string, int, string> callback)
    {
        string ip = null;
        int port = ConfigManager.GamePort;
        string error = null;

        using (UnityWebRequest request = UnityWebRequest.Get($"{NodeServerUrl}/lookup/{UnityWebRequest.EscapeURL(code)}"))
        {
            request.timeout = requestTimeoutSeconds;
            yield return request.SendWebRequest();

            if (request.responseCode == 200)
            {
                if (!TryParseLookup(request.downloadHandler.text, out ip, out port))
                    error = "문제가 발생했습니다. 나중에 다시 시도해주세요.";
            }
            else if (request.responseCode == 404)
            {
                error = "코드가 유효하지 않습니다.";
            }
            else if (request.responseCode == 429) // 요청 횟수 제한
            {
                error = "요청이 너무 많습니다. 잠시 후 다시 시도해주세요.";
            }
            else if (request.responseCode == 423) // 게임 진행 중(방장이 Node 서버에 알림)
            {
                error = "게임이 진행 중인 방에는 입장할 수 없습니다.";
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

        callback(ip, port, error);
    }

    // 응답: {"address":"...","port":7777}. 포트를 보내지 않던 예전 서버(주소만 텍스트로)도 받아 기본 게임 포트로 접속한다
    private static bool TryParseLookup(string text, out string address, out int port)
    {
        address = null;
        port = ConfigManager.GamePort;

        text = (text ?? string.Empty).Trim();
        if (text.Length == 0) return false;

        if (!text.StartsWith("{"))
        {
            address = text;
            return true;
        }

        try
        {
            var response = JsonUtility.FromJson<LookupResponse>(text);
            if (response == null || string.IsNullOrWhiteSpace(response.address)) return false;

            address = response.address.Trim();
            if (ConfigManager.IsValidPort(response.port)) port = response.port;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    [Serializable]
    class LookupResponse { public string address; public int port; }
}
