using UnityEngine;
using UnityEngine.UI;

public class DebugTabPresenter : MonoBehaviour
{
	[Header("General")]
	[SerializeField] private SimulationController _controller;
	[Header("Fragmentation visualisation")]
	[SerializeField] private Toggle _showCellBordersToggle;
	[SerializeField] private ColorPickerButton _cellBordersColorButton;
	[SerializeField] private Toggle _showCellsToggle;
	[SerializeField] private ColorPickerButton _cellsColorButton;

	private void Start()
	{
		// UI initialization
		_showCellBordersToggle.isOn = _controller.ShowCellBorders;
		_showCellsToggle.isOn = _controller.ShowCells;

		// Set up event listeners
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
}
