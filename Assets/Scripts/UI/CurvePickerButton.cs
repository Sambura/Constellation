using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace ConstellationUI
{
    public class CurvePickerButton : LabeledUIElement, IPointerClickHandler
    {
        [Header("Objects")]
        [SerializeField] private CurvePickerViewport _viewport;
        [SerializeField] private CurvePicker _curvePicker;

        [Header("Parameters")]
        [SerializeField] private Vector2 _curvePickerOffset;
        [SerializeField] private Vector2 _curvePickerPivot;
        [SerializeField] private string _curvePickerTitle = "Select curve";
        [SerializeField] private bool _findCurvePicker = true;

        public AnimationCurve Curve
        {
            get => _viewport.Curve;
            set { if (value != _viewport.Curve) { _viewport.Curve = value; CurveChanged?.Invoke(value); } }
        }
        public event Action ButtonClick;
        public event Action<AnimationCurve> CurveChanged;

        public CurvePicker CurvePicker
        {
            get => _curvePicker != null ? _curvePicker : (_findCurvePicker ? _curvePicker = FindFirstObjectByType<CurvePicker>(FindObjectsInactive.Include) : null);
            /* Set can be added as needed, but proper support for dynamic property set may be a pain to implement */
        }

        public string DialogTitle
        {
            get => _curvePickerTitle;
            set
            {
                if (_curvePickerTitle == value) return;
                _curvePickerTitle = value;
                /* TODO add dynamic title change for color picker */
            }
        }

        private RectTransform _transform;

        public void OnPointerClick(PointerEventData eventData)
        {
            ButtonClick?.Invoke();
            OpenCurvePicker();
        }

        private void OnCurvePickerCurveChange(AnimationCurve curve) => Curve = curve;

        private void OpenCurvePicker()
        {
            Vector2 zeroPosition = (Vector2)_transform.position + _transform.rect.position;
            Vector2 pivotPosition = zeroPosition + _transform.rect.size * _curvePickerPivot;
            CurvePicker.ShowDialog(_curvePickerTitle);
            CurvePicker.Position = pivotPosition + _curvePickerOffset;
            CurvePicker.Curve = Curve;
            CurvePicker.OnCurveChanged = OnCurvePickerCurveChange;
        }

        private void Awake() { _transform = GetComponent<RectTransform>(); _viewport.Curve = Curve ?? new AnimationCurve(); }
    }
}