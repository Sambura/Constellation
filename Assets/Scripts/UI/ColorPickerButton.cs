using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using TMPro;

namespace ConstellationUI
{
	public class ColorPickerButton : MonoBehaviour, IPointerClickHandler
	{
		[Header("Objects")]
		[SerializeField] private Image _colorImage;
		[SerializeField] private ColorPicker _colorPicker;
		[SerializeField] private TextMeshProUGUI _label;

		[Header("Parameters")]
		[SerializeField] private Vector2 _colorPickerOffset;
		[SerializeField] private Vector2 _colorPickerPivot;
		[SerializeField] private bool _useAlpha;
		[SerializeField] private string _colorPickerTitle = "Select color";
		[SerializeField] private bool _findColorPicker = true;

		public Color Color
		{
			get => _colorImage.color;
			set { if (_colorImage.color != value) { _colorImage.color = value; ColorChanged?.Invoke(value); } }
		}
		public event Action ButtonClick;
		public event Action<Color> ColorChanged;

		public bool Interactable { get; set; } = true;

		private RectTransform _transform;

		public ColorPicker ColorPicker
		{
			get => _colorPicker != null ? _colorPicker : (_findColorPicker ? _colorPicker = FindObjectOfType<ColorPicker>(true) : null);
			/* Set can be added as needed, but proper support for dynamic property set may be a pain to implement */
		}

		public string TextLabel
		{
			get => _label == null ? null : _label.text;
			set { if (_label != null) _label.text = value; }
		}

		public bool UseAlpha
		{
			get => _useAlpha;
			set
			{
				if (_useAlpha == value) return;
				_useAlpha = value;
				if (ColorPicker.isActiveAndEnabled) ColorPicker.UseAlpha = _useAlpha;
			}
		}

		public string DialogTitle
		{
			get => _colorPickerTitle;
			set
			{
				if (_colorPickerTitle == value) return;
				_colorPickerTitle = value;
				/* TODO add dynamic title change for color picker */
			}
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (Interactable == false) return;

			ButtonClick?.Invoke();
			OpenColorPicker();
		}

		public void CloseColorPicker()
		{
			if (ColorPicker.OnColorChanged != OnColorPickerColorChange) return;

			ColorPicker.CloseDialog(false);
		}

		private void OnColorPickerColorChange(Color color) => Color = color;

		private void OpenColorPicker()
		{
			Vector2 zeroPosition = (Vector2)_transform.position + _transform.rect.position;
			Vector2 pivotPosition = zeroPosition + _transform.rect.size * _colorPickerPivot;
			ColorPicker.ShowDialog(_colorPickerTitle);
			ColorPicker.Position = _transform.position + (Vector3)_colorPickerOffset;
			ColorPicker.OnColorChanged = OnColorPickerColorChange;
			ColorPicker.Color = Color;
			ColorPicker.UseAlpha = _useAlpha;
		}

		private void Awake()
		{
			_transform = GetComponent<RectTransform>();
		}
	}
}