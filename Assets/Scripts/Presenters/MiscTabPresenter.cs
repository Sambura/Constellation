using UnityEngine;
using UnityEngine.UI;
using System;

public class MiscTabPresenter : MonoBehaviour
{
	[Header("General")]
	[SerializeField] private SimulationController _controller;
	//[SerializeField] private ColorPicker _colorPicker;
	//[SerializeField] private Vector2 _colorPickerOffset;
	[Header("Stuff")]
	[SerializeField] private Button _exitButton;

	//private Action<Color> _colorPicerAction;

	private void Start()
	{
		// UI initialization


		// Set up event listeners
		//_colorPicker.ColorChanged += OnColorPickerColorChanged;

		_exitButton.onClick.AddListener(OnExitButtonClick);
	}

	private void OnExitButtonClick()
	{
		Application.Quit();
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

