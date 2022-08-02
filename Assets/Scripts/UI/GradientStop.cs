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
	private float _time;

	public void SetNormalizedPosition(Vector2 normalizedPosition)
	{
		Position = UIPositionHelper.NormalizedToWorldPosition(_parent, normalizedPosition);
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
		Vector3 local = _parent.InverseTransformPoint(newPosition);
		local.x = Mathf.Clamp(local.x, _parent.rect.xMin, _parent.rect.xMax);
		local.y = _yPosition;
		_transform.position = _parent.TransformPoint(local);
		Time = UIPositionHelper.LocalToNormalizedPosition(_parent, local).x;
	}

	protected override void Awake()
	{
		base.Awake();

		_image = GetComponent<Image>();

		SetSelectedWithoutNotify(false);
	}
}