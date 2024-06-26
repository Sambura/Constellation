using ConfigSerialization.Structuring;
using ConfigSerialization;
using UnityEngine;
using UnityCore;
using System;
using System.Collections;
using System.Collections.Generic;
using ConstellationUI;

// Consider using Unity's FrameTimingManager
public class AnalyticsCore : MonoBehaviour
{
	[SerializeField] private FileDialog _fileDialog;
	[SerializeField] private GameObject _uiObject;
	[SerializeField] private ParticleController _particleController;
	[SerializeField] private StaticTimeFPSCounter _fpsCounter;
	[SerializeField] private Viewport _viewport;
	[SerializeField] private PerformanceReportDialog _perfDialog;

	private FrameTimingTracker _tracker;
	private float _benchmarkDuration = 10;
	private bool _automaticBufferSize = true;
	private float _autoBufferSizeMargin = 2f;
	private int _frameTimingBufferSize = 500000;
	private bool _fpsCounterPreviousState;
	private StaticTimeFPSCounter _helperFpsCounter;

	[ConfigGroupMember("Performance analysis")]
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

	[ConfigGroupMember] [ConfigGroupToggle(1, 2)]
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

	[ConfigGroupMember(1, 0)] [SliderProperty(0, 2, 0, inputFormatting: "0.0", name: "Buffer size margin")] 
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

	[ConfigGroupMember(2, 0)]
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

	[SetComponentProperty(typeof(UnityEngine.UI.Image), "color", typeof(Color), new object[] { 1f, 0.7733893f, 0.4198113f }, "Border")]
	[SetComponentProperty(typeof(TMPro.TextMeshProUGUI), "color", typeof(Color), new object[] { 0.8862745f, 0.6864019f, 0f })]
	[SetComponentProperty(typeof(UnityEngine.UI.Button), "colors.normalColor", typeof(Color), new object[] { 1f, 0.784871f, 0.2877358f, 0.1843137f })]
	[ConfigGroupMember] [InvokableMethod] [ConfigMemberOrder(-3)]
	public void StartTracking()
	{
		_uiObject.SetActive(false);
		_particleController.RestartSimulation();

		if (_tracker is { }) { Destroy(_tracker); _tracker = null; }
		_tracker = gameObject.AddComponent<FrameTimingTracker>();
		_fpsCounterPreviousState = _fpsCounter.enabled;
		_fpsCounter.enabled = false;
		_viewport.enabled = false;
		if (AutomaticBufferSize)
		{
			_helperFpsCounter = gameObject.AddComponent<StaticTimeFPSCounter>();
			_helperFpsCounter.TimeWindow = FPSMeasuringDelay;
		}

		StartCoroutine(StartTrackingDelayed(AutomaticBufferSize ? FPSMeasuringDelay : 0, TestStartDelay));
	}

	[ConfigGroupMember] [InvokableMethod] [ConfigMemberOrder(-2)]
	public void ShowReport()
	{
		if (_tracker is null || _tracker.FramesCaptured == 0)
		{
			_fileDialog.Manager.ShowMessageBox("Warning", "No frame timing data was captured yet", StandardMessageBoxIcons.Warning);
			return;
		}

		_perfDialog.ShowDialog();
		_perfDialog.UpdateReport(new List<float>(_tracker.Buffer).GetRange(0, _tracker.FramesCaptured));
	}

	[ConfigGroupMember] [InvokableMethod] [ConfigMemberOrder(-1)]
	public void ExportFrameTimings()
	{
		if (_tracker is null || _tracker.FramesCaptured == 0)
		{
			_fileDialog.Manager.ShowMessageBox("Warning", "No frame timing data was captured yet", StandardMessageBoxIcons.Warning);
			return;
		}

		_fileDialog.FileFilters = new List<FileDialog.FileFilter>() { new FileDialog.FileFilter() { Description = "CSV files", Pattern = "*.csv" } };
		_fileDialog.FileName = "timings.scv";
		_fileDialog.ShowDialog("Select save location", TrySaveTimings);
		_fileDialog.SyncCurrentDirectory(this);
	}

	private bool TrySaveTimings(MonoDialog fileDialog, bool result)
	{
		if (result == false) return true;

		string fileName = _fileDialog.FileName; // make a copy of the string

		if (System.IO.File.Exists(fileName))
		{
			_fileDialog.Manager.ShowOkCancelMessageBox("Confirmation", "The file with the given name already exists." +
			   " Do you want to replace it?", StandardMessageBoxIcons.Question, x => { if (x) return DoSaveTimings(fileName); return true; }, _fileDialog);

			return false;
		}

		DoSaveTimings(fileName);
		return true;

		bool DoSaveTimings(string path)
		{
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			for (int i = 0; i < _tracker.FramesCaptured; i++)
			{
				builder.Append(_tracker.Buffer[i].ToString("G9"));
				if (i != _tracker.FramesCaptured - 1) builder.Append(", ");
			}
			System.IO.File.WriteAllText(path, builder.ToString());
			fileDialog.OnDialogClosing = null;
			fileDialog.CloseDialog(true);

			_fileDialog.Manager.ShowMessageBox("Success", "Frame timings were saved successfully", StandardMessageBoxIcons.Info);

			return false;
		}
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

	private IEnumerator StopTrackingDelayed(float delay)
	{
		yield return new WaitForSeconds(delay);

		_tracker.StopTracking();
		_uiObject.SetActive(true);
		_fpsCounter.enabled = _fpsCounterPreviousState;
		_viewport.enabled = true;

		ShowReport();
	}
}
