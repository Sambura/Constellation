using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Text.RegularExpressions;

public class SliderWithText : MonoBehaviour
{
	[Header("Objects")]
	[SerializeField] private Slider _slider;
	[SerializeField] private TMP_InputField _inputField;
	[SerializeField] private TextMeshProUGUI _label;

	[Header("Parameters")]
	[SerializeField] private bool _keepSliderConstraints;
	[SerializeField] private float _minValue;
	[SerializeField] private float _maxValue;
	[SerializeField] private float _value;
	[SerializeField] private string _inputFormatting = "0.000";
	[SerializeField] private string _inputRegex = @"([-+]?[0-9]*\.?[0-9]+)";
	[SerializeField] private int _regexGroupIndex = 1;

	public float Value
	{
		get => _value;
		set
		{
			if (value == _value) return;
			int intValue = IntValue;
			SetValueWithoutNotify(value);
			ValueChanged?.Invoke(_value);
			int newIntValue = IntValue;
			if (intValue != newIntValue) IntValueChanged?.Invoke(newIntValue);
		}
	}
	public int IntValue
	{
		get => Mathf.RoundToInt(_value);
		set => Value = value;
	}

	public float MinValue
	{
		get => _minValue;
		set
		{
			if (value == _minValue) return;
			_minValue = value;
			Value = Clamp(Value);
		}
	}
	public float MaxValue
	{
		get => _maxValue;
		set
		{
			if (value == _maxValue) return;
			_maxValue = value;
			Value = Clamp(Value);
		}
	}
	public float MinSliderValue
	{
		get => _slider.minValue;
		set
		{
			if (_slider.minValue == value) return;
			MinValue = Mathf.Min(MinValue, value);
			_slider.minValue = value;
		}
	}
	public float MaxSliderValue
	{
		get => _slider.maxValue;
		set
		{
			if (_slider.maxValue == value) return;
			MaxValue = Mathf.Max(MaxValue, value);
			_slider.maxValue = value;
		}
	}
	public string InputFormatting
	{
		get => _inputFormatting;
		set
		{
			if (_inputFormatting == value) return;
			_inputFormatting = value;
			UpdateInputFieldValue(true);
		}
	}
	public string InputRegex
	{
		get => _inputRegex;
		set
		{
			if (_inputRegex == value) return;
			_inputRegex = value;
			_regex = new Regex(_inputRegex, RegexOptions.Compiled);
		}
	}
	public int RegexGroupIndex
	{
		get => _regexGroupIndex;
		set
		{
			if (_regexGroupIndex == value) return;
			_regexGroupIndex = value;
		}
	}
	public string TextLabel
	{
		get => _label == null ? null : _label.text;
		set { if (_label != null) _label.text = value; }
	}

	public event Action<float> ValueChanged;
	public event Action<int> IntValueChanged;

	private Regex _regex;

	public void SetValueWithoutNotify(float value)
	{
		_value = Clamp(value);
		_slider.SetValueWithoutNotify(Mathf.Clamp(_value, _slider.minValue, _slider.maxValue));
		UpdateInputFieldValue();
	}

	private void UpdateInputFieldValue(bool force = false)
	{
		if (force || _inputField.isFocused == false) 
			_inputField.SetTextWithoutNotify(_value.ToString(_inputFormatting));
	}

	private float Clamp(float value) => Mathf.Clamp(value, _minValue, _maxValue);

	private void OnSliderValueChanged(float value)
	{
		Value = value;
	}

	private void OnInputFieldValueChanged(string text)
	{
		Match match = _regex.Match(text);
		if (match.Success == false) return;
		if (float.TryParse(match.Groups[_regexGroupIndex].Value, out float value))
			Value = value;
	}

	private void OnInputFieldSubmit(string text) => UpdateInputFieldValue(true);

	private void Start()
	{
		if (_keepSliderConstraints == false)
		{
			_slider.minValue = _minValue;
			_slider.maxValue = _maxValue;
		}
		_regex = new Regex(_inputRegex, RegexOptions.Compiled);

		SetValueWithoutNotify(_value);

		_slider.onValueChanged.AddListener(OnSliderValueChanged);
		_inputField.onValueChanged.AddListener(OnInputFieldValueChanged);
		_inputField.onDeselect.AddListener(OnInputFieldSubmit);
		_inputField.onSubmit.AddListener(OnInputFieldSubmit);
	}

	private void OnDestroy()
	{
		_slider?.onValueChanged.RemoveListener(OnSliderValueChanged);
		_inputField?.onValueChanged.RemoveListener(OnInputFieldValueChanged);
		_inputField?.onDeselect.RemoveListener(OnInputFieldSubmit);
		_inputField?.onSubmit.RemoveListener(OnInputFieldSubmit);
	}
}
