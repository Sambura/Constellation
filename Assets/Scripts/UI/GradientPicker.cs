using UnityEngine;
using System;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Core;

namespace ConstellationUI
{
    public class GradientPicker : MonoDialog
    {
        [SerializeField] private UIEvents _hitbox;
        [SerializeField] private GradientImage _viewport;
        [SerializeField] private GameObject _gradientStopPrefab;
        [SerializeField] private ColorPickerButton _stopColorButton;

        private GradientStop _selectedStop;
        private Gradient _gradient;
        private List<GradientStop> _stops = new List<GradientStop>();

        public GradientStop SelectedStop
        {
            get => _selectedStop;
            set
            {
                if (_selectedStop == value) return;
                if (_selectedStop) _selectedStop.Selected = false;
                _selectedStop = value;
                if (_selectedStop)
                {
                    _selectedStop.Selected = true;
                    _stopColorButton.Color = _selectedStop.Color;
                    _stopColorButton.Interactable = true;
                }
                else
                {
                    _stopColorButton.Interactable = false;
                    _stopColorButton.CloseColorPicker();
                }
            }
        }

        public Gradient Gradient
        {
            get => _gradient;
            set { if (_gradient != value) SetGradient(value); }
        }
        public Action<Gradient> OnGradientChanged { get; set; }
        public event Action<Gradient> GradientChanged;

        public void SetGradient(Gradient gradient)
        {
            if (gradient == null)
                gradient = new Gradient() { alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1, 0) } };

            _gradient = gradient;
            SelectedStop = null; // Reset selection
            GradientColorKey[] keys = gradient.colorKeys;

            while (_stops.Count > keys.Length)
            {
                Destroy(_stops[_stops.Count - 1].gameObject);
                _stops.RemoveAt(_stops.Count - 1);
            }

            while (_stops.Count < keys.Length)
                AddStop(Vector2.zero);

            for (int i = 0; i < keys.Length; i++)
            {
                _stops[i].SetNormalizedPosition(new Vector2(keys[i].time, 0));
                _stops[i].Color = keys[i].color;
            }

            // Raises GradientChanged event
            UpdateStops();

            SelectedStop = _stops[0]; // Select first stop
        }

        private void CallOnGradientChanged(Gradient gradient) => OnGradientChanged?.Invoke(gradient);

        protected override void Awake()
        {
            base.Awake();

            GradientChanged += CallOnGradientChanged;
            _hitbox.PointerDown += OnHitboxPointerDown;
            _hitbox.PointerUp += OnHitboxPointerUp;
            _stopColorButton.ColorChanged += OnStopColorChanged;
            _stopColorButton.Interactable = false;
            SetGradient(_gradient);
        }

        private void OnStopColorChanged(Color color)
        {
            if (SelectedStop == null) return;

            color.a = 1;
            SelectedStop.Color = color;
            UpdateStops();
        }

        private void UpdateStops()
        {
            Algorithm.BubbleSort(_stops, (x, y) =>
            {
                bool byX = x.Position.x != y.Position.x;
                return byX ? x.Position.x < y.Position.x : x.GetHashCode() < y.GetHashCode();
            });

            Gradient gradient = new Gradient() { alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1, 0) } };
            GradientColorKey[] keys = new GradientColorKey[_stops.Count];
            for (int i = 0; i < _stops.Count; i++)
            {
                keys[i] = new GradientColorKey(_stops[i].Color, _stops[i].Time);
            }
            gradient.colorKeys = keys;

            _gradient = gradient;
            _viewport.Gradient = _gradient;
            GradientChanged?.Invoke(_gradient);
        }

        private GradientStop AddStop(Vector2 position)
        {
            GradientStop newStop = Instantiate(_gradientStopPrefab, _viewport.transform).GetComponent<GradientStop>();
            _stops.Add(newStop);
            newStop.Position = position;
            newStop.Color = Gradient.Evaluate(newStop.Time);
            newStop.SelectedChanged += OnStopSelectedChanged;
            newStop.TimeChanged += OnStopTimeChanged;
            return newStop;
        }

        private void OnStopTimeChanged(float time)
        {
            UpdateStops();
        }

        private void OnStopSelectedChanged(MonoSelectable stop, bool value)
        {
            if (value == false) return;
            SelectedStop = (GradientStop)stop;
        }

        public void RemoveSelectedStop()
        {
            if (SelectedStop == null) return;

            _stops.Remove(SelectedStop);
            Destroy(SelectedStop.gameObject);
            SelectedStop = null;

            UpdateStops();
        }

        private void OnHitboxPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                SelectedStop = null;
                return;
            }
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (Gradient.colorKeys.Length >= 8)
            {
                SelectedStop = null;
                Manager.ShowMessageBox("Warning", "Sorry, but currently you cannot" +
                    " add more than 8 gradient stops.", StandardMessageBoxIcons.Warning);
                return;
            }

            GradientStop newStop = AddStop(eventData.position);
            UpdateStops();
            newStop.OnPointerDown(eventData);
        }

        private void OnHitboxPointerUp(PointerEventData eventData)
        {
            SelectedStop?.OnPointerUp(eventData);
        }
    }
}