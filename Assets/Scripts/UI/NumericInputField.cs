using System.Text.RegularExpressions;
using UnityEngine;
using System;
using TMPro;

namespace ConstellationUI
{
	public class NumericInputField : InputField
	{
		[Header("Input field parameters")]
		[SerializeField] private float _value;
		[SerializeField] private float _minValue = float.MinValue;
		[SerializeField] private float _maxValue = float.MaxValue;
		[SerializeField] private bool _wholeNumbers = false;
		[SerializeField] private string _inputFormatting = "0.000";
		[SerializeField] private string _inputRegex = @"([-+]?[0-9]*\.?[0-9]+)";
		[SerializeField] private int _regexGroupIndex = 1;

		private Regex _regex;

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
			set { if (_inputRegex != value) SetInputRegex(value); }
		}
		public int RegexGroupIndex
		{
			get => _regexGroupIndex;
			set { if (_regexGroupIndex != value) _regexGroupIndex = value; }
		}
		public bool WholeNumbers
		{
			get => _wholeNumbers;
			set { if (_wholeNumbers != value) SetWholeNumbers(value); }
		}

		private void SetWholeNumbers(bool value)
		{
			_wholeNumbers = value;
			_inputField.contentType = value ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.DecimalNumber;
		}
		private void SetInputRegex(string value)
		{
			_inputRegex = value;
			_regex = new Regex(value, RegexOptions.Compiled);
		}

		public event Action<float> ValueChanged;
		public event Action<int> IntValueChanged;

		public virtual void SetValueWithoutNotify(float value)
		{
			_value = Clamp(value);
			UpdateInputFieldValue();
		}

		private void UpdateInputFieldValue(bool force = false)
		{
			if (force || _inputField.isFocused == false)
				_inputField.SetTextWithoutNotify(_value.ToString(_inputFormatting));
		}

		private float Clamp(float value) => Mathf.Clamp(value, _minValue, _maxValue);

		protected override void OnInputFieldTextChanged(string text)
		{
			base.OnInputFieldTextChanged(text);
			Match match = _regex.Match(text);
			if (match.Success == false) return;
			if (float.TryParse(match.Groups[_regexGroupIndex].Value, out float value))
				Value = value;
		}

		private void OnInputFieldSubmit(string text) => UpdateInputFieldValue(true);

		protected override void Start()
		{
			SetWholeNumbers(_wholeNumbers);
			SetInputRegex(_inputRegex);
			SetValueWithoutNotify(_value);

			base.Start();
			_inputField.onDeselect.AddListener(OnInputFieldSubmit);
			_inputField.onSubmit.AddListener(OnInputFieldSubmit);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			_inputField?.onDeselect.RemoveListener(OnInputFieldSubmit);
			_inputField?.onSubmit.RemoveListener(OnInputFieldSubmit);
		}
	}
}
