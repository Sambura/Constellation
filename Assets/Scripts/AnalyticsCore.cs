﻿using ConfigSerialization.Structuring;
using ConfigSerialization;
using UnityEngine;
using UnityCore;
using System;
using System.Collections;
using System.Collections.Generic;
using ConstellationUI;
using Core;
using Core.Json;
using System.IO;
using System.Linq;

// Consider using Unity's FrameTimingManager
public class AnalyticsCore : MonoBehaviour
{
    [SerializeField] private FileDialog _fileDialog;
    [SerializeField] private GameObject _uiObject;
    [SerializeField] private ParticleController _particleController;
    [SerializeField] private MainVisualizer _mainVisualizer;
    [SerializeField] private StaticTimeFPSCounter _fpsCounter;
    [SerializeField] private Viewport _viewport;
    [SerializeField] private PerformanceReportDialog _perfDialog;
    [SerializeField] private ConfigSerializer _configSerializer;
    [SerializeField] private InteractionCore _interactionCore;
    [SerializeField] private ApplicationController _applicationController;

    private FrameTimingTracker _tracker;
    private StaticTimeFPSCounter _helperFpsCounter;

    #region Config properties

    private BenchmarkMode _benchmarkMode = BenchmarkMode.Custom;
    private float _benchmarkDuration = 10;
    private float _warmupDuration = 0.5f;
    private float _cooldownDuration = 0;
    private float? _benchmarkDurationOverride = null;
    private float? _warmupDurationOverride = null;
    private float? _cooldownDurationOverride = null;
    private bool _automaticBufferSize = true;
    private float _autoBufferSizeMargin = 2f;
    private int _frameTimingBufferSize = 500000;
    private string _benchmarkFilePath = null;
    private string _benchmarkSuiteFilePath = null;
    private int _benchmarkRepeatCount = 1;
    private int _suiteRepeatCount = 1;
    private bool _shuffleBenchmarks = false;

    private BenchmarkConfig _currentBenchmarkConfig;
    private BenchmarkSuiteConfig _currentBenchmarkSuiteConfig;

    [ConfigGroupMember("Performance analysis")]
    [ConfigGroupToggle(3, new object[] { 1, 3 }, 2, DoNotReorder = true)]
    [ConfigProperty]
    public BenchmarkMode BenchmarkMode
    {
        get => _benchmarkMode;
        set
        {
            if (_benchmarkMode == value) return;
            _benchmarkMode = value;
            BenchmarkModeChanged?.Invoke(value);
        }
    }

    [ConfigGroupMember(groupIndex: 1, parentIndex: 0)]
    [FilePathProperty("Select benchmark config file", true, new string[] { "Json files", "*.json", "All files", "*" }, typeof(BenchmarkNameGetter), name: "Benchmark:")]
    public string BenchmarkFilePath
    {
        get => _benchmarkFilePath;
        set { if (_benchmarkFilePath != value) SetBenchmarkFilePath(value); BenchmarkFilePathChanged?.Invoke(_benchmarkFilePath); }
    }

    [ConfigGroupMember(groupIndex: 2, parentIndex: 0)]
    [SetComponentProperty(typeof(TMPro.TextMeshProUGUI), "color", typeof(Color), new object[] { 0.972549f, 0.9294117647f, 0.56470588f, 0.5f }, childName: "Labels/FilenameLabel")]
    [FilePathProperty("Select benchmark suite config file", true, new string[] { "Json files", "*.json", "All files", "*" }, typeof(BenchmarkSuiteNameGetter), name: "Benchmark suite:")]
    public string BenchmarkSuiteFilePath
    {
        get => _benchmarkSuiteFilePath;
        set { if (_benchmarkSuiteFilePath != value) { SetBenchmarkSuiteFilePath(value); BenchmarkSuiteFilePathChanged?.Invoke(_benchmarkSuiteFilePath); } }
    }

