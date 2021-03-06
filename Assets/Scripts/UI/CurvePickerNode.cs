using UnityEngine;
using UnityEngine.UI;

public class CurvePickerNode : MonoSelectable
{
    [SerializeField] private Color _selectedColor;
    [SerializeField] private Color _deselectedColor;
    [SerializeField] private Vector2 _selectedSize;
    [SerializeField] private Vector2 _deselectedSize;

    private Image _image;
	private RectTransform _transform;
	private RectTransform _viewport;

	public void SetNormalizedPosition(Vector2 normalizedPosition)
	{
		Position = UIPositionHelper.NormalizedToWorldPosition(_viewport, normalizedPosition);
	}

	public Keyframe Data;

	public override void SetSelectedWithoutNotify(bool value)
	{
		base.SetSelectedWithoutNotify(value);

		_image.color = value ? _selectedColor : _deselectedColor;
		_transform.sizeDelta = value ? _selectedSize : _deselectedSize;
	}

	public override void SetPositionWithoutNotify(Vector3 pointerPosition)
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

		SetSelectedWithoutNotify(false);
	}
}
