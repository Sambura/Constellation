using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CurvePickerKnot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	[SerializeField] private Color _selectedColor;
	[SerializeField] private Color _deselectedColor;
	[SerializeField] private Vector2 _selectedSize;
	[SerializeField] private Vector2 _deselectedSize;

	private Image _image;
	private RectTransform _transform;
	private RectTransform _viewport;
	private bool _isDragging;
	private Vector3 _dragOffset;
	private bool _isSelected;

	public bool Selected
	{
		get => _isSelected;
		set
		{
			if (_isSelected == value) return;
			_isSelected = value;

			_image.color = value ? _selectedColor : _deselectedColor;
			_transform.sizeDelta = value ? _selectedSize : _deselectedSize;
			SelectedChanged?.Invoke(this, value);
		}
	}

	public Vector3 Position
	{
		get => _transform.position;
		set { if (_transform.position != value) { SetPosition(value); PositionChanged?.Invoke(value); } }
	}

	public void SetNormalizedPosition(Vector2 normalizedPosition)
	{
		Position = UIPositionHelper.NormalizedToWorldPosition(_viewport, normalizedPosition);
	}

	public event Action<Vector3> PositionChanged;
	public event Action<CurvePickerKnot, bool> SelectedChanged;

	public void OnPointerDown(PointerEventData eventData)
	{
		_isDragging = true;
		_dragOffset = _transform.position - (Vector3)eventData.position;
		Selected = true;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		_isDragging = false;
		Position = (Vector3)eventData.position + _dragOffset;
	}

	private void SetPosition(Vector3 pointerPosition)
	{
		if (pointerPosition == _transform.position) return;
		_transform.position = pointerPosition;
	}

	private void Awake()
	{
		_image = GetComponent<Image>();
		_transform = GetComponent<RectTransform>();
		_viewport = transform.parent.GetComponent<RectTransform>();
	}

	private void Update()
	{
		if (_isDragging) Position = Input.mousePosition + _dragOffset;
	}
}
