using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class RoomJoin : MonoBehaviour
{
    private string nodeServerUrl;

    private void Awake()
    {
        nodeServerUrl = "http://" + ConfigManager.Config.IP + ":3000";
    }
    public void CheckRoomBeforeJoin(string code, Action<bool, string> onResult)
    {
        StartCoroutine(GetIpFromCode(code, (ip, error) =>
        {
            if (!string.IsNullOrEmpty(error))
            {
                onResult(false, error);
                return;
            }

            Debug.Log($"[RoomJoin] 조회된 IP: {ip}");
            RoomManager.singleton.networkAddress = ip;
            onResult(true, null); // 입장 가능
        }));
    }
    IEnumerator GetIpFromCode(string code, Action<string, string> callback)
    {
        UnityWebRequest request = UnityWebRequest.Get($"{nodeServerUrl}/lookup/{code}");
        yield return request.SendWebRequest();
        
        if (request.responseCode == 404)
        {
            callback(null, "코드가 유효하지 않습니다.");
            yield break;
        }
        else if (request.responseCode == 403) // 인원수 초과
        {          
            callback(null, "방 인원이 가득 찼습니다.");
            yield break;
        }
        else if (request.responseCode == 200)
        {
            string ip = request.downloadHandler.text;
            callback(ip, null);
        }
        else  if (request.result != UnityWebRequest.Result.Success)
        {
            callback(null, "서버와 연결할 수 없습니다.");
            yield break;
        }
        else
        {
            callback(null, "문제가 발생했습니다. 나중에 다시 시도해주세요.");
            yield break;
        }
    }
}
