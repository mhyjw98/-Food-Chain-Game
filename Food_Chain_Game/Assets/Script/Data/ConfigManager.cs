using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public static class ConfigManager
{
    private static string ConfigFile = "config.json";
    private static string ConfigPath => Path.Combine(Application.streamingAssetsPath, ConfigFile);
    public static NetworkConfig Config { get; private set; }

    static ConfigManager()
    {
        Load();
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
}

public struct NetworkConfig
{
    [JsonProperty("IP")]
    public string IP { get; set; }

}
