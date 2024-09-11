using UnityEngine;
using UnityEngine.UI;
using System;

namespace ConstellationUI
{
    public class ColorPicker : MonoDialog
    {
        [SerializeField] private UIPositionHelper _colorWindow;
        [SerializeField] private UIPositionHelper _hueBar;
        [SerializeField] private Slider _alphaSlider;
        [SerializeField] private Image _colorWindowImage;
        [SerializeField] private RectTransform _hueBarKnob;
        [SerializeField] private RectTransform _colorWindowKnob;
        
        protected float Alpha => _alphaSlider.Value / 255f;

        public Color Color
        {
            get => new Color(_color.r, _color.g, _color.b, UseAlpha ? Alpha : 1f);
            set
            {
                if (_color == value) return;
                SetColor(value);
                ColorChanged?.Invoke(Color);
            }
        }
        public Action<Color> OnColorChanged { get; set; }
        public event Action<Color> ColorChanged;

        public bool UseAlpha
        {
            get => _useAlpha;
            set
            {
                _useAlpha = value;
                _alphaSlider.gameObject.SetActive(value);
            }
        }

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
        private bool _useAlpha;

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

        protected override void Awake()
        {
            base.Awake();
            ColorChanged += CallOnColorChanged;
            _colorWindowImage.material = new Material(_colorWindowImage.material);
            _palleteMaterial = _colorWindowImage.material;
            SetColor(_color);

            _colorWindow.PointerPositionChanged += OnColorWindowPointerPositionChanged;
            _hueBar.PointerPositionChanged += OnHueBarPointerPositionChanged;
            _alphaSlider.ValueChanged += OnAlphaSliderValueChanged;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _colorWindow.PointerPositionChanged -= OnColorWindowPointerPositionChanged;
            _hueBar.PointerPositionChanged -= OnHueBarPointerPositionChanged;
            _alphaSlider.ValueChanged -= OnAlphaSliderValueChanged;
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
    }
}