using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SliderWithText : MonoBehaviour
{
	[SerializeField] private Slider _slider;
	[SerializeField] private TMP_InputField _inputField;
	[SerializeField] private bool _keepSliderConstraints;
	[SerializeField] private float _minValue;
	[SerializeField] private float _maxValue;
	[SerializeField] private float _value;
	[SerializeField] private string _inputFormatting = "0.000";

	public float Value
	{
		get => _value;
		set
		{
			if (value == _value) return;
			SetValueWithoutNotify(value);
			ValueChanged?.Invoke(_value);
		}
	}
	public event Action<float> ValueChanged;

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
		if (float.TryParse(text, out float value))
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
