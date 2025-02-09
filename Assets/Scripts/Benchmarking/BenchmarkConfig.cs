using System.IO;
using Core.Json;

public class BenchmarkConfig
{
    public string BenchmarkVersion { get; set; } = "1.1.0";
    public string Name { get; set; }
    public string Description { get; set; }
    public float? BenchmarkDuration { get; set; } = null;
    public float? WarmupTime { get; set; } = null;
    public float? CooldownTime { get; set; } = null;
    public string SimulationConfigPath { get; set; }

    [NoJsonSerialization] public string SimulationConfigJson { get; set; }
    [NoJsonSerialization] public string BaseFilename { get; set; }

    public static BenchmarkConfig FromFile(string configPath)
    {
        string json = File.ReadAllText(configPath);

        if (!DefaultJsonSerializer.Default.ReadJsonProperty(json, nameof(BenchmarkVersion), out string _))
            throw new System.ArgumentException("The provided file is not recognized as a benchmark config");

        var config = (BenchmarkConfig)DefaultJsonSerializer.Default.FromJson(json, typeof(BenchmarkConfig), true);
        if (Core.Algorithm.ParseVersion(config.BenchmarkVersion) is null)
            throw new System.ArgumentException("Invalid benchmark version");

        string parentDir = Path.GetDirectoryName(configPath);

        if (config.SimulationConfigPath is null) {
            string configsDir = Path.Combine(parentDir, "Configs");
            if (Path.GetFileName(configPath).EndsWith("-benchmark.json"))
            {
                string configFilename = Path.GetFileName(configPath);
                config.BaseFilename = configFilename.Substring(0, configFilename.LastIndexOf('-'));
                config.SimulationConfigPath = Path.Combine(configsDir, config.BaseFilename + ".json");
            }
        } else if (!Path.IsPathRooted(config.SimulationConfigPath))
        {
            config.SimulationConfigPath = Path.Combine(parentDir, config.SimulationConfigPath);
        }

        if (!File.Exists(config.SimulationConfigPath)) throw new FileNotFoundException("Could not find corresponding simulation config");
        config.SimulationConfigJson = JsonSerializerUtility.Compress(File.ReadAllText(config.SimulationConfigPath));

        return config;
    }

    public BenchmarkConfig Copy() { return MemberwiseClone() as BenchmarkConfig; }
}