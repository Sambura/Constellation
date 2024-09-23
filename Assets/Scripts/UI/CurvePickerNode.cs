using UnityEngine;
using UnityEngine.UI;

public class CurvePickerNode : MonoSelectable
{
    [SerializeField] private Color _selectedColor;
    [SerializeField] private Color _deselectedColor;
    [SerializeField] private Vector2 _selectedSize;
    [SerializeField] private Vector2 _deselectedSize;

    private Image _image;

    public void SetNormalizedPosition(Vector2 normalizedPosition)
    {
        Position = UIPositionHelper.NormalizedToWorldPosition(_parent, normalizedPosition);
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
        Vector3 local = _parent.InverseTransformPoint(newPosition);
        local.x = Mathf.Clamp(local.x, _parent.rect.xMin, _parent.rect.xMax);
        local.y = Mathf.Clamp(local.y, _parent.rect.yMin, _parent.rect.yMax);
        _transform.position = _parent.TransformPoint(local);
        Vector2 normal = UIPositionHelper.LocalToNormalizedPosition(_parent, local);
        Data.time = normal.x;
        Data.value = normal.y;
    }

    protected override void Awake()
    {
        base.Awake();

        _image = GetComponent<Image>();
        Data = new Keyframe();
        Data.inWeight = 0.1f;
        Data.outWeight = 0.1f;
        Data.weightedMode = WeightedMode.Both;

        SetSelectedWithoutNotify(false);
    }
}
