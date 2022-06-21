using UnityEngine;
using UnityEngine.UI;

public class CurvePickerKnot : MonoSelectable
{
	[SerializeField] private Color _selectedColor;
	[SerializeField] private Color _deselectedColor;
	[SerializeField] private Vector2 _selectedSize;
	[SerializeField] private Vector2 _deselectedSize;

	private Image _image;
	private RectTransform _transform;
	private RectTransform _viewport;

	public override void SetSelectedWithoutNotify(bool value)
	{
		base.SetSelectedWithoutNotify(value);

		_image.color = value ? _selectedColor : _deselectedColor;
		_transform.sizeDelta = value ? _selectedSize : _deselectedSize;
	}

	public void SetNormalizedPosition(Vector2 normalizedPosition)
	{
		Position = UIPositionHelper.NormalizedToWorldPosition(_viewport, normalizedPosition);
	}

	private void Awake()
	{
		_image = GetComponent<Image>();
		_transform = GetComponent<RectTransform>();
		_viewport = transform.parent.GetComponent<RectTransform>();
		SetSelectedWithoutNotify(false);
	}
}