    [ConfigGroupMember(groupIndex: 3, parentIndex: 0)]
    [SliderProperty(1, 120, 0, inputFormatting: "0.0 s", inputRegex: @"([-+]?[0-9]*\.?[0-9]+) *s?")]
    public float BenchmarkDuration
    {
        get => _benchmarkDuration;
        set
        {
            if (_benchmarkDuration == value) return;
            _benchmarkDuration = value;
            BenchmarkDurationChanged?.Invoke(value);
        }
    }

    [ConfigGroupMember(groupIndex: 3)]
    [SliderProperty(0, 30, 0, inputFormatting: "0.0 s", inputRegex: @"([-+]?[0-9]*\.?[0-9]+) *s?")]
    public float CooldownDuration
    {
        get => _cooldownDuration;
        set
        {
            if (_cooldownDuration == value) return;
            _cooldownDuration = value;
            CooldownDurationChanged?.Invoke(value);
        }
    }

    [ConfigGroupMember(groupIndex: 3)]
    [SliderProperty(0.1f, 30, 0, inputFormatting: "0.0 s", inputRegex: @"([-+]?[0-9]*\.?[0-9]+) *s?")]
    public float WarmupDuration
    {
        get => _warmupDuration;
        set
        {
            if (_warmupDuration == value) return;
            _warmupDuration = value;
            WarmupDurationChanged?.Invoke(value);
        }
    }

    [ConfigGroupMember(groupIndex: 3)]
    [SliderProperty(1, 50, 1, name: "Repeat count")]
    public int BenchmarkRepeatCount
    {
        get => _benchmarkRepeatCount;
        set
        {
            if (_benchmarkRepeatCount == value) return;
            _benchmarkRepeatCount = value;
            BenchmarkRepeatCountChanged?.Invoke(value);
        }
    }

    [ConfigGroupMember(groupIndex: 2)]
    [SetComponentProperty(typeof(UIArranger), nameof(UIArranger.SelectedConfigurationName), "Compact")]
    [SliderProperty(1, 300, 0, inputFormatting: "0.0 s", inputRegex: @"([-+]?[0-9]*\.?[0-9]+) *s?", name: "Override benchmark duration")]
    public float? BenchmarkDurationOverride
    {
        get => _benchmarkDurationOverride;
        set
        {
            if (_benchmarkDurationOverride == value) return;
            _benchmarkDurationOverride = value;
            BenchmarkDurationOverrideChanged?.Invoke(value);
        }
    }

    [ConfigGroupMember(groupIndex: 2)]
    [SetComponentProperty(typeof(UIArranger), nameof(UIArranger.SelectedConfigurationName), "Compact")]
    [SliderProperty(0, 30, 0, inputFormatting: "0.0 s", inputRegex: @"([-+]?[0-9]*\.?[0-9]+) *s?", name: "Override cooldown duration")]
    public float? CooldownDurationOverride
    {
        get => _cooldownDurationOverride;
        set
        {
            if (_cooldownDurationOverride == value) return;
            _cooldownDurationOverride = value;
            CooldownDurationOverrideChanged?.Invoke(value);
        }
    }

    [ConfigGroupMember(groupIndex: 2)]
    [SetComponentProperty(typeof(UIArranger), nameof(UIArranger.SelectedConfigurationName), "Compact")]
    [SliderProperty(0.1f, 30, 0, inputFormatting: "0.0 s", inputRegex: @"([-+]?[0-9]*\.?[0-9]+) *s?", name: "Override warmup duration")]
    public float? WarmupDurationOverride
    {
        get => _warmupDurationOverride;
        set
        {
            if (_warmupDurationOverride == value) return;
            _warmupDurationOverride = value;
            WarmupDurationOverrideChanged?.Invoke(value);
        }
    }

    [ConfigGroupMember(groupIndex: 2)]
    [SliderProperty(1, 50, 1, name: "Suite repeat count")]
    public int BenchmarkSuiteRepeatCount
    {
        get => _suiteRepeatCount;
        set
        {
            if (_suiteRepeatCount == value) return;
            _suiteRepeatCount = value;
            BenchmarkSuiteRepeatCountChanged?.Invoke(value);
        }
    }

