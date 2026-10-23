using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class RoomHost : MonoBehaviour
{
    private const string TokenHeader = "X-Room-Token";

    [Header("Node 서버 통신")]
    [Tooltip("Node 서버 응답을 기다리는 최대 시간(초)")]
    [SerializeField] private int requestTimeoutSeconds = 10;
    [Tooltip("방 코드가 이미 사용 중일 때 새 코드로 다시 등록을 시도하는 최대 횟수")]
    [SerializeField] private int maxRegisterAttempts = 5;
    [Tooltip("방이 살아 있음을 Node 서버에 알리는 간격(초). 서버의 방 만료 시간(ROOM_TTL_SECONDS, 기본 60초)보다 충분히 짧아야 한다")]
    [SerializeField] private float heartbeatIntervalSeconds = 20f;

    // 설정은 Awake에서 한 번만 읽지 않고 요청할 때마다 확인한다(ConfigManager.EnsureValid로 다시 읽을 수 있음)
    private static string NodeServerUrl => ConfigManager.NodeServerUrl;
    private static bool IsConfigValid => ConfigManager.IsValid;
    private Coroutine heartbeatCoroutine;
    // 게임 진행 중인지. Node 서버가 이 값을 보고 게임 중인 방의 코드 조회를 바로 거부한다
    private bool inGame;

    private void Awake()
    {
        if (!IsConfigValid)
            Debug.LogError("[RoomHost] ConfigManager.Config.IP가 비어있어 Node 서버와 통신할 수 없습니다. Assets/StreamingAssets/config.json을 확인해주세요.");
    }

    // 새 방 코드를 만들어 Node 서버에 등록한다.
    // 성공하면 방 코드와 서버가 발급한 토큰이 RoomSessionData에 저장된다.
    // 호스트 주소는 보내지 않는다(서버가 이 요청의 실제 접속 주소를 호스트 주소로 저장). 게임 포트는 보낸다(입장자가 이 포트로 접속)
    public void RegisterNewRoom(int maxPlayer, Action<bool, string> onResult)
    {
        if (!ConfigManager.EnsureValid())
        {
            onResult?.Invoke(false, "서버 설정(IP)이 올바르지 않습니다.");
            return;
        }

        StartCoroutine(RegisterNewRoomCoroutine(maxPlayer, onResult));
    }

    IEnumerator RegisterNewRoomCoroutine(int maxPlayer, Action<bool, string> onResult)
    {
        int attempts = Mathf.Max(1, maxRegisterAttempts);

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            string code = CodeManager.GenerateCode();
            string json = JsonUtility.ToJson(new RoomData { code = code, maxPlayer = maxPlayer, port = ConfigManager.HostGamePort });

            using (UnityWebRequest req = CreateJsonPost("/register", json, null))
            {
                yield return req.SendWebRequest();

                // 이미 사용 중인 코드 → 새 코드로 다시 시도
                if (req.responseCode == 409)
                {
                    Debug.LogWarning($"[RoomHost] 방 코드 중복, 다시 시도합니다 ({attempt + 1}/{attempts})");
                    continue;
                }

                if (req.responseCode == 200)
                {
                    string token = ParseToken(req.downloadHandler.text);
                    if (string.IsNullOrEmpty(token))
                    {
                        Debug.LogError($"[RoomHost] 등록 응답에 토큰이 없습니다: {req.downloadHandler.text}");
                        onResult?.Invoke(false, "서버 응답이 올바르지 않습니다.");
                        yield break;
                    }

                    RoomSessionData.CurrentRoomCode = code;
                    RoomSessionData.RoomToken = token;
                    Debug.Log("방 코드 등록 성공");
                    onResult?.Invoke(true, null);
                    yield break;
                }

                Debug.LogError($"코드 등록 실패 {req.error}");
                Debug.LogError($"응답 코드: {req.responseCode}");
                Debug.LogError($"서버 응답: {req.downloadHandler.text}");

                // 응답 코드 0 = 서버에 닿지 못함(연결 실패, 시간 초과), 429 = 요청 횟수 제한
                onResult?.Invoke(false, req.responseCode == 0
                    ? "서버와 연결할 수 없습니다."
                    : req.responseCode == 429
                        ? "요청이 너무 많습니다. 잠시 후 다시 시도해주세요."
                        : "방을 만들지 못했습니다. 나중에 다시 시도해주세요.");
                yield break;
            }
        }

        onResult?.Invoke(false, "방 코드를 만들지 못했습니다. 다시 시도해주세요.");
    }

    public void ComeAndGoing(string code, int delta)
    {
        if (!IsConfigValid) return;

        // 응답을 기다리는 사이 RoomSessionData가 초기화될 수 있으므로 토큰은 지금 값을 잡아 둔다
        string token = RoomSessionData.RoomToken;
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogWarning("[Room] 방 토큰이 없어 출입을 Node 서버에 알리지 못했습니다.");
            return;
        }

        StartCoroutine(ComeAndGoingCoroutine(code, delta, token));
    }
    IEnumerator ComeAndGoingCoroutine(string code, int delta, string token)
    {
        string json = JsonUtility.ToJson(new CAGData { code = code, delta = delta });

        using (UnityWebRequest request = CreateJsonPost("/comeandgoing", json, token))
        {
            yield return request.SendWebRequest();

            if (request.responseCode == 200)
                Debug.Log("[Room] 출입 성공");
            else if (request.responseCode == 404 && delta < 0)
                Debug.Log("[Room] 퇴장 보고: 방이 이미 삭제됨");
            else
                Debug.LogError($"[Room] 출입 실패 (응답 코드: {request.responseCode}, {request.error})");
        }
    }

    public void DeleteRoom(string code)
    {
        StopHeartbeat();

        if (!IsConfigValid) return;

        string token = RoomSessionData.RoomToken;
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(token)) return;

        // 삭제는 서버가 내려가는 중에 호출되고, 그 직후 타이틀 씬이 로드되면서 이 오브젝트가 파괴된다.
        // 코루틴은 오브젝트와 함께 멈추므로 완료 콜백으로 처리해 요청 정리까지 끝나게 한다
        UnityWebRequest request = UnityWebRequest.Delete($"{NodeServerUrl}/delete/{UnityWebRequest.EscapeURL(code)}");
        request.timeout = requestTimeoutSeconds;
        request.SetRequestHeader(TokenHeader, token);

        request.SendWebRequest().completed += _ =>
        {
            // 404 = 마지막 퇴장 보고로 서버가 이미 지운 방
            if (request.responseCode == 200 || request.responseCode == 404)
                Debug.Log("삭제 성공");
            else
                Debug.LogError($"[RoomManager] 방 삭제 실패 (응답 코드: {request.responseCode}, {request.error})");

            request.Dispose();
        };
    }

    // 방이 살아 있음을 주기적으로 알린다. 호스트가 창 닫기·크래시로 삭제 요청 없이 사라지면
    // 하트비트가 끊기고, Node 서버가 일정 시간 뒤 그 방을 지운다
    public void StartHeartbeat()
    {
        StopHeartbeat();
        inGame = false;

        if (!IsConfigValid) return;
        heartbeatCoroutine = StartCoroutine(HeartbeatCoroutine());
    }

    public void StopHeartbeat()
    {
        if (heartbeatCoroutine == null) return;

        StopCoroutine(heartbeatCoroutine);
        heartbeatCoroutine = null;
    }

    private IEnumerator HeartbeatCoroutine()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(1f, heartbeatIntervalSeconds));

            // 방이 닫혔으면 중단
            if (string.IsNullOrEmpty(RoomSessionData.CurrentRoomCode) || string.IsNullOrEmpty(RoomSessionData.RoomToken))
            {
                heartbeatCoroutine = null;
                yield break;
            }

            yield return SendHeartbeat();
        }
    }

    // 게임 시작(true)·로비 복귀(false) 때 부른다. 바로 서버에 알리고, 실패해도 다음 하트비트에 같은 값이 실려 맞춰진다
    public void SetInGame(bool value)
    {
        if (inGame == value) return;
        inGame = value;

        if (!IsConfigValid || !isActiveAndEnabled) return;
        StartCoroutine(SendHeartbeat());
    }

    private IEnumerator SendHeartbeat()
    {
        string code = RoomSessionData.CurrentRoomCode;
        string token = RoomSessionData.RoomToken;
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(token)) yield break;

        string json = JsonUtility.ToJson(new HeartbeatData { code = code, inGame = inGame });

        using (UnityWebRequest request = CreateJsonPost("/heartbeat", json, token))
        {
            yield return request.SendWebRequest();

            if (request.responseCode == 404)
                Debug.LogWarning("[RoomHost] Node 서버에 방이 없습니다(만료 또는 삭제됨). 이 코드로는 새로 입장할 수 없습니다.");
            else if (request.responseCode != 200)
                Debug.LogWarning($"[RoomHost] 하트비트 실패 (응답 코드: {request.responseCode}, {request.error})");
        }
    }

    private UnityWebRequest CreateJsonPost(string path, string json, string token)
    {
        UnityWebRequest request = new UnityWebRequest($"{NodeServerUrl}{path}", "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = requestTimeoutSeconds;
        request.SetRequestHeader("Content-Type", "application/json");

        if (!string.IsNullOrEmpty(token))
            request.SetRequestHeader(TokenHeader, token);

        return request;
    }

    private static string ParseToken(string json)
    {
        try
        {
            RegisterResponse response = JsonUtility.FromJson<RegisterResponse>(json);
            return response != null ? response.token : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    [System.Serializable]
    class RoomData { public string code; public int maxPlayer; public int port; }

    [System.Serializable]
    class CAGData { public string code; public int delta; }

    [System.Serializable]
    class HeartbeatData { public string code; public bool inGame; }

    [System.Serializable]
    class RegisterResponse { public string token; }
}
