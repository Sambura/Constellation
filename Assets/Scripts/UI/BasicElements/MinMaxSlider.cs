using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class MinMaxSlider : Selectable, IDragHandler, IInitializePotentialDragHandler, ICanvasElement
    {
        public enum Direction
        {
            LeftToRight,
            RightToLeft,
            BottomToTop,
            TopToBottom,
        }

        [SerializeField]
        private RectTransform m_FillRect;
        public RectTransform fillRect { get { return m_FillRect; } set { UpdateCachedReferences(); UpdateVisuals(); } }

        [SerializeField]
        private RectTransform _minHandleRect;
        public RectTransform MinHandleRect { get { return _minHandleRect; } set { UpdateCachedReferences(); UpdateVisuals(); } }

        [SerializeField]
        private RectTransform _maxHandleRect;
        public RectTransform MaxHandleRect { get { return _maxHandleRect; } set { UpdateCachedReferences(); UpdateVisuals(); } }

        [Space]

        [SerializeField]
        private Direction m_Direction = Direction.LeftToRight;
        public Direction direction { get { return m_Direction; } set { UpdateVisuals(); } }

        [SerializeField]
        private float m_MinValue = 0;
        public float minValue { get { return m_MinValue; } set { m_MinValue = value; SetMinSliderValue(_minSliderValue); UpdateVisuals(); } }

        [SerializeField]
        private float m_MaxValue = 1;
        public float maxValue { get { return m_MaxValue; } set { m_MaxValue = value; SetMaxSliderValue(_maxSliderValue); UpdateVisuals(); } }

        [SerializeField]
        private bool m_WholeNumbers = false;
        public bool wholeNumbers { get { return m_WholeNumbers; } set { SetMinSliderValue(_minSliderValue); UpdateVisuals(); } }

        [SerializeField]
        protected float _minSliderValue;
        public virtual float MinSliderValue
        {
            get
            {
                return wholeNumbers ? Mathf.Round(_minSliderValue) : _minSliderValue;
            }
            set
            {
                SetMinSliderValue(value);
            }
        }

        [SerializeField]
        protected float _maxSliderValue;
        public virtual float MaxSliderValue
        {
            get
            {
                return wholeNumbers ? Mathf.Round(_maxSliderValue) : _maxSliderValue;
            }
            set
            {
                SetMaxSliderValue(value);
            }
        }

        public virtual void SetMinSliderValueWithoutNotify(float input)
        {
            SetMinSliderValue(input, false);
        }

        public virtual void SetMaxSliderValueWithoutNotify(float input)
        {
            SetMaxSliderValue(input, false);
        }

        public float MinSliderNormalizedValue
        {
            get
            {
                if (Mathf.Approximately(minValue, maxValue))
                    return 0;
                return Mathf.InverseLerp(minValue, maxValue, MinSliderValue);
            }
            set
            {
                MinSliderValue = Mathf.Lerp(minValue, maxValue, value);
            }
        }

        public float MaxSliderNormalizedValue
        {
            get
            {
                if (Mathf.Approximately(minValue, maxValue))
                    return 0;
                return Mathf.InverseLerp(minValue, maxValue, MaxSliderValue);
            }
            set
            {
                MaxSliderValue = Mathf.Lerp(minValue, maxValue, value);
            }
        }

        public event Action<float> MinSliderValueChanged;
        public event Action<float> MaxSliderValueChanged;

        // Private fields

        private Image m_FillImage;
        private Transform m_FillTransform;
        private RectTransform m_FillContainerRect;
        private Transform _minHandleTransform;
        private Transform _maxHandleTransform;
        private RectTransform m_HandleContainerRect;

        // The offset from handle position to mouse down position
        private Vector2 m_Offset = Vector2.zero;
        private RectTransform _capturedHandle;

        // field is never assigned warning
#pragma warning disable 649
        private DrivenRectTransformTracker m_Tracker;
#pragma warning restore 649

        // This "delayed" mechanism is required for case 1037681.
        private bool m_DelayedUpdateVisuals = false;

        // Size of each step.
        float stepSize { get { return wholeNumbers ? 1 : (maxValue - minValue) * 0.1f; } }

        protected MinMaxSlider() { }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (wholeNumbers)
            {
                m_MinValue = Mathf.Round(m_MinValue);
                m_MaxValue = Mathf.Round(m_MaxValue);
            }

            //Onvalidate is called before OnEnabled. We need to make sure not to touch any other objects before OnEnable is run.
            if (IsActive())
            {
                UpdateCachedReferences();
                // Update rects in next update since other things might affect them even if value didn't change.
                m_DelayedUpdateVisuals = true;
            }

            if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this) && !Application.isPlaying)
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }

