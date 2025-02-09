using System.Collections.Generic;
using System.IO;
using Core.Json;

/// <summary>
/// Configuration class for benchmarks that involve several different versions of Constellation
/// </summary>
public class CrossBenchmarkConfig
{
    public string CrossBenchmarkVersion { get; set; } = "1.0.1";
    public List<CrossBenchmark> BenchmarkSequence { get; set; }
    public bool TemporaryConfig { get; set; } = false;
    public SystemAction PostBenchmarkAction { get; set; } = SystemAction.Nothing;
    public bool QuitAndLockOnFocusLoss { get; set; } = true;
    public bool ExecuteActionOnCancel { get; set; } = true;

    public static CrossBenchmarkConfig FromFile(string path)
    {
        string json = File.ReadAllText(path);

        if (!DefaultJsonSerializer.Default.ReadJsonProperty(json, nameof(CrossBenchmarkVersion), out string _))
            throw new System.ArgumentException("The provided file is not recognized as a cross benchmark config");

        var config = (CrossBenchmarkConfig)DefaultJsonSerializer.Default.FromJson(json, typeof(CrossBenchmarkConfig), true);
        if (Core.Algorithm.ParseVersion(config.CrossBenchmarkVersion) is null)
            throw new System.ArgumentException("Invalid cross benchmark version");

        for (int i = 0; i < config.BenchmarkSequence.Count; i++)
        {
            CrossBenchmark benchmark = config.BenchmarkSequence[i];
            // first element is allowed to not have executable path, since it's supposed to be the path of the currently
            // loaded executable
            if (benchmark.ExecutablePath is { } || i > 0)
                if (!File.Exists(benchmark.ExecutablePath) ) throw new System.ArgumentException($"Could not find executable for benchmark #{i}");
            if (!File.Exists(benchmark.BenchmarkSuitePath)) throw new System.ArgumentException($"Could not find benchmark suite for benchmark #{i}");
        }

        return config;
    }
}

public struct CrossBenchmark
{
    public string ExecutablePath { get; set; }
    public string BenchmarkSuitePath { get; set; }
    public string ResultsOutputPath { get; set; }
}
