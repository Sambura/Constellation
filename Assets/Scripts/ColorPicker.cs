using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class ColorPicker : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    [SerializeField] private UIPositionHelper _colorWindow;
    [SerializeField] private UIPositionHelper _hueBar;
    [SerializeField] private SliderWithText _alphaSlider;
    [SerializeField] private Image _colorWindowImage;
    [SerializeField] private RectTransform _hueBarKnob;
    [SerializeField] private RectTransform _colorWindowKnob;
    [SerializeField] private Button _okButton;

    public Color Color
	{
        get => _color;
        set
		{
            if (_color == value) return;
            SetColor(value);
            ColorChanged?.Invoke(value);
		}
	}
    public Action<Color> OnColorChanged { get; set; }
    public event Action<Color> ColorChanged;

    private void SetColor(Color color)
	{
        _color = color;
        Color.RGBToHSV(color, out _h, out _s, out _v);
        _palleteMaterial.SetFloat(HuePropertyId, _h);
        PlaceHueBarKnob();
        PlaceColorWindowKnob();
        _alphaSlider.Value = Mathf.Round(255 * _color.a);
	}

    private void CallOnColorChanged(Color color) => OnColorChanged?.Invoke(color);

    private Color _color;
    private Material _palleteMaterial;
    private float _h;
    private float _s;
    private float _v;
    private Vector2 _clickPosition;

    private static readonly int HuePropertyId = Shader.PropertyToID("_Hue");

    private void PlaceHueBarKnob()
	{
        Vector2 position = _hueBar.NormalizedToWorldPosition(new Vector2(0, _h));
        _hueBarKnob.position = new Vector3(_hueBarKnob.position.x, position.y);
	}

    private void PlaceColorWindowKnob()
    {
        _colorWindowKnob.position = _colorWindow.NormalizedToWorldPosition(new Vector2(_s, _v));
    }

    private void Awake()
    {
        ColorChanged += CallOnColorChanged;
        _palleteMaterial = _colorWindowImage.material;
        SetColor(_color);

        _colorWindow.PointerPositionChanged += OnColorWindowPointerPositionChanged;
		_hueBar.PointerPositionChanged += OnHueBarPointerPositionChanged;
		_alphaSlider.ValueChanged += OnAlphaSliderValueChanged;
        _okButton?.onClick.AddListener(OnOkButtonPressed);
    }

	private void OnDestroy()
	{
        _colorWindow.PointerPositionChanged -= OnColorWindowPointerPositionChanged;
        _hueBar.PointerPositionChanged -= OnHueBarPointerPositionChanged;
        _alphaSlider.ValueChanged -= OnAlphaSliderValueChanged;
        _okButton?.onClick.RemoveListener(OnOkButtonPressed);
    }

    private void OnOkButtonPressed()
	{
        gameObject.SetActive(false);
	}

	private void OnAlphaSliderValueChanged(float value) => CalculateNewColor();

	private void OnHueBarPointerPositionChanged(Vector2 position)
	{
        _h = Mathf.Clamp01(_hueBar.PointerPositionNormalized.y); 
        CalculateNewColor();
    }

	private void OnColorWindowPointerPositionChanged(Vector2 position)
	{
        _s = Mathf.Clamp01(_colorWindow.PointerPositionNormalized.x);
        _v = Mathf.Clamp01(_colorWindow.PointerPositionNormalized.y);
        CalculateNewColor();
    }

    private void CalculateNewColor()
	{
        float h = _h, s = _s, v = _v;
        Color color = Color.HSVToRGB(_h, _s, _v);
        color.a = _alphaSlider.Value / 255f;
        Color = color;
        _h = h;
        _s = s;
        _v = v;
        PlaceHueBarKnob();
        PlaceColorWindowKnob();
        _palleteMaterial.SetFloat(HuePropertyId, _h);
    }

	public void OnDrag(PointerEventData eventData)
	{
        transform.position = _clickPosition + eventData.position - eventData.pressPosition;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
        _clickPosition = transform.position;
    }
}
