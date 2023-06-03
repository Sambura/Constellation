using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Text.RegularExpressions;

namespace ConstellationUI
{
	public class MinMaxSliderWithInput : LabeledUIElement
	{
		[Header("Objects")]
		[SerializeField] private MinMaxSlider _slider;
		[SerializeField] private TMP_InputField _minInputField;
		[SerializeField] private TMP_InputField _maxInputField;
		[SerializeField] private TextMeshProUGUI _lowerLabel;
		[SerializeField] private TextMeshProUGUI _higherLabel;

		[Header("Parameters")]
		[SerializeField] private bool _keepSliderConstraints = true;
		[SerializeField] private float _minValue;
		[SerializeField] private float _maxValue;
		[SerializeField] private float _lowerValue;
		[SerializeField] private float _higherValue;
		[SerializeField] private string _inputFormatting = "0.000";
		[SerializeField] private string _inputRegex = @"([-+]?[0-9]*\.?[0-9]+)";
		[SerializeField] private int _regexGroupIndex = 1;
		[SerializeField] private float _minMaxSpacing = 0;

		public float LowerValue
		{
			get => _lowerValue;
			set
			{
				if (value == _lowerValue) return;
				int intValue = IntLowerValue;
				SetLowerValueWithoutNotify(value);
				LowerValueChanged?.Invoke(_lowerValue);
				int newIntValue = IntLowerValue;
				if (intValue != newIntValue) IntLowerValueChanged?.Invoke(newIntValue);
			}
		}
		public int IntLowerValue => Mathf.RoundToInt(_lowerValue);

		public float HigherValue
		{
			get => _higherValue;
			set
			{
				if (value == _higherValue) return;
				int intValue = IntHigherValue;
				SetHigherValueWithoutNotify(value);
				HigherValueChanged?.Invoke(_higherValue);
				int newIntValue = IntHigherValue;
				if (intValue != newIntValue) IntHigherValueChanged?.Invoke(newIntValue);
			}
		}
		public int IntHigherValue => Mathf.RoundToInt(_higherValue);

		public event Action<float> LowerValueChanged;
		public event Action<int> IntLowerValueChanged;
		public event Action<float> HigherValueChanged;
		public event Action<int> IntHigherValueChanged;

		protected float ActualMinMaxRange => HigherValue - LowerValue;

