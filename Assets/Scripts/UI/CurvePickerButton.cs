using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class CurvePickerButton : MonoBehaviour, IPointerClickHandler
{
	[SerializeField] private CurvePickerViewport _viewport;
	[SerializeField] private CurvePicker _curvePicker;
	[SerializeField] private Vector2 _curvePickerOffset;
	[SerializeField] private Vector2 _curvePickerPivot;
	[SerializeField] private string _curvePickerTitle = "Select curve";

	public AnimationCurve Curve
	{
		get => _viewport.Curve;
		set { if (value != _viewport.Curve) { _viewport.Curve = value; CurveChanged?.Invoke(value); } }
	}
	public event Action ButtonClick;
	public event Action<AnimationCurve> CurveChanged;

	private RectTransform _transform;

	public void OnPointerClick(PointerEventData eventData)
	{
		ButtonClick?.Invoke();
		OpenCurvePicker();
	}

	private void OnCurvePickerCurveChange(AnimationCurve curve) => Curve = curve;

	private void OpenCurvePicker()
	{
		Vector2 zeroPosition = (Vector2)_transform.position + _transform.rect.position;
		Vector2 pivotPosition = zeroPosition + _transform.rect.size * _curvePickerPivot;
		_curvePicker.ShowDialog(_curvePickerTitle);
		_curvePicker.Position = pivotPosition + _curvePickerOffset;
		_curvePicker.OnCurveChanged = OnCurvePickerCurveChange;
		_curvePicker.Curve = Curve;
	}

	private void Awake() { _transform = GetComponent<RectTransform>(); _viewport.Curve = new AnimationCurve(); }
}
