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
	[SerializeField] private StaticTimeFPSCounter _fpsCounter;
	[SerializeField] private Viewport _viewport;
	[SerializeField] private PerformanceReportDialog _perfDialog;
	[SerializeField] private ConfigSerializer _configSerializer;
	[SerializeField] private InteractionCore _interactionCore;

	private FrameTimingTracker _tracker;
	private StaticTimeFPSCounter _helperFpsCounter;
	private bool _wasFpsCounterEnabled;

	private BenchmarkMode _benchmarkMode = BenchmarkMode.Custom;
	private float _benchmarkDuration = 10;
	private bool _automaticBufferSize = true;
	private float _autoBufferSizeMargin = 2f;
	private int _frameTimingBufferSize = 500000;
	private string _benchmarkFilePath = null;
	private string _benchmarkSuiteFilePath = null;

	private BenchmarkConfig _currentBenchmarkConfig;
	private BenchmarkSuiteConfig _currentBenchmarkSuiteConfig;
	private string _currentTimingExportPath;
	private int _currentSuiteIndex;
	private int _successfulSuiteRuns;
	private string _currentSimulationConfigJson;

	[ConfigGroupMember("Performance analysis")]
	// [ConfigGroupToggle(groupIndex: 1)]
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

    [ConfigGroupMember(groupIndex: 1, parentIndex: 0, SetDisplayIndex = 1)]
    [FilePathProperty("Select benchmark config file", true, new string[] { "Json files", "*.json", "All files",  "*" }, typeof(BenchmarkNameGetter), name: "Benchmark:")]
    public string BenchmarkFilePath
    {
        get => _benchmarkFilePath;
		set
		{
			if (_benchmarkFilePath == value) return;
            if (value is null) {
                _currentBenchmarkConfig = null;
            }
            else {
                try {
                    _currentBenchmarkConfig = BenchmarkConfig.FromFile(value);
                    _benchmarkFilePath = value;
					BenchmarkDuration = _currentBenchmarkConfig.BenchmarkDuration;
                }
                catch (JsonSerializerException e) {
                    _fileDialog.Manager.ShowMessageBox("Error", $"Couldn't load benchmark config: {e.Message}", StandardMessageBoxIcons.Error);
                }
                catch (FileNotFoundException) {
                    _fileDialog.Manager.ShowMessageBox("Error", $"Couldn't find benchmark config file at: {value}", StandardMessageBoxIcons.Error);
                } // in case of exception an event will still be generated to notify that the setter has declined the operation
				catch (Exception e) {
                    _fileDialog.Manager.ShowMessageBox("Error", $"Encountered error while reading benchmark config: {e.Message}", StandardMessageBoxIcons.Error);
                }
            }

            BenchmarkFilePathChanged?.Invoke(_benchmarkFilePath);
        }
    }

    [ConfigGroupMember(groupIndex: 2, parentIndex: 0, SetDisplayIndex = 2)]
    [SetComponentProperty(typeof(TMPro.TextMeshProUGUI), "color", typeof(Color), new object[] { 0.972549f, 0.9294117647f, 0.56470588f, 0.5f }, childName: "Labels/FilenameLabel")]
    [FilePathProperty("Select benchmark suite config file", true, new string[] { "Json files", "*.json", "All files", "*" }, typeof(BenchmarkSuiteNameGetter), name: "Benchmark suite:")]
    public string BenchmarkSuiteFilePath
    {
        get => _benchmarkSuiteFilePath;
        set
        {
            if (_benchmarkSuiteFilePath == value) return;
            if (value is null) {
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

            BenchmarkSuiteFilePathChanged?.Invoke(_benchmarkSuiteFilePath);
        }
    }

    [ConfigGroupMember]
	[SliderProperty(1, 100, 0, name: "Benchmark duration")]
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

	[ConfigGroupMember] [ConfigGroupToggle(20, 21)]
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

	public float FPSMeasuringDelay { get; set; } = 1f;
	public float TestStartDelay { get; set; } = 0.5f;

	public event Action<float> BenchmarkDurationChanged;
	public event Action<bool> AutomaticBufferSizeChanged;
	public event Action<float> AutomaticBufferSizeMarginChanged;
	public event Action<int> FrameTimingBufferSizeChanged;
    public event Action<BenchmarkMode> BenchmarkModeChanged;
    public event Action<string> BenchmarkFilePathChanged;
    public event Action<string> BenchmarkSuiteFilePathChanged;

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
                _fileDialog.Manager.ShowMessageBox("Error", $"Unknown error: {e.Message}", StandardMessageBoxIcons.Error);
            return false;
        }

        return true;
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
			StartCoroutine(StartTrackingDelayed(AutomaticBufferSize ? FPSMeasuringDelay : 0, TestStartDelay));
		}
		catch (Exception e)
		{
            RestoreUI();
            _fileDialog.Manager.ShowMessageBox("Error", $"Unknown error: {e.Message}", StandardMessageBoxIcons.Error);
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
        BenchmarkDuration = config.BenchmarkDuration;
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
			DoStartBenchmark();
			return;
		}

		_successfulSuiteRuns = 0;
		_currentSuiteIndex = 0;
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
		return DefaultJsonSerializer.Default.ToJson(stats, false);
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

	private IEnumerator StartTrackingDelayed(float initDelay, float delay)
	{
		yield return new WaitForSeconds(initDelay);
		if (AutomaticBufferSize)
		{
			FrameTimingBufferSize = Mathf.RoundToInt(_helperFpsCounter.CurrentFps * BenchmarkDuration * (1 + AutomaticBufferSizeMargin));
			DestroyImmediate(_helperFpsCounter);
		}
		_tracker.BufferSize = FrameTimingBufferSize;
		_tracker.PrepareTracking();

		yield return new WaitForSeconds(delay);

		_tracker.StartTracking();
		StartCoroutine(StopTrackingDelayed(BenchmarkDuration));
	}

	private void DisableUI()
	{
        _uiObject.SetActive(false);
        _wasFpsCounterEnabled = _fpsCounter.enabled;
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