    [ConfigGroupMember(groupIndex: 2)]
    [ConfigProperty]
    public bool ShuffleBenchmarks
    {
        get => _shuffleBenchmarks;
        set
        {
            if (_shuffleBenchmarks == value) return;
            _shuffleBenchmarks = value;
            ShuffleBenchmarksChanged?.Invoke(value);
        }
    }

    [ConfigGroupMember]
    [ConfigGroupToggle(20, 21)]
    [ConfigMemberOrder(4)]
    [ConfigProperty]
    public bool AutomaticBufferSize
    {
        get => _automaticBufferSize;
        set
        {
            if (_automaticBufferSize == value) return;
            _automaticBufferSize = value;
            AutomaticBufferSizeChanged?.Invoke(value);
        }
    }

    [ConfigGroupMember(groupIndex: 20, parentIndex: 0)]
    [SliderProperty(0, 2, 0, inputFormatting: "0.0", name: "Buffer size margin")]
    public float AutomaticBufferSizeMargin
    {
        get => _autoBufferSizeMargin;
        set
        {
            if (_autoBufferSizeMargin == value) return;
            _autoBufferSizeMargin = value;
            AutomaticBufferSizeMarginChanged?.Invoke(value);
        }
    }

    [ConfigGroupMember(groupIndex: 21, parentIndex: 0)]
    [InputFieldProperty(0, 100000000, name: "Manual buffer size")]
    public int FrameTimingBufferSize
    {
        get => _frameTimingBufferSize;
        set
        {
            if (FrameTimingBufferSize == value) return;
            _frameTimingBufferSize = value;
            FrameTimingBufferSizeChanged?.Invoke(value);
        }
    }

    public event Action<float> BenchmarkDurationChanged;
    public event Action<float> WarmupDurationChanged;
    public event Action<float> CooldownDurationChanged;
    public event Action<float?> BenchmarkDurationOverrideChanged;
    public event Action<float?> WarmupDurationOverrideChanged;
    public event Action<float?> CooldownDurationOverrideChanged;
    public event Action<bool> AutomaticBufferSizeChanged;
    public event Action<float> AutomaticBufferSizeMarginChanged;
    public event Action<int> FrameTimingBufferSizeChanged;
    public event Action<BenchmarkMode> BenchmarkModeChanged;
    public event Action<string> BenchmarkFilePathChanged;
    public event Action<string> BenchmarkSuiteFilePathChanged;
    public event Action<int> BenchmarkRepeatCountChanged;
    public event Action<int> BenchmarkSuiteRepeatCountChanged;
    public event Action<bool> ShuffleBenchmarksChanged;

    private void SetBenchmarkFilePath(string value)
    {
        if (value is null)
        {
            _currentBenchmarkConfig = null;
        }
        else
        {
            try
            {
                _currentBenchmarkConfig = BenchmarkConfig.FromFile(value);
                _benchmarkFilePath = value;
                if (_currentBenchmarkConfig.BenchmarkDuration.HasValue)
                    BenchmarkDuration = _currentBenchmarkConfig.BenchmarkDuration.Value;
            }
            catch (JsonSerializerException e)
            {
                _fileDialog.Manager.ShowMessageBox("Error", $"Couldn't load benchmark config: {e.Message}", StandardMessageBoxIcons.Error);
            }
            catch (FileNotFoundException)
            {
                _fileDialog.Manager.ShowMessageBox("Error", $"Couldn't find benchmark config file at: {value}", StandardMessageBoxIcons.Error);
            } // in case of exception an event will still be generated to notify that the setter has declined the operation
            catch (Exception e)
            {
                _fileDialog.Manager.ShowMessageBox("Error", $"Encountered error while reading benchmark config: {e.Message}", StandardMessageBoxIcons.Error);
            }
        }
    }

