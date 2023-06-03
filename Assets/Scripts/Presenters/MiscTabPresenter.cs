using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MiscTabPresenter : MonoBehaviour
{
	[Header("Components")]
	[SerializeField] private ApplicationController _application;
	[SerializeField] private StaticTimeFPSCounter _fpsCounter;
	[SerializeField] private GameObject _fpsCounterObject;

	[Header("FPS")]
	[SerializeField] private ConstellationUI.Slider _fpsLimitSlider;
	[SerializeField] private Toggle _showFpsToggle;
	[SerializeField] private ConstellationUI.Slider _timeWindowSlider;
	[SerializeField] private CustomDropdown _fullscreenDropdown;

	[Header("Other")]
	[SerializeField] private Button _makeTransparentButton;
	[SerializeField] private Button _exitButton;

	private Dictionary<int, FullScreenMode> _fullscreenModeMapping = new Dictionary<int, FullScreenMode>()
	{
		{ 0, FullScreenMode.ExclusiveFullScreen },
		{ 1, FullScreenMode.FullScreenWindow },
		{ 2, FullScreenMode.Windowed },
		{ 3, FullScreenMode.MaximizedWindow },
	};

	private void Start()
	{
		// UI initialization
		_fpsLimitSlider.Value = _application.TargetFrameRate;
		_showFpsToggle.isOn = _fpsCounterObject.activeSelf;
		_timeWindowSlider.Value = _fpsCounter.TimeWindow;
		_fullscreenDropdown.value = FullscreenModeToValue(_application.FullScreenMode);

		// Set up listeners
		_exitButton.onClick.AddListener(OnExitButtonClick);

		_makeTransparentButton.onClick.AddListener(OnMakeTransparentButtonClick);

		_fpsLimitSlider.IntValueChanged += OnTargetFrameRateChanged;
		_application.TargetFrameRateChanged += OnTargetFrameRateChanged;

		_showFpsToggle.onValueChanged.AddListener(OnShowFpsValueChanged);

		_timeWindowSlider.ValueChanged += OnTimeWindowValueChanged;

		_fullscreenDropdown.onValueChanged.AddListener(OnFullscreenChanged);
		_application.FullScreenModeChanged += OnFullscreenChanged;
	}

	private int FullscreenModeToValue(FullScreenMode mode)
	{
		foreach (var value in _fullscreenModeMapping)
			if (value.Value == mode) return value.Key;

		return -1;
	}

	private void OnFullscreenChanged(int value) => OnFullscreenChanged(_fullscreenModeMapping[value]);
	private void OnFullscreenChanged(FullScreenMode value)
	{
		_application.FullScreenMode = value;
		_fullscreenDropdown.SetValueWithoutNotify(FullscreenModeToValue(value));
	}

	private void OnTimeWindowValueChanged(float value)
	{
		_fpsCounter.TimeWindow = value;
	}

	private void OnShowFpsValueChanged(bool value)
	{
		_fpsCounterObject.SetActive(value);
	}

	private void OnTargetFrameRateChanged(int value)
	{
		_application.TargetFrameRate = value;
		_fpsLimitSlider.SetValueWithoutNotify(value);
	}

	private void OnExitButtonClick() => _application.Quit();

	private void OnMakeTransparentButtonClick()
	{
		TransparentWindow tw = FindObjectOfType<TransparentWindow>(true);
		tw.MakeTransparent();
	}
}

