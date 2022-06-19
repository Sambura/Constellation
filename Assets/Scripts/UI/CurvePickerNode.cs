using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CurvePickerNode : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
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
		set { if (_isSelected != value) { SetSelected(value); SelectedChanged?.Invoke(this, value); } }
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
	public event Action<CurvePickerNode, bool> SelectedChanged;

	public Keyframe Data;

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

	private void SetSelected(bool value)
	{
		_isSelected = value;

		_image.color = value ? _selectedColor : _deselectedColor;
		_transform.sizeDelta = value ? _selectedSize : _deselectedSize;
	}

	private void SetPosition(Vector3 pointerPosition)
	{
		Vector3 newPosition = pointerPosition;
		if (newPosition == _transform.position) return;
		Vector3 local = _viewport.InverseTransformPoint(newPosition);
		local.x = Mathf.Clamp(local.x, _viewport.rect.xMin, _viewport.rect.xMax);
		local.y = Mathf.Clamp(local.y, _viewport.rect.yMin, _viewport.rect.yMax);
		_transform.position = _viewport.TransformPoint(local);
		Vector2 normal = UIPositionHelper.LocalToNormalizedPosition(_viewport, local);
		Data.time = normal.x;
		Data.value = normal.y;
	}

	private void Awake()
	{
		_image = GetComponent<Image>();
		_transform = GetComponent<RectTransform>();
		_viewport = transform.parent.GetComponent<RectTransform>();
		Data = new Keyframe();
		Data.inWeight = 0.1f;
		Data.outWeight = 0.1f;
		Data.weightedMode = WeightedMode.Both;
		SetSelected(false);
	}

	private void Update()
	{
		if (_isDragging) Position = Input.mousePosition + _dragOffset;
	}
}
