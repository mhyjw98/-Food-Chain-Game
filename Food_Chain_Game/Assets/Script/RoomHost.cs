using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class RoomHost : MonoBehaviour
{
    public string nodeServerUrl = "http://119.70.42.239:3000";

    public void RegisterRoom(string code, string ip)
    {
        StartCoroutine(RegisterRoomToServer(code, ip));
    }

    IEnumerator RegisterRoomToServer(string code, string ip)
    {
        string json = JsonUtility.ToJson(new RoomData { code = code, ip = ip });

        UnityWebRequest req = new UnityWebRequest($"{nodeServerUrl}/register", "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log("방 코드 등록 성공");
        else
            Debug.LogError($"코드 등록 실패: {req.error}");
    }

    [System.Serializable]
    class RoomData { public string code; public string ip; }
}