#endif // if UNITY_EDITOR

        public virtual void Rebuild(CanvasUpdate executing)
        {
#if UNITY_EDITOR
            if (executing == CanvasUpdate.Prelayout)
            {
                MinSliderValueChanged?.Invoke(MinSliderValue);
                MaxSliderValueChanged?.Invoke(MaxSliderValue);
            }
#endif
        }

        /// <summary>
        /// See ICanvasElement.LayoutComplete
        /// </summary>
        public virtual void LayoutComplete() { }

        /// <summary>
        /// See ICanvasElement.GraphicUpdateComplete
        /// </summary>
        public virtual void GraphicUpdateComplete() { }

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateCachedReferences();
            SetMinSliderValue(_minSliderValue, false);
            // Update rects since they need to be initialized correctly.
            UpdateVisuals();
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();
            base.OnDisable();
        }

        /// <summary>
        /// Update the rect based on the delayed update visuals.
        /// Got around issue of calling sendMessage from onValidate.
        /// </summary>
        protected virtual void Update()
        {
            if (m_DelayedUpdateVisuals)
            {
                m_DelayedUpdateVisuals = false;
                SetMinSliderValue(_minSliderValue, false);
                UpdateVisuals();
            }
        }

        protected override void OnDidApplyAnimationProperties()
        {
            // Has value changed? Various elements of the slider have the old normalisedValue assigned, we can use this to perform a comparison.
            // We also need to ensure the value stays within min/max.
            _minSliderValue = ClampValue(_minSliderValue);
            float oldMinNormalizedValue = MinSliderNormalizedValue;
            if (m_FillContainerRect != null)
            {
                if (m_FillImage != null && m_FillImage.type == Image.Type.Filled)
                    oldMinNormalizedValue = m_FillImage.fillAmount;
                else
                    oldMinNormalizedValue = (reverseValue ? 1 - m_FillRect.anchorMin[(int)axis] : m_FillRect.anchorMax[(int)axis]);
            }
            else if (m_HandleContainerRect != null)
                oldMinNormalizedValue = (reverseValue ? 1 - _minHandleRect.anchorMin[(int)axis] : _minHandleRect.anchorMin[(int)axis]);

            UpdateVisuals();

            if (oldMinNormalizedValue != MinSliderNormalizedValue)
            {
                UISystemProfilerApi.AddMarker("MinMaxSlider.MinSliderValue", this);
                MinSliderValueChanged?.Invoke(_minSliderValue);
            }
        }

        void UpdateCachedReferences()
        {
            if (m_FillRect && m_FillRect != (RectTransform)transform)
            {
                m_FillTransform = m_FillRect.transform;
                m_FillImage = m_FillRect.GetComponent<Image>();
                if (m_FillTransform.parent != null)
                    m_FillContainerRect = m_FillTransform.parent.GetComponent<RectTransform>();
            }
            else
            {
                m_FillRect = null;
                m_FillContainerRect = null;
                m_FillImage = null;
            }

            if (_minHandleRect && _maxHandleRect && _minHandleRect != (RectTransform)transform)
            {
                _minHandleTransform = _minHandleRect.transform;
                _maxHandleTransform = _maxHandleRect.transform;
                if (_minHandleTransform.parent != null)
                    m_HandleContainerRect = _minHandleTransform.parent.GetComponent<RectTransform>();
            }
            else
            {
                _minHandleRect = null;
                _maxHandleRect = null;
                m_HandleContainerRect = null;
            }
        }

        float ClampValue(float input)
        {
            float newValue = Mathf.Clamp(input, minValue, maxValue);
            if (wholeNumbers)
                newValue = Mathf.Round(newValue);
            return newValue;
        }

        protected virtual void SetMinSliderValue(float input, bool sendCallback = true)
        {
            // Clamp the input
            float newValue = ClampValue(input);

            // If the stepped value doesn't match the last one, it's time to update
            if (_minSliderValue == newValue)
                return;

            _minSliderValue = newValue;
            MaxSliderValue = Mathf.Max(_minSliderValue, MaxSliderValue);
            UpdateVisuals();
            if (sendCallback)
            {
                UISystemProfilerApi.AddMarker("MinMaxSlider.MinSliderValue", this);
                MinSliderValueChanged?.Invoke(newValue);
            }
        }

        protected virtual void SetMaxSliderValue(float input, bool sendCallback = true)
        {
            // Clamp the input
            float newValue = ClampValue(input);

            // If the stepped value doesn't match the last one, it's time to update
            if (_maxSliderValue == newValue)
                return;

            _maxSliderValue = newValue;
            MinSliderValue = Mathf.Min(MinSliderValue, _maxSliderValue);
            UpdateVisuals();
            if (sendCallback)
            {
                UISystemProfilerApi.AddMarker("MinMaxSlider.MaxSliderValue", this);
                MaxSliderValueChanged?.Invoke(newValue);
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            //This can be invoked before OnEnabled is called. So we shouldn't be accessing other objects, before OnEnable is called.
            if (!IsActive())
                return;

            UpdateVisuals();
        }

        enum Axis
        {
            Horizontal = 0,
            Vertical = 1
        }

        Axis axis { get { return (m_Direction == Direction.LeftToRight || m_Direction == Direction.RightToLeft) ? Axis.Horizontal : Axis.Vertical; } }
        bool reverseValue { get { return m_Direction == Direction.RightToLeft || m_Direction == Direction.TopToBottom; } }

        // Force-update the slider. Useful if you've changed the properties and want it to update visually.
        private void UpdateVisuals()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateCachedReferences();
#endif

            m_Tracker.Clear();

            if (m_FillContainerRect != null)
            {
                m_Tracker.Add(this, m_FillRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;

                if (m_FillImage != null && m_FillImage.type == Image.Type.Filled)
                {
                    m_FillImage.fillAmount = MinSliderNormalizedValue;
                }
                else
                {
                    if (reverseValue)
                    {
                        anchorMin[(int)axis] = 1 - MaxSliderNormalizedValue;
                        anchorMax[(int)axis] = 1 - MinSliderNormalizedValue;
                    }
                    else
                    {
                        anchorMin[(int)axis] = MinSliderNormalizedValue;
                        anchorMax[(int)axis] = MaxSliderNormalizedValue;
                    }
                }

                m_FillRect.anchorMin = anchorMin;
                m_FillRect.anchorMax = anchorMax;
            }

            if (m_HandleContainerRect != null)
            {
                m_Tracker.Add(this, _minHandleRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;
                // Min value
                anchorMin[(int)axis] = anchorMax[(int)axis] = (reverseValue ? (1 - MinSliderNormalizedValue) : MinSliderNormalizedValue);
                _minHandleRect.anchorMin = anchorMin;
                _minHandleRect.anchorMax = anchorMax;
                // Max value
                anchorMin[(int)axis] = anchorMax[(int)axis] = (reverseValue ? (1 - MaxSliderNormalizedValue) : MaxSliderNormalizedValue);
                _maxHandleRect.anchorMin = anchorMin;
                _maxHandleRect.anchorMax = anchorMax;
            }
        }

        void UpdateDragMin(PointerEventData eventData, Camera cam)
        {
            RectTransform clickRect = m_HandleContainerRect ?? m_FillContainerRect;
            if (clickRect != null && clickRect.rect.size[(int)axis] > 0)
            {
                Vector2 position = eventData.position;

                Vector2 localCursor;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(clickRect, position, cam, out localCursor))
                    return;
                localCursor -= clickRect.rect.position;

                float val = Mathf.Clamp01((localCursor - m_Offset)[(int)axis] / clickRect.rect.size[(int)axis]);
                MinSliderNormalizedValue = (reverseValue ? 1f - val : val);
            }
        }

        void UpdateDragMax(PointerEventData eventData, Camera cam)
        {
            RectTransform clickRect = m_HandleContainerRect ?? m_FillContainerRect;
            if (clickRect != null && clickRect.rect.size[(int)axis] > 0)
            {
                Vector2 position = eventData.position;

                Vector2 localCursor;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(clickRect, position, cam, out localCursor))
                    return;
                localCursor -= clickRect.rect.position;

                float val = Mathf.Clamp01((localCursor - m_Offset)[(int)axis] / clickRect.rect.size[(int)axis]);
                MaxSliderNormalizedValue = (reverseValue ? 1f - val : val);
            }
        }

        private bool MayDrag(PointerEventData eventData)
        {
            return IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            base.OnPointerDown(eventData);

            m_Offset = Vector2.zero;
            if (m_HandleContainerRect != null && RectTransformUtility.RectangleContainsScreenPoint(_maxHandleRect, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera))
            {
                Vector2 localMousePos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_maxHandleRect, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out localMousePos))
                {
                    m_Offset = localMousePos;
                    _capturedHandle = _maxHandleRect;
                }
            } else
            if (m_HandleContainerRect != null && RectTransformUtility.RectangleContainsScreenPoint(_minHandleRect, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera))
            {
                Vector2 localMousePos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_minHandleRect, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out localMousePos))
                {
                    m_Offset = localMousePos;
                    _capturedHandle = _minHandleRect;
                }
            } 
            else
            {
                // Outside the slider handle - jump to this point instead
                Vector2 localMousePos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_HandleContainerRect, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out localMousePos)) {
                    if (Vector2.Distance(localMousePos, _minHandleRect.localPosition) <= Vector2.Distance(localMousePos, _maxHandleRect.localPosition))
                    {
                        _capturedHandle = _minHandleRect;
                        UpdateDragMin(eventData, eventData.pressEventCamera);
                    }
                    else
                    {
                        _capturedHandle = _maxHandleRect;
                        UpdateDragMax(eventData, eventData.pressEventCamera);
                    }
                }
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            _capturedHandle = null;
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;
            if (_capturedHandle == _minHandleRect) UpdateDragMin(eventData, eventData.pressEventCamera);
            if (_capturedHandle == _maxHandleRect) UpdateDragMax(eventData, eventData.pressEventCamera);
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
            {
                base.OnMove(eventData);
                return;
            }

            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    if (axis == Axis.Horizontal && FindSelectableOnLeft() == null)
                        SetMinSliderValue(reverseValue ? MinSliderValue + stepSize : MinSliderValue - stepSize);
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Right:
                    if (axis == Axis.Horizontal && FindSelectableOnRight() == null)
                        SetMinSliderValue(reverseValue ? MinSliderValue - stepSize : MinSliderValue + stepSize);
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Up:
                    if (axis == Axis.Vertical && FindSelectableOnUp() == null)
                        SetMinSliderValue(reverseValue ? MinSliderValue - stepSize : MinSliderValue + stepSize);
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Down:
                    if (axis == Axis.Vertical && FindSelectableOnDown() == null)
                        SetMinSliderValue(reverseValue ? MinSliderValue + stepSize : MinSliderValue - stepSize);
                    else
                        base.OnMove(eventData);
                    break;
            }
        }

        public override Selectable FindSelectableOnLeft()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Horizontal)
                return null;
            return base.FindSelectableOnLeft();
        }

        public override Selectable FindSelectableOnRight()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Horizontal)
                return null;
            return base.FindSelectableOnRight();
        }

        public override Selectable FindSelectableOnUp()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Vertical)
                return null;
            return base.FindSelectableOnUp();
        }

        public override Selectable FindSelectableOnDown()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Vertical)
                return null;
            return base.FindSelectableOnDown();
        }

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        public void SetDirection(Direction direction, bool includeRectLayouts)
        {
            Axis oldAxis = axis;
            bool oldReverse = reverseValue;
            this.direction = direction;

            if (!includeRectLayouts)
                return;

            if (axis != oldAxis)
                RectTransformUtility.FlipLayoutAxes(transform as RectTransform, true, true);

            if (reverseValue != oldReverse)
                RectTransformUtility.FlipLayoutOnAxis(transform as RectTransform, (int)axis, true, true);
        }
    }
}
