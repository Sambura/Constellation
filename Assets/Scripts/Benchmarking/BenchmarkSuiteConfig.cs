using System.IO;
using System.Collections.Generic;
using Core.Json;
using UnityEngine;

public class BenchmarkSuiteConfig
{
    public string BenchmarkSuiteVersion { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public FullScreenMode FullscreenMode { get; set; } = FullScreenMode.ExclusiveFullScreen;
    public int FpsCap { get; set; } = 0;
    public List<BenchmarkConfig> Configs { get; set; } // TODO - make so that this is not serialized
    public int ConfigsFailedToLoad { get; protected set; }

    public static BenchmarkSuiteConfig FromFile(string configPath)
    {
        string json = File.ReadAllText(configPath);

        if (!DefaultJsonSerializer.Default.ReadJsonProperty(json, nameof(BenchmarkSuiteVersion), out string _))
            throw new System.ArgumentException("The provided file is not recognized as a benchmark suite config");

        var config = (BenchmarkSuiteConfig)DefaultJsonSerializer.Default.FromJson(json, typeof(BenchmarkSuiteConfig), true);
        
        DirectoryInfo confgisDir = Directory.CreateDirectory(Path.GetDirectoryName(configPath));
        config.Configs = new List<BenchmarkConfig>();
        config.ConfigsFailedToLoad = 0;
        foreach (FileInfo file in confgisDir.EnumerateFiles("*-benchmark.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                config.Configs.Add(BenchmarkConfig.FromFile(file.FullName));
            }
            catch { config.ConfigsFailedToLoad++; }
        }

        return config;
    }
}
