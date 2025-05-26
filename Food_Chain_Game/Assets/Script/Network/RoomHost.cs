using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class RoomHost : MonoBehaviour
{
    public string nodeServerUrl = "http://119.70.42.239:3000";

    public void RegisterRoom(string code, string ip, int maxPlayer)
    {
        StartCoroutine(RegisterRoomToServer(code, ip, maxPlayer));
    }

    IEnumerator RegisterRoomToServer(string code, string ip, int maxPlayer)
    {
        string json = JsonUtility.ToJson(new RoomData { code = code, ip = ip, maxPlayer = maxPlayer });

        UnityWebRequest req = new UnityWebRequest($"{nodeServerUrl}/register", "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.responseCode == 200)
            Debug.Log("�� �ڵ� ��� ����");
        else
            Debug.LogError($"�ڵ� ��� ����: {req.error}");
    }
    public void ComeAndGoing(string code, int delta)
    {
        StartCoroutine(ComeAndGoingCoroutine(code, delta));
    }
    IEnumerator ComeAndGoingCoroutine(string code, int delta)
    {
        string json = JsonUtility.ToJson(new CAGData { code = code, delta = delta});

        UnityWebRequest request = new UnityWebRequest($"{nodeServerUrl}/comeandgoing", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.responseCode == 200)
            Debug.Log("[Room] ���� ����");
        else
            Debug.LogError("[Room] ���� ����");
    }
    public void DeleteRoom(string code)
    {
        StartCoroutine(DeleteRoomCoroutine(code));
    }

    private IEnumerator DeleteRoomCoroutine(string code)
    {
        UnityWebRequest request = UnityWebRequest.Get($"{nodeServerUrl}/delete/{code}");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            Debug.Log("���� ����");
        else
            Debug.LogError($"[RoomManager] �� ���� ����: {request.error}");
    }

    [System.Serializable]
    class RoomData { public string code; public string ip; public int maxPlayer; }

    [System.Serializable]
    class CAGData { public string code; public int delta; }
}
