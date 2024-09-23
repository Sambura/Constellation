using ConfigSerialization.Structuring;
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
    private bool _wasFpsCounterEnabled;

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
    private string _currentTimingExportPath;
    private int _currentSuiteIndex;
    private int _successfulSuiteRuns;
    private string _currentSimulationConfigJson;

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
    [FilePathProperty("Select benchmark config file", true, new string[] { "Json files", "*.json", "All files",  "*" }, typeof(BenchmarkNameGetter), name: "Benchmark:")]
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
    [SliderProperty(1, 120, 0)]
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
    [SliderProperty(0, 30, 0)]
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
    [SliderProperty(0.1f, 30, 0)]
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
    // [SliderProperty(1, 50, 1, name: "Repeat count")]
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
    [SliderProperty(1, 300, 0, name: "Override benchmark duration")]
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
    [SliderProperty(0, 30, 0, name: "Override cooldown duration")]
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
    [SliderProperty(0.1f, 30, 0, name: "Override warmup duration")]
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
    // [SliderProperty(1, 50, 1, name: "Suite repeat count")]
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
    // [ConfigProperty]
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

    [ConfigGroupMember] [ConfigGroupToggle(20, 21)]
    [ConfigMemberOrder(4)]
    [ConfigProperty] public bool AutomaticBufferSize
    {
        get => _automaticBufferSize;
        set
        {
            if (_automaticBufferSize == value) return;
            _automaticBufferSize = value;
            AutomaticBufferSizeChanged?.Invoke(value);
        }
    }

    [ConfigGroupMember(groupIndex: 20, parentIndex: 0)] [SliderProperty(0, 2, 0, inputFormatting: "0.0", name: "Buffer size margin")] 
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
                if (newSuite.Configs.Count == 0) throw new Exception("Could not find any valid benchmark configs for this suite");
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

    public float FPSMeasuringDelay { get; set; } = 1f;

    // make methods to wrap these try/catches ?
    /// <summary>
    /// Loads simulation config specified in benchmark config (could be null)
    /// If any error is encountered returns false, and shows an error message box (if verbose is true)
    /// </summary>
    private bool LoadBenchmarkConfig(BenchmarkConfig config, bool verbose = true)
    {
        string path = config?.SimulationConfigPath;
        if (path is null) {
            if (verbose)
                _fileDialog.Manager.ShowMessageBox("Error", $"Benchmark config is invalid or does not provide simulation config path", StandardMessageBoxIcons.Error);
            return false;
        }
        try
        {
            if (_configSerializer.DeserializeConfig(path) <= 0)
            {
                if (verbose)
                    _fileDialog.Manager.ShowMessageBox("Error", "No data deserialized form the simulation config", StandardMessageBoxIcons.Error);
                return false;
            }
        }
        catch (JsonSerializerException e)
        {
            if (verbose)
                _fileDialog.Manager.ShowMessageBox("Error", $"Couldn't load simulation config: {e.Message}", StandardMessageBoxIcons.Error);
            return false;
        }
        catch (FileNotFoundException)
        {
            if (verbose)
                _fileDialog.Manager.ShowMessageBox("Error", $"Couldn't find simulation config file at: {path}", StandardMessageBoxIcons.Error);
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
    /// Just returns CooldownDuration property, unless BenchmarkMode is set to BenchmarkSuite
    /// Otherwise returns either override value from suite config, or value from benchmark config, or CooldownDuration
    /// </summary>
    private float GetCooldownDuration()
    {
        if (BenchmarkMode != BenchmarkMode.BenchmarkSuite) return CooldownDuration;

        float configValue = _currentBenchmarkConfig is { } ? _currentBenchmarkConfig.CooldownTime.GetValueOrDefault(CooldownDuration) : CooldownDuration;
        return CooldownDurationOverride.GetValueOrDefault(configValue);
    }

    /// <summary>
    /// Same as GetCooldownDuration() but for warmup
    /// </summary>
    private float GetWarmupDuration()
    {
        if (BenchmarkMode != BenchmarkMode.BenchmarkSuite) return WarmupDuration;

        float configValue = _currentBenchmarkConfig is { } ? _currentBenchmarkConfig.WarmupTime.GetValueOrDefault(WarmupDuration) : WarmupDuration;
        return WarmupDurationOverride.GetValueOrDefault(configValue);
    }

    /// <summary>
    /// Same as GetCooldownDuration() but for benchmark duration
    /// </summary>
    private float GetBenchmarkDuration()
    {
        if (BenchmarkMode != BenchmarkMode.BenchmarkSuite) return BenchmarkDuration;

        float configValue = _currentBenchmarkConfig is { } ? _currentBenchmarkConfig.BenchmarkDuration.GetValueOrDefault(BenchmarkDuration) : BenchmarkDuration;
        return BenchmarkDurationOverride.GetValueOrDefault(configValue);
    }

    /// <summary>
    /// Actually starts the benchmark. Does not apply any configs/settings before starting
    /// </summary>
    private void DoStartBenchmark()
    {
        _currentSimulationConfigJson = _configSerializer.GetCurrentConfigJson(false);
        DisableUI();
        try
        {
            _particleController.RestartSimulation();

            if (_tracker is { }) { Destroy(_tracker); _tracker = null; }
            _tracker = gameObject.AddComponent<FrameTimingTracker>();
            if (AutomaticBufferSize)
            {
                _helperFpsCounter = gameObject.AddComponent<StaticTimeFPSCounter>();
                _helperFpsCounter.TimeWindow = FPSMeasuringDelay;
            }
            StartCoroutine(StartTrackingDelayed(AutomaticBufferSize ? FPSMeasuringDelay : 0, GetCooldownDuration(), GetWarmupDuration()));
        }
        catch (Exception e)
        {
            RestoreUI();
            _fileDialog.Manager.ShowMessageBox("Error", $"Unknown error while starting benchmark: {e.Message}", StandardMessageBoxIcons.Error);
        }
    }

    private void StartSuiteIteration()
    {
        if (_currentBenchmarkSuiteConfig.Configs.Count <= _currentSuiteIndex)
        {
            OnSuiteFinish();
            return;
        }
        BenchmarkConfig config = _currentBenchmarkSuiteConfig.Configs[_currentSuiteIndex++];
        if (config.BenchmarkDuration.HasValue)
            BenchmarkDuration = config.BenchmarkDuration.Value;
        if (!LoadBenchmarkConfig(config, false)) { StartSuiteIteration(); return; }
        _successfulSuiteRuns++;
        DoStartBenchmark();
    }

    private void OnSuiteFinish()
    {
        _currentTimingExportPath = null;
        RestoreUI();

        _fileDialog.Manager.ShowMessageBox("Benchmark suite finished",
            $"{_successfulSuiteRuns} / {_currentBenchmarkSuiteConfig.Configs.Count} benchmarks executed successfully", 
            StandardMessageBoxIcons.Success);
    }

    [SetComponentProperty(typeof(UnityEngine.UI.Image), "color", typeof(Color), new object[] { 1f, 0.7733893f, 0.4198113f }, "Border")]
    [SetComponentProperty(typeof(TMPro.TextMeshProUGUI), "color", typeof(Color), new object[] { 0.8862745f, 0.6864019f, 0f })]
    [SetComponentProperty(typeof(UnityEngine.UI.Button), "colors.normalColor", typeof(Color), new object[] { 1f, 0.784871f, 0.2877358f, 0.1843137f })]
    [ConfigGroupMember]
    [InvokableMethod]
    [ConfigMemberOrder(-3)]
    public void StartBenchmark()
    {
        if (BenchmarkMode == BenchmarkMode.BenchmarkFile && !LoadBenchmarkConfig(_currentBenchmarkConfig)) return;
        if (BenchmarkMode == BenchmarkMode.BenchmarkSuite)
        {
            if (_currentBenchmarkSuiteConfig is null)
            {
                _fileDialog.Manager.ShowMessageBox("Error", $"No benchmark suite selected", StandardMessageBoxIcons.Error);
                return;
            }

            if (_currentTimingExportPath is null)
            {
                _fileDialog.SyncCurrentDirectory(this);
                _fileDialog.ShowDialog("Select directory to export timings", (x, y) =>
                {
                    if (!y) return true;
                    _currentTimingExportPath = _fileDialog.CurrentDirectory.FullName;
                    return true;
                });
                return;
            }
        }

        if (BenchmarkMode != BenchmarkMode.BenchmarkSuite)
        {
            _wasFpsCounterEnabled = _fpsCounter.enabled;
            DoStartBenchmark();
            return;
        }

        _fileDialog.Manager.ShowMessageBox("Alert", "You are about to start benchmark suite! Please <color=yellow>close all other applications</color> and " +
            "make sure that the <color=orange>power mode of your PC is configured</color> correctly. <color=#b0b0b0>Refrain from using your PC until the benchmark " +
            "is complete to ensure the best measurement results</color>", StandardMessageBoxIcons.Info, null, (x, y) => { StartCoroutine(DoStartBenchmarkSuite()); return true; });
    }

    private IEnumerator DoStartBenchmarkSuite()
    {
        _wasFpsCounterEnabled = _fpsCounter.enabled;
        _successfulSuiteRuns = 0;
        _currentSuiteIndex = 0;
        _applicationController.FullScreenMode = _currentBenchmarkSuiteConfig.FullscreenMode;
        _applicationController.TargetFrameRate = _currentBenchmarkSuiteConfig.FpsCap;

        yield return null; // wait one frame for viewport dimensions to update (due to chaning fullscreen mode)

        StartSuiteIteration();
    }

    [ConfigGroupMember] [InvokableMethod] [ConfigMemberOrder(-2)]
    public void ShowReport()
    {
        if (_tracker is null || _tracker.FramesCaptured == 0)
        {
            _fileDialog.Manager.ShowMessageBox("Warning", "No frame timing data was captured yet", StandardMessageBoxIcons.Warning);
            return;
        }

        _perfDialog.OnExportReportClick = x => ExportReport();
        _perfDialog.OnExportSummaryClick = x => ExportSummary();
        _perfDialog.OnExportTimingsClick = x => ExportFrameTimings();
        _perfDialog.ShowDialog();
        _perfDialog.UpdateReport(_tracker.FrameTimings);
    }

    [ConfigGroupMember] [InvokableMethod] [ConfigMemberOrder(-1)]
    public void ExportFrameTimings()
    {
        ExportTrackedData(
            fileFilters: new List<FileDialog.FileFilter>() { new FileDialog.FileFilter() { Description = "CSV files", Pattern = "*.csv" } },
            defaultFilename: "timings.csv",
            fileSaveDelegate: DoSaveTimings
        );
    }

    public void ExportSummary()
    {
        ExportTrackedData(
            fileFilters: new List<FileDialog.FileFilter>() { new FileDialog.FileFilter() { Description = "JSON files", Pattern = "*.json" } },
            defaultFilename: "summary.json",
            fileSaveDelegate: DoSaveSummary
        );
    }

    public void ExportReport()
    {
        ExportTrackedData(
            fileFilters: new List<FileDialog.FileFilter>() { new FileDialog.FileFilter() { Description = "JSON files", Pattern = "*.json" } },
            defaultFilename: "report.json",
            fileSaveDelegate: DoSaveReport
        );
    }

    private void ExportTrackedData(List<FileDialog.FileFilter> fileFilters, string defaultFilename, Action<string> fileSaveDelegate)
    {
        if (_tracker is null || _tracker.FramesCaptured == 0)
        {
            _fileDialog.Manager.ShowMessageBox("Warning", "No frame timing data was captured yet", StandardMessageBoxIcons.Warning);
            return;
        }
        
        _fileDialog.FileFilters = fileFilters;
        _fileDialog.FileName = defaultFilename;
        _fileDialog.ShowDialog("Select save location", (x, y) => SaveFile(x, y, fileSaveDelegate));
        _fileDialog.SyncCurrentDirectory(this);
    }

    private string GenerateTimingsCsv()
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int i = 0; i < _tracker.FramesCaptured; i++)
        {
            builder.Append(_tracker.Buffer[i].ToString("G9"));
            if (i != _tracker.FramesCaptured - 1) builder.Append(", ");
        }
        return builder.ToString();
    }

    private string GenerateSummaryJson() {
        FrameStatistics stats = new FrameStatistics();
        stats.SetFrameTimings(_tracker.FrameTimings);
        return DefaultJsonSerializer.Default.ToJson(stats);
    }

    public bool DoSaveFile(string path, Action<string> fileSaveDelegate, string errorCaption = "Could not save file: {0}")
    {
        try
        {
            fileSaveDelegate(path);
            return true;
        }
        catch (Exception e)
        {
            _fileDialog.Manager.ShowMessageBox("Error", string.Format(errorCaption, e.Message), StandardMessageBoxIcons.Error);
        }

        return false;
    }

    public void DoSaveTimings(string path) => File.WriteAllText(path, GenerateTimingsCsv());

    public void DoSaveSummary(string path) => File.WriteAllText(path, JsonSerializerUtility.Prettify(GenerateSummaryJson()));

    public void DoSaveReport(string path)
    {
        System.Text.StringBuilder report = new System.Text.StringBuilder();
        JsonSerializerUtility.BeginObject(report);
        JsonSerializerUtility.SerializeDefault(report, "ReportVersion", "1.0.0");
        JsonSerializerUtility.SerializeDefault(report, "ConstellationVersion", Application.version);
        JsonSerializerUtility.SerializeDefault(report, "ReportDateTime", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
        JsonSerializerUtility.SerializeDefault(report, "BuiltPlayer", !Application.isEditor);
        JsonSerializerUtility.SerializeDefault(report, "DisplayResolution", Screen.currentResolution);
        JsonSerializerUtility.SerializeDefault(report, "FullscreenMode", Screen.fullScreenMode);
        JsonSerializerUtility.PrintProperty(report, "Summary", GenerateSummaryJson());
        JsonSerializerUtility.SerializeDefault(report, "Timings", GenerateTimingsCsv());
        JsonSerializerUtility.PrintProperty(report, "SimulationConfig", _currentSimulationConfigJson);
        JsonSerializerUtility.EndObject(report);
        
        File.WriteAllText(path, JsonSerializerUtility.Prettify(report.ToString()));
    }

    private bool SaveFile(MonoDialog fileDialog, bool result, Action<string> fileSaveDelegate)
    {
        if (result == false) return true;

        string fileName = _fileDialog.FileName;

        if (File.Exists(fileName))
        {
            _fileDialog.Manager.ShowOkCancelMessageBox("Confirmation", "The file with the given name already exists." +
               " Do you want to replace it?", StandardMessageBoxIcons.Question, x => { if (x) FinishSave(fileName); return true; }, _fileDialog);

            return false;
        }

        FinishSave(fileName);

        void FinishSave(string path)
        {
            fileDialog.OnDialogClosing = null;
            fileDialog.CloseDialog(true);

            if (DoSaveFile(path, fileSaveDelegate))
                _fileDialog.Manager.ShowMessageBox("Success", "File saved successfully", StandardMessageBoxIcons.Info);
        }
        return true;
    }

    private IEnumerator StartTrackingDelayed(float initDelay, float cooldown, float warmup)
    {
        yield return new WaitForSeconds(initDelay);
        if (AutomaticBufferSize)
        {
            FrameTimingBufferSize = Mathf.RoundToInt(_helperFpsCounter.CurrentFps * BenchmarkDuration * (1 + AutomaticBufferSizeMargin));
            DestroyImmediate(_helperFpsCounter);
        }
        _tracker.BufferSize = FrameTimingBufferSize;
        _tracker.PrepareTracking();

        if (cooldown > 0)
        {
            // look into disabling the renderer as well (I would like to make sure that exposing `enabled` field for write will not hinder its performance!)
            int targetFps = _applicationController.TargetFrameRate;
            _applicationController.TargetFrameRate = 7; // frame rate too low == coroutines lag as well
            _particleController.enabled = false;
            _mainVisualizer.enabled = false;
            yield return new WaitForSeconds(cooldown);
            _particleController.enabled = true;
            _mainVisualizer.enabled = true;
            _applicationController.TargetFrameRate = targetFps;
        }

        yield return new WaitForSeconds(warmup);

        _tracker.StartTracking();
        StartCoroutine(StopTrackingDelayed(GetBenchmarkDuration()));
    }

    private void DisableUI()
    {
        _uiObject.SetActive(false);
        _fpsCounter.enabled = false;
        _viewport.enabled = false;
        _interactionCore.enabled = false;
    }

    private void RestoreUI()
    {
        _uiObject.SetActive(true);
        _fpsCounter.enabled = _wasFpsCounterEnabled;
        _viewport.enabled = true;
        _interactionCore.enabled = true;
    }

    private IEnumerator StopTrackingDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        _tracker.StopTracking();
        if (BenchmarkMode == BenchmarkMode.BenchmarkSuite)
        {
            string name = _currentBenchmarkSuiteConfig.Configs[_currentSuiteIndex - 1].Name;
            DoSaveReport(Path.Combine(_currentTimingExportPath, name + "-report.json"));
            StartSuiteIteration();
        } else
        {
            RestoreUI();
            ShowReport();
        }
    }
}
public enum BenchmarkMode { Custom, BenchmarkFile, BenchmarkSuite }

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
