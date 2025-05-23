using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace ConstellationUI
{
    public class ColorPickerButton : LabeledUIElement, IPointerClickHandler
    {
        [Header("Objects")]
        [SerializeField] private Image _colorImage;
        [SerializeField] private ColorPicker _colorPicker;

        [Header("Parameters")]
        [SerializeField] private Vector2 _colorPickerOffset;
        [SerializeField] private Vector2 _colorPickerPivot;
        [SerializeField] private bool _useAlpha;
        [SerializeField] private string _colorPickerTitle = "Select color";
        [SerializeField] private bool _findColorPicker = true;

        public Color Color
        {
            get => _colorImage.color;
            set { if (_colorImage.color != value) { _colorImage.color = value; ColorChanged?.Invoke(value); } }
        }
        public event Action ButtonClick;
        public event Action<Color> ColorChanged;

        public bool Interactable { get; set; } = true;

        private RectTransform _transform;

        public ColorPicker ColorPicker
        {
            get => _colorPicker != null ? _colorPicker : (_findColorPicker ? _colorPicker = FindFirstObjectByType<ColorPicker>(FindObjectsInactive.Include) : null);
            /* Set can be added as needed, but proper support for dynamic property set may be a pain to implement */
        }

        public bool UseAlpha
        {
            get => _useAlpha;
            set
            {
                if (_useAlpha == value) return;
                _useAlpha = value;
                if (ColorPicker.isActiveAndEnabled) ColorPicker.UseAlpha = _useAlpha;
            }
        }

        public string DialogTitle
        {
            get => _colorPickerTitle;
            set
            {
                if (_colorPickerTitle == value) return;
                _colorPickerTitle = value;
                /* TODO add dynamic title change for color picker */
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Interactable == false) return;

            ButtonClick?.Invoke();
            OpenColorPicker();
        }

        public void CloseColorPicker()
        {
            if (ColorPicker.OnColorChanged != OnColorPickerColorChange) return;

            ColorPicker.CloseDialog(false);
        }

        private void OnColorPickerColorChange(Color color) => Color = color;

        private void OpenColorPicker()
        {
            Vector2 zeroPosition = (Vector2)_transform.position + _transform.rect.position;
            Vector2 pivotPosition = zeroPosition + _transform.rect.size * _colorPickerPivot;
            ColorPicker.ShowDialog(_colorPickerTitle);
            ColorPicker.Position = _transform.position + (Vector3)_colorPickerOffset;
            ColorPicker.OnColorChanged = OnColorPickerColorChange;
            ColorPicker.UseAlpha = _useAlpha;
            ColorPicker.Color = Color;
        }

        private void Awake()
        {
            _transform = GetComponent<RectTransform>();
        }
    }
}