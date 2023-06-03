using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace ConstellationUI
{
	public class GradientPickerButton : LabeledUIElement, IPointerClickHandler
	{
		[Header("Objects")]
		[SerializeField] private GradientImage _viewport;
		[SerializeField] private GradientPicker _gradientPicker;

		[Header("Parameters")]
		[SerializeField] private Vector2 _gradientPickerOffset;
		[SerializeField] private Vector2 _gradientPickerPivot;
		[SerializeField] private string _gradientPickerTitle = "Select gradient";
		[SerializeField] private bool _findGradientPicker = true;

		public Gradient Gradient
		{
			get => _viewport.Gradient;
			set { if (value != _viewport.Gradient) { _viewport.Gradient = value; GradientChanged?.Invoke(value); } }
		}
		public event Action ButtonClick;
		public event Action<Gradient> GradientChanged;

		private RectTransform _transform;

		public GradientPicker GradientPicker
		{
			get => _gradientPicker != null ? _gradientPicker : (_findGradientPicker ? _gradientPicker = FindObjectOfType<GradientPicker>(true) : null);
			/* Set can be added as needed, but proper support for dynamic property set may be a pain to implement */
		}

		public string DialogTitle
		{
			get => _gradientPickerTitle;
			set
			{
				if (_gradientPickerTitle == value) return;
				_gradientPickerTitle = value;
				/* TODO add dynamic title change for color picker */
			}
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			ButtonClick?.Invoke();
			OpenGradientPicker();
		}

		private void OnGradientPickerGradientChange(Gradient gradient) => Gradient = gradient;

		private void OpenGradientPicker()
		{
			Vector2 zeroPosition = (Vector2)_transform.position + _transform.rect.position;
			Vector2 pivotPosition = zeroPosition + _transform.rect.size * _gradientPickerPivot;
			GradientPicker.ShowDialog(_gradientPickerTitle);
			GradientPicker.Position = pivotPosition + _gradientPickerOffset;
			GradientPicker.Gradient = Gradient;
			GradientPicker.OnGradientChanged = OnGradientPickerGradientChange;
		}

		private void Awake() { _transform = GetComponent<RectTransform>(); _viewport.Gradient = new Gradient(); }
	}
}