    private void SetBenchmarkSuiteFilePath(string value)
    {
        if (value is null)
        {
            _currentBenchmarkSuiteConfig = null;
        }
        else
        {
            try
            {
                var newSuite = BenchmarkSuiteConfig.FromFile(value);
                if (newSuite.Configs.Count == 0 && newSuite.ConfigsFailedToLoad == 0) throw new Exception("Could not find any valid benchmark configs for this suite");
                if (newSuite.Configs.Count == 0) throw new Exception($"Failed to load any of {newSuite.ConfigsFailedToLoad} benchmark configs");
                _currentBenchmarkSuiteConfig = newSuite;
                _benchmarkSuiteFilePath = value;
                BenchmarkDurationOverride = newSuite.BenchmarkDurationOverride;
                WarmupDurationOverride = newSuite.WarmupDurationOverride;
                CooldownDurationOverride = newSuite.CooldownDurationOverride;
            }
            catch (JsonSerializerException e)
            {
                _fileDialog.Manager.ShowMessageBox("Error", $"Couldn't load benchmark suite config: {e.Message}", StandardMessageBoxIcons.Error);
            }
            catch (FileNotFoundException)
            {
                _fileDialog.Manager.ShowMessageBox("Error", $"Couldn't find benchmark suite config file at: {value}", StandardMessageBoxIcons.Error);
            } // in case of exception an event will still be generated to notify that the setter has declined the operation
            catch (Exception e)
            {
                _fileDialog.Manager.ShowMessageBox("Error", $"Encountered error while reading benchmark suite config: {e.Message}", StandardMessageBoxIcons.Error);
            }
        }
    }

    #endregion

    private List<BenchmarkConfig> _benchmarksQueue = new List<BenchmarkConfig>();
    private List<BenchmarkResult> _benchmarkResults = new List<BenchmarkResult>();

    public float FPSMeasuringDuration { get; set; } = 1f;

