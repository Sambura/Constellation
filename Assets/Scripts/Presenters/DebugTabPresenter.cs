using UnityEngine;
using UnityEngine.UI;
using System;

public class DebugTabPresenter : MonoBehaviour
{
	[Header("General")]
	[SerializeField] private SimulationController _controller;
	//[SerializeField] private ColorPicker _colorPicker;
	//[SerializeField] private Vector2 _colorPickerOffset;
	[Header("Fragmentation visualisation")]
	[SerializeField] private Toggle _showCellBordersToggle;
	[SerializeField] private Toggle _showCellsToggle;

	//private Action<Color> _colorPicerAction;

	private void Start()
	{
		// UI initialization
		_showCellBordersToggle.isOn = _controller.ShowCellBorders;
		_showCellsToggle.isOn = _controller.ShowCells;

		// Set up event listeners
		//_colorPicker.ColorChanged += OnColorPickerColorChanged;

		_showCellBordersToggle.onValueChanged.AddListener(OnShowCellBordersChanged);
		_controller.ShowCellBordersChanged += OnShowCellBordersChanged;

		_showCellsToggle.onValueChanged.AddListener(OnShowCellsChanged);
		_controller.ShowCellsChanged += OnShowCellsChanged;
	}

	private void OnShowCellBordersChanged(bool value)
	{
		_controller.ShowCellBorders = value;
		_showCellBordersToggle.SetIsOnWithoutNotify(value);
	}

	private void OnShowCellsChanged(bool value)
	{
		_controller.ShowCells = value;
		_showCellsToggle.SetIsOnWithoutNotify(value);
	}

	//private void OnColorPickerColorChanged(Color color) => _colorPicerAction(color);

	//private void OpenColorPicker(Transform button, Action<Color> colorPickAction, Color initialColor)
	//{
	//	_colorPicker.transform.position = button.position + (Vector3)_colorPickerOffset;
	//	_colorPicker.gameObject.SetActive(true);
	//	_colorPicerAction = colorPickAction;
	//	_colorPicker.Color = initialColor;
	//}
}
