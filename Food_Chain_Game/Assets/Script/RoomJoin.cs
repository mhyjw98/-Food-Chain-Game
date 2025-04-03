using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class RoomJoin : MonoBehaviour
{
    public string nodeServerUrl = "http://119.70.42.239:3000";

    public void JoinRoom(string code)
    {
        StartCoroutine(GetIpFromCode(code, (ip) => {
            Debug.Log($"조회된 IP: {ip}");
            RoomManager.singleton.networkAddress = ip;
            RoomManager.singleton.StartClient();
        }));
    }

    IEnumerator GetIpFromCode(string code, Action<string> onSuccess)
    {
        UnityWebRequest req = UnityWebRequest.Get($"{nodeServerUrl}/lookup/{code}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string ip = req.downloadHandler.text;
            onSuccess?.Invoke(ip);
        }
        else
        {
            Debug.LogError($"코드 조회 실패: {req.error}");
        }
    }
}