    /// <summary>
    /// Loads SimulationConfigJson specified in benchmark config.
    /// If any error is encountered returns false, and shows an error message box (if verbose is true)
    /// </summary>
    private bool LoadBenchmarkConfig(BenchmarkConfig config, bool verbose = true)
    {
        try
        {
            if (_configSerializer.DeserializeJsonConfig(config.SimulationConfigJson) <= 0)
            {
                if (verbose)
                    _fileDialog.Manager.ShowMessageBox("Error", "No data deserialized from the simulation config", StandardMessageBoxIcons.Error);
                return false;
            }
        }
        catch (JsonSerializerException e)
        {
            if (verbose)
                _fileDialog.Manager.ShowMessageBox("Error", $"Couldn't load simulation config: {e.Message}", StandardMessageBoxIcons.Error);
            return false;
        }
        catch (Exception e)
        {
            if (verbose)
                _fileDialog.Manager.ShowMessageBox("Error", $"Unknown error while loading benchmark config: {e.Message}", StandardMessageBoxIcons.Error);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Unity Coroutine <br/>
    /// Executes all benchmarks currently added to <see cref="_benchmarksQueue"/>. Executing benchmarks includes loading simulation
    /// config, disabling UI, estimating buffer size, etc. The only requirement to run this is to fill queue with valid entries. Each
    /// BenchmarkConfig in queue should have non-null duration, cooldown, warmup, SimulationConfigJson, and BaseFilename. The results
    /// of benchmarks are stored in <see cref="_benchmarkResults"/> (the list is cleared automatically before starting the benchmarks)
    /// </summary>
    /// <param name="onQueueFinish">Delegate to run when benchmarks are finished. Accepts list of failed benchmarks. The benchmarks may fail
    /// mainly because of invalid simulation config json. UI is enabled automatically on coroutine end, no need to do that in the delegate. </param>
    /// <param name="exportPath">Path where the benchmark reports should be saved to, or null if there is no need to export reports</param>
    private IEnumerator RunBenchmarkQueue(Action<List<BenchmarkConfig>> onQueueFinish, string exportPath = null)
    {
        string previousConfig = _configSerializer.GetCurrentConfigJson(false);
        bool wasFpsCounterEnabled = DisableUI();
        List<BenchmarkConfig> failedBenchmarks = new List<BenchmarkConfig>();
        _benchmarkResults.Clear();

        try
        {
            foreach (var benchmark in _benchmarksQueue)
            {
                if (!LoadBenchmarkConfig(benchmark, false))
                {
                    failedBenchmarks.Add(benchmark);
                    continue;
                }
                _particleController.RestartSimulation();

                if (_tracker is { }) { Destroy(_tracker); _tracker = null; }
                _tracker = gameObject.AddComponent<FrameTimingTracker>();
                if (AutomaticBufferSize)
                {
                    _helperFpsCounter = gameObject.AddComponent<StaticTimeFPSCounter>();
                    _helperFpsCounter.TimeWindow = FPSMeasuringDuration;
                }

                yield return new WaitForSeconds(AutomaticBufferSize ? FPSMeasuringDuration : 0);
                if (AutomaticBufferSize)
                {
                    FrameTimingBufferSize = Mathf.RoundToInt(_helperFpsCounter.CurrentFps * BenchmarkDuration * (1 + AutomaticBufferSizeMargin));
                    DestroyImmediate(_helperFpsCounter);
                }
                _tracker.BufferSize = FrameTimingBufferSize;
                _tracker.PrepareTracking();

                if (benchmark.CooldownTime.Value > 0)
                {
                    // look into disabling the renderer as well (I would like to make sure that exposing `enabled` field for write will not hinder its performance!)
                    int targetFps = _applicationController.TargetFrameRate;
                    _applicationController.TargetFrameRate = 7; // frame rate too low == coroutines lag as well. 7 is arbitrary
                    _particleController.enabled = false;
                    _mainVisualizer.enabled = false;
                    yield return new WaitForSeconds(benchmark.CooldownTime.Value);
                    _particleController.enabled = true;
                    _mainVisualizer.enabled = true;
                    _applicationController.TargetFrameRate = targetFps;
                }

                yield return new WaitForSeconds(benchmark.WarmupTime.Value);

                _tracker.StartTracking();

                yield return new WaitForSeconds(benchmark.BenchmarkDuration.Value);

                _tracker.StopTracking();
                BenchmarkResult currentResult = new BenchmarkResult(benchmark, _tracker.FrameTimings);
                _benchmarkResults.Add(currentResult);

                if (exportPath is { })
                    DoSaveReport(Path.Combine(exportPath, benchmark.BaseFilename + "-report.json"), currentResult);
            }
        }
        finally { RestoreUI(wasFpsCounterEnabled); }

        onQueueFinish?.Invoke(failedBenchmarks);
        _configSerializer.DeserializeJsonConfig(previousConfig);
    }

    /// <summary>
    /// "Start benchmark" button in UI menu
    /// </summary>
    [SetComponentProperty(typeof(UnityEngine.UI.Image), "color", typeof(Color), new object[] { 1f, 0.7733893f, 0.4198113f }, "Border")]
    [SetComponentProperty(typeof(TMPro.TextMeshProUGUI), "color", typeof(Color), new object[] { 0.8862745f, 0.6864019f, 0f })]
    [SetComponentProperty(typeof(UnityEngine.UI.Button), "colors.normalColor", typeof(Color), new object[] { 1f, 0.784871f, 0.2877358f, 0.1843137f })]
    [ConfigGroupMember]
    [InvokableMethod]
    [ConfigMemberOrder(-3)]
    public void StartBenchmark()
    {
        if (BenchmarkMode == BenchmarkMode.BenchmarkSuite)
        {
            if (_currentBenchmarkSuiteConfig is null)
            {
                _fileDialog.Manager.ShowMessageBox("Error", $"No benchmark suite selected", StandardMessageBoxIcons.Error);
                return;
            }

            _fileDialog.SyncCurrentDirectory(this);
            _fileDialog.ShowDialog("Select directory to export timings", (x, y) =>
            {
                if (!y) return true;
                _fileDialog.Manager.ShowMessageBox("Alert", "You are about to start benchmark suite! Please <color=yellow>close all other applications</color>" +
                    " and make sure that the <color=orange>power mode of your PC is configured</color> correctly. <color=#b0b0b0>Refrain from using your PC" +
                    " until the benchmark is complete to ensure the best measurement results</color>", StandardMessageBoxIcons.Info, null, (x, y) =>
                    { StartCoroutine(DoStartBenchmarkSuite(_fileDialog.CurrentDirectory.FullName)); return true; });
                return true;
            });
            return;
        }

        BenchmarkConfig config = BenchmarkMode == BenchmarkMode.Custom ? null : _currentBenchmarkConfig;
        config ??= new BenchmarkConfig()
        {
            Name = "Custom benchmark",
            BenchmarkVersion = "internal",
            SimulationConfigJson = _configSerializer.GetCurrentConfigJson(false)
        };

        _benchmarksQueue.Clear();
        config.BenchmarkDuration = BenchmarkDuration;
        config.CooldownTime = CooldownDuration;
        config.WarmupTime = WarmupDuration;
        for (int i = 0; i < BenchmarkRepeatCount; i++) _benchmarksQueue.Add(config);

        StartCoroutine(RunBenchmarkQueue(x => ShowReport()));
    }

    /// <summary>
    /// Unity coroutine <br/>
    /// Generates a queue of benchmarks, for the current benchmark suite, sets up and starts benchmark execution
    /// </summary>
    /// <param name="exportPath">Path where benchmark results should be exported to</param>
    private IEnumerator DoStartBenchmarkSuite(string exportPath)
    {
        _benchmarksQueue.Clear();

        foreach (BenchmarkConfig config in _currentBenchmarkSuiteConfig.Configs)
        {
            BenchmarkConfig copy = config.Copy();
            copy.BenchmarkDuration = BenchmarkDurationOverride.GetValueOrDefault(copy.BenchmarkDuration.GetValueOrDefault(1));
            copy.CooldownTime = CooldownDurationOverride.GetValueOrDefault(copy.CooldownTime.GetValueOrDefault(0));
            copy.WarmupTime = WarmupDurationOverride.GetValueOrDefault(copy.WarmupTime.GetValueOrDefault(0));

            for (int i = 0; i < BenchmarkSuiteRepeatCount; i++)
            {
                BenchmarkConfig instance = copy.Copy();
                if (BenchmarkSuiteRepeatCount > 1) instance.BaseFilename += $"-{i + 1}";
                _benchmarksQueue.Add(instance);
            }
        }

        if (ShuffleBenchmarks)
            _benchmarksQueue = new List<BenchmarkConfig>(_benchmarksQueue.OrderBy(x => UnityEngine.Random.value));

        _applicationController.FullScreenMode = _currentBenchmarkSuiteConfig.FullscreenMode;
        _applicationController.TargetFrameRate = _currentBenchmarkSuiteConfig.FpsCap;

        yield return null; // wait one frame for viewport dimensions to update (due to changing fullscreen mode)

        StartCoroutine(RunBenchmarkQueue(OnSuiteFinish, exportPath));

        void OnSuiteFinish(List<BenchmarkConfig> failed)
        {
            int successful = _benchmarksQueue.Count - failed.Count;
            string failedMessage = "";
            if (failed.Count > 0)
            {
                failedMessage = " Failed benchmarks: ";
                foreach (BenchmarkConfig config in failed) failedMessage += $"`{config.Name}`, ";
                failedMessage = failedMessage.Substring(0, failedMessage.Length - 2);
            }

            _fileDialog.Manager.ShowMessageBox("Benchmark suite finished",
                $"{successful} / {_benchmarksQueue.Count} benchmark runs executed successfully.{failedMessage}",
                StandardMessageBoxIcons.Success);
        }
    }

    private bool DisableUI()
    {
        bool wasFpsEnabled = _fpsCounter.enabled;

        _uiObject.SetActive(false);
        _fpsCounter.enabled = false;
        _viewport.enabled = false;
        _interactionCore.enabled = false;

        return wasFpsEnabled;
    }

    private void RestoreUI(bool enableFpsCounter)
    {
        _uiObject.SetActive(true);
        _fpsCounter.enabled = enableFpsCounter;
        _viewport.enabled = true;
        _interactionCore.enabled = true;
    }

    [ConfigGroupMember]
    [InvokableMethod]
    [ConfigMemberOrder(-2)]
    public void ShowReport()
    {
        if (_benchmarkResults.Count == 0)
        {
            _fileDialog.Manager.ShowMessageBox("Warning", "No frame timing data was captured yet", StandardMessageBoxIcons.Warning);
            return;
        }

        _perfDialog.OnExportReportClick = (x, y) => ExportReport(y);
        _perfDialog.OnExportSummaryClick = (x, y) => ExportSummary(y);
        _perfDialog.OnExportTimingsClick = (x, y) => ExportFrameTimings(y);
        _perfDialog.ShowDialog();
        _perfDialog.UpdateReport(_benchmarkResults);
    }

    public void ExportFrameTimings(BenchmarkResult data)
    {
        ExportTrackedData(
            fileFilters: new List<FileDialog.FileFilter>() { new FileDialog.FileFilter() { Description = "CSV files", Pattern = "*.csv" } },
            defaultFilename: "timings.csv",
            fileSaveDelegate: DoSaveTimings,
            data
        );
    }

    public void ExportSummary(BenchmarkResult data)
    {
        ExportTrackedData(
            fileFilters: new List<FileDialog.FileFilter>() { new FileDialog.FileFilter() { Description = "JSON files", Pattern = "*.json" } },
            defaultFilename: "summary.json",
            fileSaveDelegate: DoSaveSummary,
            data
        );
    }

    public void ExportReport(BenchmarkResult data)
    {
        ExportTrackedData(
            fileFilters: new List<FileDialog.FileFilter>() { new FileDialog.FileFilter() { Description = "JSON files", Pattern = "*.json" } },
            defaultFilename: "report.json",
            fileSaveDelegate: DoSaveReport,
            data
        );
    }

    private void ExportTrackedData(List<FileDialog.FileFilter> fileFilters, string defaultFilename, DataExporter fileSaveDelegate, BenchmarkResult data)
    {
        _fileDialog.FileFilters = fileFilters;
        _fileDialog.FileName = defaultFilename;
        _fileDialog.ShowDialog("Select save location", (x, y) => SaveFile(x, y, fileSaveDelegate, data));
        _fileDialog.SyncCurrentDirectory(this);
    }

    private static string GenerateTimingsCsv(List<float> timings)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int i = 0; i < timings.Count; i++)
        {
            builder.Append(timings[i].ToString("G9"));
            if (i != timings.Count - 1) builder.Append(", ");
        }
        return builder.ToString();
    }

