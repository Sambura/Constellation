using UnityEngine;
using UnityEngine.UI;

public class CurvePickerKnot : MonoSelectable
{
    [SerializeField] private Color _selectedColor;
    [SerializeField] private Color _deselectedColor;
    [SerializeField] private Vector2 _selectedSize;
    [SerializeField] private Vector2 _deselectedSize;

    private Image _image;

    public override void SetSelectedWithoutNotify(bool value)
    {
        base.SetSelectedWithoutNotify(value);

        _image.color = value ? _selectedColor : _deselectedColor;
        _transform.sizeDelta = value ? _selectedSize : _deselectedSize;
    }

    public void SetNormalizedPosition(Vector2 normalizedPosition)
    {
        Position = UIPositionHelper.NormalizedToWorldPosition(_parent, normalizedPosition);
    }

    protected override void Awake()
    {
        base.Awake();

        _image = GetComponent<Image>();
        SetSelectedWithoutNotify(false);
    }
}
