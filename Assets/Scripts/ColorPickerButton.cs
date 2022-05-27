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

    public Color Color
	{
		get => _colorImage.color;
		set { if (_colorImage.color != value) { _colorImage.color = value; ColorChanged?.Invoke(value); } }
	}
	public event Action ButtonClick;
	public event Action<Color> ColorChanged;

	private RectTransform _transform;

	public void OnPointerClick(PointerEventData eventData)
	{
		ButtonClick?.Invoke();
		OpenColorPicker();
	}

	private void OnColorPickerColorChange(Color color) => Color = color;

	private void OpenColorPicker()
	{
		Vector2 zeroPosition = (Vector2)_transform.position + _transform.rect.position;
		Vector2 pivotPosition = zeroPosition + _transform.rect.size * _colorPickerPivot;
		_colorPicker.transform.position = pivotPosition + _colorPickerOffset;
		_colorPicker.gameObject.SetActive(true);
		_colorPicker.OnColorChanged = OnColorPickerColorChange;
		_colorPicker.Color = Color;
	}

	private void Awake()
	{
		_transform = GetComponent<RectTransform>();
	}

#if UNITY_EDITOR

	private void OnDrawGizmosSelected()
	{
		//Gizmos.
	}

#endif
}