    private static string GenerateSummaryJson(List<float> timings)
    {
        FrameStatistics stats = new FrameStatistics();
        stats.SetFrameTimings(timings);
        return DefaultJsonSerializer.Default.ToJson(stats);
    }

    public static void DoSaveTimings(string path, BenchmarkResult data)
        => File.WriteAllText(path, GenerateTimingsCsv(data.Timings));

    public static void DoSaveSummary(string path, BenchmarkResult data)
        => File.WriteAllText(path, JsonSerializerUtility.Prettify(GenerateSummaryJson(data.Timings)));

    public static void DoSaveReport(string path, BenchmarkResult data)
    {
        System.Text.StringBuilder report = new System.Text.StringBuilder();
        JsonSerializerUtility.BeginObject(report);
        JsonSerializerUtility.SerializeDefault(report, "ReportVersion", "1.1.0");
        JsonSerializerUtility.SerializeDefault(report, "ConstellationVersion", Application.version);
        JsonSerializerUtility.SerializeDefault(report, "ReportDateTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
        JsonSerializerUtility.SerializeDefault(report, "BuiltPlayer", !Application.isEditor);
        JsonSerializerUtility.SerializeDefault(report, "DisplayResolution", Screen.currentResolution);
        JsonSerializerUtility.SerializeDefault(report, "FullscreenMode", Screen.fullScreenMode);
        JsonSerializerUtility.SerializeDefault(report, "BenchmarkConfigName", data.BenchmarkConfig.Name ?? data.BenchmarkConfig.BaseFilename);
        JsonSerializerUtility.SerializeDefault(report, "CooldownDuration", data.BenchmarkConfig.CooldownTime);
        JsonSerializerUtility.SerializeDefault(report, "WarmupDuration", data.BenchmarkConfig.WarmupTime);
        JsonSerializerUtility.PrintProperty(report, "Summary", GenerateSummaryJson(data.Timings));
        JsonSerializerUtility.SerializeDefault(report, "Timings", GenerateTimingsCsv(data.Timings));
        JsonSerializerUtility.PrintProperty(report, "SimulationConfig", data.BenchmarkConfig.SimulationConfigJson);
        JsonSerializerUtility.EndObject(report);

        File.WriteAllText(path, JsonSerializerUtility.Prettify(report.ToString()));
    }

    private bool SaveFile(MonoDialog fileDialog, bool result, DataExporter fileSaveDelegate, BenchmarkResult data)
    {
        if (result == false) return true;

        string fileName = _fileDialog.FileName;

        if (File.Exists(fileName))
        {
            _fileDialog.Manager.ShowOkCancelMessageBox("Confirmation", "The file with the given name already exists." +
               " Do you want to replace it?", StandardMessageBoxIcons.Question, x => { if (x) DoSaveFile(fileName); return true; }, _fileDialog);

            return false;
        }

        DoSaveFile(fileName);
        return true;

        void DoSaveFile(string path)
        {
            fileDialog.OnDialogClosing = null;
            fileDialog.CloseDialog(true);

            try
            {
                fileSaveDelegate(path, data);
                _fileDialog.Manager.ShowMessageBox("Success", "File saved successfully", StandardMessageBoxIcons.Info);
            }
            catch (Exception e)
            {
                _fileDialog.Manager.ShowMessageBox("Error", $"Could not save file: {e.Message}", StandardMessageBoxIcons.Error);
            }                
        }
    }

    public delegate void DataExporter(string path, BenchmarkResult data);
}

public enum BenchmarkMode { Custom, BenchmarkFile, BenchmarkSuite }

public class BenchmarkResult
{
    public BenchmarkConfig BenchmarkConfig { get; set; }
    public DateTime BenchmarkDateTime { get; set; }
    public FullScreenMode FullScreenMode { get; set; }
    public Resolution ScreenResolution { get; set; }
    public List<float> Timings { get; set; }

    public BenchmarkResult(BenchmarkConfig config, List<float> timings)
    {
        BenchmarkConfig = config;
        BenchmarkDateTime = DateTime.Now;
        Timings = timings;
        FullScreenMode = Screen.fullScreenMode;
        ScreenResolution = Screen.currentResolution;
    }
}

public class BenchmarkNameGetter : IStringTransformer
{
    public string Transform(string benchmarkPath) {
        try {
            return benchmarkPath is null ? null : BenchmarkConfig.FromFile(benchmarkPath).Name;
        }
        catch {
            return "<color=red>Error!</color>";
        }
    }
}

public class BenchmarkSuiteNameGetter : IStringTransformer
{
    public string Transform(string benchmarkSuitePath)
    {
        try
        {
            if (benchmarkSuitePath is null) return null;
            var suite = BenchmarkSuiteConfig.FromFile(benchmarkSuitePath);
            int totalBenchmarks = suite.Configs.Count + suite.ConfigsFailedToLoad;
            return $"{suite.Name} ({suite.Configs.Count} / {totalBenchmarks} benchmarks)";
        }
        catch
        {
            return "<color=red>Error!</color>";
        }
    }
}
