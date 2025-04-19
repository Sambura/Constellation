using System.IO;
using System.Collections.Generic;
using Core.Json;
using UnityEngine;

public class BenchmarkSuiteConfig
{
    // Let's keep this version equal to the first version of Constellation that supported it
    public string BenchmarkSuiteVersion { get; set; } = "1.1.16";
    public string Name { get; set; }
    public string Description { get; set; }
    public FullScreenMode FullscreenMode { get; set; } = FullScreenMode.ExclusiveFullScreen;
    public int FpsCap { get; set; } = 0;
    public float? BenchmarkDurationOverride { get; set; } = null;
    public float? CooldownDurationOverride { get; set; } = null;
    public float? WarmupDurationOverride { get; set; } = null;
    public float? AutoBufferMargin { get;set; } = null;
    public int? BufferSize { get; set; } = null;
    public int RepeatCount { get; set; } = 1;
    public bool ShuffleBenchmarks { get; set; } = false;
    public int? OverrideRngSeed { get; set; } = null;

    [NoJsonSerialization] public List<BenchmarkConfig> Configs { get; set; }
    [NoJsonSerialization] public int ConfigsFailedToLoad { get; protected set; }

    public static BenchmarkSuiteConfig FromFile(string configPath)
    {
        string json = File.ReadAllText(configPath);
        if (!DefaultJsonSerializer.Default.ReadJsonProperty(json, nameof(BenchmarkSuiteVersion), out string _))
            throw new System.ArgumentException("The provided file is not recognized as a benchmark suite config");

        var config = (BenchmarkSuiteConfig)DefaultJsonSerializer.Default.FromJson(json, typeof(BenchmarkSuiteConfig), true);
        if (Core.Algorithm.ParseVersion(config.BenchmarkSuiteVersion) is null)
            throw new System.ArgumentException("Invalid benchmark suite version");

        DirectoryInfo configsDir = new DirectoryInfo(Path.GetDirectoryName(configPath));
        config.Configs = new List<BenchmarkConfig>();
        config.ConfigsFailedToLoad = 0;
        foreach (FileInfo file in configsDir.EnumerateFiles("*-benchmark.json", SearchOption.TopDirectoryOnly))
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
