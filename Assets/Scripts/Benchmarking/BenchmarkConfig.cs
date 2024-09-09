using System.IO;
using Core.Json;

public class BenchmarkConfig
{
    public string BenchmarkVersion { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public float BenchmarkDuration { get; set; }
    public string SimulationConfigPath { get; set; }

    public static BenchmarkConfig FromFile(string configPath)
    {
        string json = File.ReadAllText(configPath);

        if (!DefaultJsonSerializer.Default.ReadJsonProperty(json, nameof(BenchmarkVersion), out string _))
            throw new System.ArgumentException("The provided file is not recognized as a benchmark config");

        var config = (BenchmarkConfig)DefaultJsonSerializer.Default.FromJson(json, typeof(BenchmarkConfig), true);
        if (config.SimulationConfigPath is null) {
            string parentDir = Path.GetDirectoryName(configPath);
            string configsDir = Path.Combine(parentDir, "Configs");
            if (Path.GetFileName(configPath).EndsWith("-benchmark.json"))
            {
                string configFilename = Path.GetFileName(configPath);
                string filename = configFilename.Substring(0, configFilename.LastIndexOf('-'));
                config.SimulationConfigPath = Path.Combine(configsDir, filename + ".json");
            }
        }

        return config;
    }
}