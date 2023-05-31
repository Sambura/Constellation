using UnityEngine;
using UnityEngine.UI;
using ConstellationUI;

using Toggle = UnityEngine.UI.Toggle;

public class DebugTabPresenter : MonoBehaviour
{
	[Header("General")]
	[SerializeField] private FragmentationVisualization _fragmentation;

	[Header("Fragmentation visualisation")]
	[SerializeField] private Toggle _showCellBordersToggle;
	[SerializeField] private ColorPickerButton _cellBordersColorButton;
	[SerializeField] private Toggle _showCellsToggle;
	[SerializeField] private ColorPickerButton _cellsColorButton;

	private void Start()
	{
		// UI initialization
		_showCellBordersToggle.isOn = _fragmentation.ShowCellBorders;
		_showCellsToggle.isOn = _fragmentation.ShowCells;
		_cellBordersColorButton.Color = _fragmentation.CellBorderColor;
		_cellsColorButton.Color = _fragmentation.CellColor;

		// Set up event listeners
		_showCellBordersToggle.onValueChanged.AddListener(OnShowCellBordersChanged);
		_fragmentation.ShowCellBordersChanged += OnShowCellBordersChanged;

		_showCellsToggle.onValueChanged.AddListener(OnShowCellsChanged);
		_fragmentation.ShowCellsChanged += OnShowCellsChanged;

		_cellBordersColorButton.ColorChanged += OnCellBorderColorChanged;
		_fragmentation.CellBorderColorChanged += OnCellBorderColorChanged;

		_cellsColorButton.ColorChanged += OnCellColorChanged;
		_fragmentation.CellColorChanged += OnCellColorChanged;
	}

	private void OnCellColorChanged(Color value)
	{
		_fragmentation.CellColor = value;
		_cellsColorButton.Color = value;
	}

	private void OnCellBorderColorChanged(Color value)
	{
		_fragmentation.CellBorderColor = value;
		_cellBordersColorButton.Color = value;
	}

	private void OnShowCellBordersChanged(bool value)
	{
		_fragmentation.ShowCellBorders = value;
		_showCellBordersToggle.SetIsOnWithoutNotify(value);
	}

	private void OnShowCellsChanged(bool value)
	{
		_fragmentation.ShowCells = value;
		_showCellsToggle.SetIsOnWithoutNotify(value);
	}
}
