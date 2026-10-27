using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public static class ConfigManager
{
    // config.json에 포트가 없거나 잘못됐을 때 쓰는 값
    public const int DefaultServerPort = 3000;
    public const int DefaultGamePort = 7777;

    private static string ConfigFile = "config.json";
    private static string ConfigPath => Path.Combine(Application.streamingAssetsPath, ConfigFile);
    public static NetworkConfig Config { get; private set; }

    static ConfigManager()
    {
        Load();
    }

    // 서버 주소(IP)가 들어 있는지. 요청을 보낼 때마다 이 값을 본다
    public static bool IsValid => !string.IsNullOrWhiteSpace(Config.IP);

    // 방 만들기·입장처럼 사용자가 직접 시작하는 요청 전에 부른다.
    // 설정이 비어 있었으면 파일을 다시 읽어서, 게임을 켠 채로 config.json을 고친 경우에도 다시 켜지 않고 쓸 수 있게 한다
    public static bool EnsureValid()
    {
        if (!IsValid) Load();
        return IsValid;
    }

    private static void Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                Debug.LogError($"[ConfigManager] 설정 파일이 없습니다: {ConfigPath}\n" +
                    "Assets/StreamingAssets/config.json을 만들고 서버 IP를 설정해주세요. " +
                    "(이 파일은 Assets 밖의 Resource/config.json과 달리 빌드에 자동으로 포함되고, " +
                    "ParrelSync 클론에도 Assets 심볼릭 링크를 통해 그대로 공유됩니다.)");
                Config = new NetworkConfig();
                return;
            }

            var json = File.ReadAllText(ConfigPath);
            Config = JsonConvert.DeserializeObject<NetworkConfig>(json);

            if (string.IsNullOrWhiteSpace(Config.IP))
            {
                Debug.LogError($"[ConfigManager] {ConfigPath}에 IP 값이 비어있습니다. " +
                    "방 생성/조회 등 서버 통신 기능이 동작하지 않습니다.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ConfigManager] 설정 파일을 읽는 중 오류가 발생했습니다: {ex.Message}");
            Config = new NetworkConfig();
        }
    }

    // Node 서버(방 코드 등록·조회) 포트
    public static int ServerPort => IsValidPort(Config.ServerPort) ? Config.ServerPort : DefaultServerPort;

    // 게임(Mirror) 포트. 방장이 이 포트로 방을 열고, 입장자는 Node 서버가 알려 준 방장의 포트로 접속한다
    public static int GamePort => IsValidPort(Config.GamePort) ? Config.GamePort : DefaultGamePort;

    public static string NodeServerUrl => "http://" + Config.IP + ":" + ServerPort;

    // 방장이 방을 열 포트. ParrelSync 클론은 config.json을 원본과 같이 쓰므로,
    // 같은 기기에서 원본과 클론이 각각 방을 열어도 겹치지 않게 클론 번호만큼 뒤 번호를 쓴다(원본 = GamePort, clone_0 = +1 ...)
    public static int HostGamePort
    {
        get
        {
            int port = GamePort + EditorCloneOffset();
            return IsValidPort(port) ? port : GamePort;
        }
    }

    public static bool IsValidPort(int port) => port > 0 && port <= 65535;

    // Mirror 트랜스포트의 포트를 바꾼다. 포트를 쓰지 않는 트랜스포트면 false
    public static bool TrySetTransportPort(int port)
    {
        if (!IsValidPort(port)) return false;

        var transport = Mirror.NetworkManager.singleton != null ? Mirror.NetworkManager.singleton.transport : Mirror.Transport.active;
        if (transport is Mirror.PortTransport portTransport)
        {
            portTransport.Port = (ushort)port;
            return true;
        }

        Debug.LogWarning($"[ConfigManager] 현재 트랜스포트({(transport != null ? transport.GetType().Name : "없음")})는 포트를 바꿀 수 없습니다.");
        return false;
    }

    private static int EditorCloneOffset()
    {
#if UNITY_EDITOR
        // ParrelSync 클론 프로젝트 폴더 이름: "<원본>_clone_<번호>"
        string projectFolder = Path.GetFileName(Path.GetDirectoryName(Application.dataPath));
        const string marker = "_clone_";
        int index = projectFolder != null ? projectFolder.LastIndexOf(marker, StringComparison.Ordinal) : -1;
        if (index >= 0 && int.TryParse(projectFolder.Substring(index + marker.Length), out int cloneNumber) && cloneNumber >= 0)
            return cloneNumber + 1;
#endif
        return 0;
    }
}

public struct NetworkConfig
{
    [JsonProperty("IP")]
    public string IP { get; set; }

    // 없으면 0 → ConfigManager가 기본값(3000 / 7777)을 쓴다
    [JsonProperty("ServerPort")]
    public int ServerPort { get; set; }

    [JsonProperty("GamePort")]
    public int GamePort { get; set; }
}
