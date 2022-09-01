using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class GradientPickerButton : MonoBehaviour, IPointerClickHandler
{
	[SerializeField] private GradientImage _viewport;
	[SerializeField] private GradientPicker _gradientPicker;
	[SerializeField] private Vector2 _gradientPickerOffset;
	[SerializeField] private Vector2 _gradientPickerPivot;
	[SerializeField] private string _gradientPickerTitle = "Select gradient";

	public Gradient Gradient
	{
		get => _viewport.Gradient;
		set { if (value != _viewport.Gradient) { _viewport.Gradient = value; GradientChanged?.Invoke(value); } }
	}
	public event Action ButtonClick;
	public event Action<Gradient> GradientChanged;

	private RectTransform _transform;

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
		_gradientPicker.ShowDialog(_gradientPickerTitle);
		_gradientPicker.Position = pivotPosition + _gradientPickerOffset;
		_gradientPicker.Gradient = Gradient;
		_gradientPicker.OnGradientChanged = OnGradientPickerGradientChange;
	}

	private void Awake() { _transform = GetComponent<RectTransform>(); _viewport.Gradient = new Gradient(); }
}
