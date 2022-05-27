using UnityEngine;
using UnityEngine.UI;

public class MiscTabPresenter : MonoBehaviour
{
	[Header("Components")]
	[SerializeField] private ApplicationController _application;
	[SerializeField] private StaticTimeFPSCounter _fpsCounter;
	[SerializeField] private GameObject _fpsCounterObject;

	[Header("FPS")]
	[SerializeField] private SliderWithText _fpsLimitSlider;
	[SerializeField] private Toggle _showFpsToggle;
	[SerializeField] private SliderWithText _timeWindowSlider;

	[Header("Other")]
	[SerializeField] private Button _exitButton;

	private void Start()
	{
		// UI initialization
		_fpsLimitSlider.Value = _application.TargetFrameRate;
		_showFpsToggle.isOn = _fpsCounterObject.activeSelf;
		_timeWindowSlider.Value = _fpsCounter.TimeWindow;

		// Set up listeners
		_exitButton.onClick.AddListener(OnExitButtonClick);

		_fpsLimitSlider.IntValueChanged += OnTargetFrameRateChanged;
		_application.TargetFrameRateChanged += OnTargetFrameRateChanged;

		_showFpsToggle.onValueChanged.AddListener(OnShowFpsValueChanged);

		_timeWindowSlider.ValueChanged += OnTimeWindowValueChanged;
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
}

