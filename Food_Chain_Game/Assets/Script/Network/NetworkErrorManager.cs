using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NetworkErrorReason
{
    None,
    HostDisconnected,
    ConnectionLost,
    Unknown
}
public class NetworkErrorManager : MonoBehaviour
{
    public static NetworkErrorManager Instance { get; private set; }

    public NetworkErrorReason CurrentReason { get; private set; } = NetworkErrorReason.None;
    public string CurrentMessage { get; private set; } = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetError(NetworkErrorReason reason, string message)
    {
        CurrentReason = reason;
        CurrentMessage = message;
    }

    public void Clear()
    {
        CurrentReason = NetworkErrorReason.None;
        CurrentMessage = null;
    }

    public bool HasError => CurrentReason != NetworkErrorReason.None && !string.IsNullOrEmpty(CurrentMessage);
}