		public float MinValue
		{
			get => _minValue;
			set
			{
				if (value == _minValue) return;
				_minValue = value;
				LowerValue = Clamp(LowerValue);
				HigherValue = Clamp(HigherValue);
			}
		}
		public float MaxValue
		{
			get => _maxValue;
			set
			{
				if (value == _maxValue) return;
				_maxValue = value;
				LowerValue = Clamp(LowerValue);
				HigherValue = Clamp(HigherValue);
			}
		}
		public float MinSliderValue
		{
			get => _slider.minValue;
			set
			{
				if (_slider.minValue == value) return;
				_slider.minValue = value;
				MinValue = Mathf.Min(MinValue, value);
			}
		}
		public float MaxSliderValue
		{
			get => _slider.maxValue;
			set
			{
				if (_slider.maxValue == value) return;
				_slider.maxValue = value;
				MaxValue = Mathf.Max(MaxValue, value);
			}
		}
		public string InputFormatting
		{
			get => _inputFormatting;
			set
			{
				if (_inputFormatting == value) return;
				_inputFormatting = value;
				UpdateInputFieldValues(true);
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
		public string LowerLabel
		{
			get => _lowerLabel == null ? null : _lowerLabel.text;
			set { if (_lowerLabel != null) _lowerLabel.text = value; }
		}
		public string HigherLabel
		{
			get => _higherLabel == null ? null : _higherLabel.text;
			set { if (_higherLabel != null) _higherLabel.text = value; }
		}
		public float MinMaxSpacing
		{
			get => _minMaxSpacing;
			set
			{
				_minMaxSpacing = value;
				if (ActualMinMaxRange - _minMaxSpacing < 0)
					HigherValue = LowerValue + _minMaxSpacing;
				if (ActualMinMaxRange - _minMaxSpacing < 0)
					LowerValue = HigherValue - _minMaxSpacing;
			}
		}

		private Regex _regex;

		public void SetLowerValueWithoutNotify(float value)
		{
			_lowerValue = Clamp(value);
			HigherValue = Mathf.Max(HigherValue, _lowerValue + MinMaxSpacing);
			_slider.SetMinSliderValueWithoutNotify(Mathf.Clamp(_lowerValue, _slider.minValue, _slider.maxValue));
			UpdateInputFieldValues();
		}

		public void SetHigherValueWithoutNotify(float value)
		{
			_higherValue = Clamp(value);
			LowerValue = Mathf.Min(LowerValue, _higherValue - MinMaxSpacing);
			_slider.SetMaxSliderValueWithoutNotify(Mathf.Clamp(_higherValue, _slider.minValue, _slider.maxValue));
			UpdateInputFieldValues();
		}

		private void UpdateInputFieldValues(bool force = false)
		{
			if (force || _minInputField.isFocused == false)
				_minInputField.SetTextWithoutNotify(_lowerValue.ToString(_inputFormatting));

			if (force || _maxInputField.isFocused == false)
				_maxInputField.SetTextWithoutNotify(_higherValue.ToString(_inputFormatting));
		}

		private float Clamp(float value) => Mathf.Clamp(value, _minValue, _maxValue);

		private void OnSliderLowerValueChanged(float value) => LowerValue = value;
		private void OnSliderHigherValueChanged(float value) => HigherValue = value;

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

		private void OnLowerInputFieldValueChanged(string text) { if (TryParseInput(text, out float value)) LowerValue = value; }
		private void OnHigherInputFieldValueChanged(string text) { if (TryParseInput(text, out float value)) HigherValue = value; }

		private void OnInputFieldSubmit(string text) => UpdateInputFieldValues(true);

		private void Start()
		{
			if (_keepSliderConstraints == false)
			{
				_slider.minValue = _minValue;
				_slider.maxValue = _maxValue;
			}
			_regex = new Regex(_inputRegex, RegexOptions.Compiled);

			SetLowerValueWithoutNotify(_lowerValue);
			SetHigherValueWithoutNotify(_higherValue);

			_slider.MinSliderValueChanged += OnSliderLowerValueChanged;
			_slider.MaxSliderValueChanged += OnSliderHigherValueChanged;
			_minInputField.onValueChanged.AddListener(OnLowerInputFieldValueChanged);
			_minInputField.onDeselect.AddListener(OnInputFieldSubmit);
			_minInputField.onSubmit.AddListener(OnInputFieldSubmit);
			_maxInputField.onValueChanged.AddListener(OnHigherInputFieldValueChanged);
			_maxInputField.onDeselect.AddListener(OnInputFieldSubmit);
			_maxInputField.onSubmit.AddListener(OnInputFieldSubmit);
		}

		private void OnDestroy()
		{
			if (_slider != null)
			{
				_slider.MinSliderValueChanged -= OnSliderLowerValueChanged;
				_slider.MaxSliderValueChanged -= OnSliderHigherValueChanged;
			}
			_minInputField?.onValueChanged.RemoveListener(OnLowerInputFieldValueChanged);
			_minInputField?.onDeselect.RemoveListener(OnInputFieldSubmit);
			_minInputField?.onSubmit.RemoveListener(OnInputFieldSubmit);
			_maxInputField?.onValueChanged.RemoveListener(OnHigherInputFieldValueChanged);
			_maxInputField?.onDeselect.RemoveListener(OnInputFieldSubmit);
			_maxInputField?.onSubmit.RemoveListener(OnInputFieldSubmit);
		}
	}
}