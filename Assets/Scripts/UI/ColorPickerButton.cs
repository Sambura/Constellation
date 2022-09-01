using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class ColorPickerButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _colorImage;
	[SerializeField] private ColorPicker _colorPicker;
	[SerializeField] private Vector2 _colorPickerOffset;
	[SerializeField] private Vector2 _colorPickerPivot;
	[SerializeField] private bool _useAlpha;
	[SerializeField] private string _colorPickerTitle = "Select color";

	public Color Color
	{
		get => _colorImage.color;
		set { if (_colorImage.color != value) { _colorImage.color = value; ColorChanged?.Invoke(value); } }
	}
	public event Action ButtonClick;
	public event Action<Color> ColorChanged;

	public bool Interactable { get; set; } = true;

	private RectTransform _transform;

	public void OnPointerClick(PointerEventData eventData)
	{
		if (Interactable == false) return;

		ButtonClick?.Invoke();
		OpenColorPicker();
	}

	public void CloseColorPicker()
	{
		if (_colorPicker.OnColorChanged != OnColorPickerColorChange) return;

		_colorPicker.CloseDialog(false);
	}

	private void OnColorPickerColorChange(Color color) => Color = color;

	private void OpenColorPicker()
	{
		Vector2 zeroPosition = (Vector2)_transform.position + _transform.rect.position;
		Vector2 pivotPosition = zeroPosition + _transform.rect.size * _colorPickerPivot;
		_colorPicker.ShowDialog(_colorPickerTitle);
		_colorPicker.Position = _transform.position + (Vector3)_colorPickerOffset;
		_colorPicker.OnColorChanged = OnColorPickerColorChange;
		_colorPicker.Color = Color;
		_colorPicker.UseAlpha = _useAlpha;
	}

	private void Awake()
	{
		_transform = GetComponent<RectTransform>();
	}
}
