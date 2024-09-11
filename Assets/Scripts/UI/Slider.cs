using UnityEngine;

namespace ConstellationUI
{
	public class Slider : NumericInputField
	{
		[Header("Objects")]
		[SerializeField] private UnityEngine.UI.Slider _slider;

		[Header("Slider parameters")]
		[SerializeField] private bool _keepSliderConstraints;

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

		public override void SetValueWithoutNotify(float value)
		{
			base.SetValueWithoutNotify(value);
			_slider.SetValueWithoutNotify(Mathf.Clamp(Value, _slider.minValue, _slider.maxValue));
		}

		private void OnSliderValueChanged(float value) { Value = value; }

		protected override void Start()
		{
			if (_keepSliderConstraints == false)
			{
				_slider.minValue = MinValue;
				_slider.maxValue = MaxValue;
			}

			base.Start();

			_slider.onValueChanged.AddListener(OnSliderValueChanged);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			_slider?.onValueChanged.RemoveListener(OnSliderValueChanged);
		}
	}
}