using UnityEngine;
using UnityEngine.UI;
using System;

public class GradientStop : MonoSelectable
{
	[SerializeField] private Color _selectedColor;
	[SerializeField] private Color _deselectedColor;
	[SerializeField] private float _yPosition = 0;
	[SerializeField] private Image _colorImage;

	private Image _image;
	private RectTransform _transform;
	private RectTransform _viewport;
	private float _time;

	public void SetNormalizedPosition(Vector2 normalizedPosition)
	{
		Position = UIPositionHelper.NormalizedToWorldPosition(_viewport, normalizedPosition);
	}

	public Color Color
	{
		get => _colorImage.color;
		set => _colorImage.color = value;
	}
	public float Time
	{
		get => _time;
		private set { if (_time != value) { _time = value; TimeChanged?.Invoke(value); } }
	}

	public event Action<float> TimeChanged;

	public override void SetSelectedWithoutNotify(bool value)
	{
		base.SetSelectedWithoutNotify(value);

		_image.color = value ? _selectedColor : _deselectedColor;
	}

	public override void SetPositionWithoutNotify(Vector3 pointerPosition)
	{
		Vector3 newPosition = pointerPosition;
		if (newPosition == _transform.position) return;
		Vector3 local = _viewport.InverseTransformPoint(newPosition);
		local.x = Mathf.Clamp(local.x, _viewport.rect.xMin, _viewport.rect.xMax);
		local.y = _yPosition;
		_transform.position = _viewport.TransformPoint(local);
		Time = UIPositionHelper.LocalToNormalizedPosition(_viewport, local).x;
	}

	private void Awake()
	{
		_image = GetComponent<Image>();
		_transform = GetComponent<RectTransform>();
		_viewport = transform.parent.GetComponent<RectTransform>();

		SetSelectedWithoutNotify(false);
	}
}