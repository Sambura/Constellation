using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Text.RegularExpressions;

public class MinMaxSliderWithInput : MonoBehaviour
{
	[SerializeField] private MinMaxSlider _slider;
	[SerializeField] private TMP_InputField _minInputField;
	[SerializeField] private TMP_InputField _maxInputField;
	[SerializeField] private bool _keepSliderConstraints;
	[SerializeField] private float _minConstraint;
	[SerializeField] private float _maxConstraint;
	[SerializeField] private float _minValue;
	[SerializeField] private float _maxValue;
	[SerializeField] private string _inputFormatting = "0.000";
	[SerializeField] private string _inputRegex = @"([-+]?[0-9]*\.?[0-9]+)";
	[SerializeField] private int _regexGroupIndex = 1;

	public float MinValue
	{
		get => _minValue;
		set
		{
			if (value == _minValue) return;
			int intValue = IntMinValue;
			SetMinValueWithoutNotify(value);
			MinValueChanged?.Invoke(_minValue);
			int newIntValue = IntMinValue;
			if (intValue != newIntValue) IntMinValueChanged?.Invoke(newIntValue);
		}
	}
	public int IntMinValue => Mathf.RoundToInt(_minValue);

	public float MaxValue
	{
		get => _maxValue;
		set
		{
			if (value == _maxValue) return;
			int intValue = IntMaxValue;
			SetMaxValueWithoutNotify(value);
			MaxValueChanged?.Invoke(_maxValue);
			int newIntValue = IntMaxValue;
			if (intValue != newIntValue) IntMaxValueChanged?.Invoke(newIntValue);
		}
	}
	public int IntMaxValue => Mathf.RoundToInt(_maxValue);

	public event Action<float> MinValueChanged;
	public event Action<int> IntMinValueChanged;
	public event Action<float> MaxValueChanged;
	public event Action<int> IntMaxValueChanged;

	private Regex _regex;

	public void SetMinValueWithoutNotify(float value)
	{
		_minValue = Clamp(value);
		MaxValue = Mathf.Max(MaxValue, _minValue);
		_slider.SetMinSliderValueWithoutNotify(Mathf.Clamp(_minValue, _slider.minValue, _slider.maxValue));
		UpdateInputFieldValues();
	}

	public void SetMaxValueWithoutNotify(float value)
	{
		_maxValue = Clamp(value);
		MinValue = Mathf.Min(MinValue, _maxValue);
		_slider.SetMaxSliderValueWithoutNotify(Mathf.Clamp(_maxValue, _slider.minValue, _slider.maxValue));
		UpdateInputFieldValues();
	}

	private void UpdateInputFieldValues(bool force = false)
	{
		if (force || _minInputField.isFocused == false)
			_minInputField.SetTextWithoutNotify(_minValue.ToString(_inputFormatting));

		if (force || _maxInputField.isFocused == false)
			_maxInputField.SetTextWithoutNotify(_maxValue.ToString(_inputFormatting));
	}

	private float Clamp(float value) => Mathf.Clamp(value, _minConstraint, _maxConstraint);

	private void OnSliderMinValueChanged(float value) => MinValue = value;
	private void OnSliderMaxValueChanged(float value) => MaxValue = value;

	private bool TryParseInput(string text, out float value)
	{
		Match match = _regex.Match(text);
		if (match.Success == false)
		{
			value = 0;
			return false;
		}
		return float.TryParse(match.Groups[_regexGroupIndex].Value, out value);
	}

	private void OnMinInputFieldValueChanged(string text) { if (TryParseInput(text, out float value)) MinValue = value; }
	private void OnMaxInputFieldValueChanged(string text) { if (TryParseInput(text, out float value)) MaxValue = value; }

	private void OnInputFieldSubmit(string text) => UpdateInputFieldValues(true);

	private void Start()
	{
		if (_keepSliderConstraints == false)
		{
			_slider.minValue = _minConstraint;
			_slider.maxValue = _maxConstraint;
		}
		_regex = new Regex(_inputRegex, RegexOptions.Compiled);

		SetMinValueWithoutNotify(_minValue);
		SetMaxValueWithoutNotify(_maxValue);

		_slider.MinSliderValueChanged += OnSliderMinValueChanged;
		_slider.MaxSliderValueChanged += OnSliderMaxValueChanged;
		_minInputField.onValueChanged.AddListener(OnMinInputFieldValueChanged);
		_minInputField.onDeselect.AddListener(OnInputFieldSubmit);
		_minInputField.onSubmit.AddListener(OnInputFieldSubmit);
		_maxInputField.onValueChanged.AddListener(OnMaxInputFieldValueChanged);
		_maxInputField.onDeselect.AddListener(OnInputFieldSubmit);
		_maxInputField.onSubmit.AddListener(OnInputFieldSubmit);
	}

	private void OnDestroy()
	{
		if (_slider != null)
		{
			_slider.MinSliderValueChanged -= OnSliderMinValueChanged;
			_slider.MaxSliderValueChanged -= OnSliderMaxValueChanged;
		}
		_minInputField?.onValueChanged.RemoveListener(OnMinInputFieldValueChanged);
		_minInputField?.onDeselect.RemoveListener(OnInputFieldSubmit);
		_minInputField?.onSubmit.RemoveListener(OnInputFieldSubmit);
		_maxInputField?.onValueChanged.RemoveListener(OnMaxInputFieldValueChanged);
		_maxInputField?.onDeselect.RemoveListener(OnInputFieldSubmit);
		_maxInputField?.onSubmit.RemoveListener(OnInputFieldSubmit);
	}
}
