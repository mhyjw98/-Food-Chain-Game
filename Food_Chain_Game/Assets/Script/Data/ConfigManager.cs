using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public static class ConfigManager
{
    private static string ConfigFolder = "Resource";
    private static string ConfigFile = "config.json";
    private static string ConfigPath = ConfigFolder + "/" + ConfigFile;
    public static NetworkConfig Config { get; set; }

    static ConfigManager()
    {
        if (!Directory.Exists(ConfigFolder))
            Directory.CreateDirectory(ConfigFolder);

        if (!File.Exists(ConfigPath))
        {
            Config = new NetworkConfig();
            var json = JsonConvert.SerializeObject(Config, Formatting.Indented);
            File.WriteAllText(ConfigPath, json);
        }
        else
        {
            var json = File.ReadAllText(ConfigPath);
            Config = JsonConvert.DeserializeObject<NetworkConfig>(json);
        }
    }
}

public struct NetworkConfig
{
    [JsonProperty("IP")]
    public string IP { get; set; }

}
