using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace ConstellationUI
{
    /// <summary>
    /// Class for creating UI Plot that displays some simple line plot
    /// Note: I have added only those parameters that I needed, which is why some features that
    /// *should* be there - are not (i.e. there is vertical margin but no horizontal etc.).
    /// sooo good luck adding these in yourself :)
    /// 
    /// Some terms and things to know:
    ///     - Plot limits: two ways of setting:
    ///         1) Automatic limits: the limits are set so that the whole plot fits on the canvas.
    ///             Automatic limits have margins that are calculated as axisRange * margin, e.g.,
    ///             YRange * _bottomRelativeMargin is used to calculate bottom margin
    ///         2) Manual limits: set all the limits manually, e.g., YMinLimit sets lower y limit.
    ///             if it is null, automatic limit is used instead
    /// </summary>
    /// 
    public class LinePlot : LabeledUIElement, IScrollHandler, IDragHandler, IPointerDownHandler
    {
        [Header("Objects")]
        [SerializeField] private UILineRenderer _plotCurve;
        [SerializeField] private UILineRenderer _horizontalAxis;
        [SerializeField] private UILineRenderer _verticalAxis;
        [SerializeField] private Behaviour _globalMask;
        [SerializeField] private RectTransform _canvasTransform;

        [Header("Parameters")]
        [SerializeField] private float _bottomRelativeMargin = 0.03f;
        [SerializeField] private float _minPixelsPerXTick = 5;
        [SerializeField] private float _minPixelsPerYTick = 10;
        [SerializeField] private int _majorXTickCapacity = 10;
        [SerializeField] private int _majorYTickCapacity = 5;
        [SerializeField] private float _tickLabelsFontSize = 18;
        [Range(0, 1)][SerializeField] private float _xStopTickLabels = 0.99f;

        private List<GameObject> _labels = new List<GameObject>();

        protected int PointCount => DataPoints.Length;
        /// Minimum X value in the current data
        protected float XMin;
        /// Maximum X value in the current data
        protected float XMax;
        /// Minimum Y value in the current data
        protected float YMin;
        /// Maximum Y value in the current data
        protected float YMax;
        /// XMax - XMin
        protected float XRange => XMax - XMin;
        /// YMax - YMin
        protected float YRange => YMax - YMin;
        protected Vector2 CanvasSize => _canvasTransform.rect.size;
        protected float CanvasWidth => _canvasTransform.rect.width;
        protected float CanvasHeight => _canvasTransform.rect.height;

        protected float DisplayedMinYLimit => MinYLimit ?? YMin - YRange * _bottomRelativeMargin;
        protected float DisplayedMaxYLimit => YMax; // replace
        protected float DisplayedYRange => DisplayedMaxYLimit - DisplayedMinYLimit;

        protected Vector2[] DataPoints;
        protected Vector2[] NormalizedPoints;

        protected float[] XTickSpacings;
        protected float[] YTickSpacings;

        private Vector2 _plotScale;
        private Vector2 _plotOffset;

        public float? MinYLimit { get; set; }

        /// <summary>
        /// Scale of the plot (is set by user scrolling/zooming the plot)
        /// Minimum value: (1, 1) (reason: why zoom out further? could be removed tho)
        /// </summary>
        protected Vector2 PlotScale
        {
            get => _plotScale;
            set { _plotScale = new Vector2(Mathf.Max(1, value.x), Mathf.Max(1, value.y)); }
        }

        /// <summary>
        /// Normalized offset of the plot (is set by user dragging/translating the plot)
        /// Offset is relative to the center of the displayed plot and has range [-0.5; 0.5]
        /// </summary>
        protected Vector2 PlotOffset
        {
            get => _plotOffset;
            set
            {
                Vector2 max = 0.5f * (Vector2.one - Vector2.one / PlotScale);
                _plotOffset = new Vector2(Mathf.Clamp(value.x, -max.x, max.x), Mathf.Clamp(value.y, -max.y, max.y));
            }
        }

        private void Awake()
        {
            _horizontalAxis.ChunckedMode = true;
            _verticalAxis.ChunckedMode = true;
        }

        public void DisplayPlot(float[] xTickSpacings, float[] yTickSpacings)
        {
            if (DataPoints == null)
                throw new InvalidOperationException($"{nameof(LinePlot)}: {nameof(DisplayPlot)}() called before calling {nameof(SetData)}()");
            PlotScale = Vector2.one; PlotOffset = Vector2.zero;

            NormalizedPoints = new Vector2[PointCount];
            for (int i = 0; i < PointCount; i++)
                NormalizedPoints[i] = (DataPoints[i] - new Vector2(XMin, DisplayedMinYLimit)) / new Vector2(XMax - XMin, YMax - DisplayedMinYLimit);

            _plotCurve.SetNormalizedPoints(NormalizedPoints);
            XTickSpacings = xTickSpacings;
            YTickSpacings = yTickSpacings;

            MakePlotTicksAndLabels();
        }

        public void SetData(IReadOnlyList<float> values)
        {
            DataPoints = new Vector2[values.Count];
            YMin = YMax = values[0];
            for (int i = 0; i < values.Count; i++)
            {
                DataPoints[i] = new Vector2(i, values[i]);
                YMin = Mathf.Min(YMin, values[i]);
                YMax = Mathf.Max(YMax, values[i]);
            }
            XMin = 0;
            XMax = values.Count - 1;
        }

        private static float SelectTickStep(float canvasSize, float dataSize, float minUnitsPerTick, float[] spacings)
        {
            float currentSpacing = spacings.Length > 0 ? spacings[0] : 1;

            // increase spacings
            for (int i = 0; ; i++)
            {
                if (i < spacings.Length)
                {
                    currentSpacing = spacings[i];
                }
                else
                {
                    currentSpacing *= 2;
                }

                if (GetUnitsPerTicks(currentSpacing) >= minUnitsPerTick) break;
            }

            // decrease spacings (beyond values in `spacings`)
            float prevSpacing = currentSpacing;
            while (GetUnitsPerTicks(currentSpacing) >= minUnitsPerTick)
            {
                prevSpacing = currentSpacing;
                currentSpacing /= 2;
            }

            return prevSpacing;

            float GetUnitsPerTicks(float spacing) { return canvasSize / (dataSize / spacing); }
        }

        private void MakePlotTicksAndLabels()
        {
            float xTickStep = SelectTickStep(_canvasTransform.rect.width, PointCount, _minPixelsPerXTick / PlotScale.x, XTickSpacings);
            float yTickStep = SelectTickStep(_canvasTransform.rect.height, DisplayedYRange, _minPixelsPerYTick / PlotScale.y, YTickSpacings);
            int majorXTickCapacity = _majorXTickCapacity;
            while (xTickStep < 1) // xTickStep should be at least 1 
            {
                xTickStep = Mathf.Min(1, xTickStep * 2);
                majorXTickCapacity = Mathf.Max(1, majorXTickCapacity / 2);
            }

            foreach (GameObject go in _labels) Destroy(go);
            _labels.Clear();
            if (PointCount < 2) return;

            float baseX = Mathf.Ceil(XMin);
            MakeAxisTicks(_horizontalAxis, baseX, XMin, XMax, xTickStep, majorXTickCapacity, PlotOffset.x, PlotScale.x, new Vector2(5, 19),
                (ticks, x, isMajor) =>
                {
                    ticks.Add(new Vector2(x, isMajor ? 0.25f : 0.6f));
                    ticks.Add(new Vector2(x, 1));
                }, x => new Vector2(x, 0), x => $"{Mathf.RoundToInt(x)}", _xStopTickLabels);

            float yBase = Mathf.Round(DisplayedMinYLimit / yTickStep) * yTickStep;
            MakeAxisTicks(_verticalAxis, yBase, DisplayedMinYLimit, DisplayedMaxYLimit, yTickStep, _majorYTickCapacity, PlotOffset.y, PlotScale.y,
                new Vector2(0, 22), (ticks, y, isMajor) =>
                {
                    ticks.Add(new Vector2(0, y));
                    ticks.Add(new Vector2(isMajor ? 0.6f : 0.25f, y));
                }, y => new Vector2(0, y), y => $"{y * 1000:0.00}", 1);

            // Without this the TMP labels stay masked :(
            _globalMask.enabled = false; _globalMask.enabled = true;

            // oh god forgive
            void MakeAxisTicks(UILineRenderer axis, float startValue, float minValue, float maxValue, float tickStep, int majorTickCapacity,
                float offset, float scale, Vector2 worldLabelOffset, Action<List<Vector2>, float, bool> tickDelegate, Func<float, Vector2> normalPosDelegate,
                Func<float, string> textDelegate, float labelMaxLimit)
            {
                List<Vector2> ticks = new List<Vector2>();
                RectTransform axisTransform = axis.gameObject.GetComponent<RectTransform>();
                int ticksTillMajor = 0;
                for (float x = startValue; x <= maxValue || Mathf.Approximately(x, maxValue); x += tickStep)
                {
                    bool isMajor = ticksTillMajor <= 0;
                    if (isMajor) ticksTillMajor = majorTickCapacity;
                    ticksTillMajor--;

                    float normalized = (x - minValue) / (maxValue - minValue);
                    float transformed = (normalized - 0.5f + offset) * scale + 0.5f;
                    if (transformed < -1e-6) continue; if (transformed > 1) break;

                    tickDelegate(ticks, transformed, isMajor);

                    if (isMajor && transformed <= labelMaxLimit)
                        AddTickLabel(axisTransform, normalPosDelegate(transformed), worldLabelOffset, textDelegate(x));
                }

                axis.SetNormalizedPoints(ticks.ToArray());
            }

            void AddTickLabel(RectTransform target, Vector2 normalPos, Vector2 worldOffset, string text)
            {
                GameObject majorLabel = new GameObject { name = "Tick label" };
                RectTransform rectTransform = majorLabel.AddComponent<RectTransform>();
                rectTransform.SetParent(target, false);
                rectTransform.sizeDelta = Vector2.zero;
                TextMeshProUGUI label = majorLabel.AddComponent<TextMeshProUGUI>();
                label.fontSize = _tickLabelsFontSize;
                label.text = text;
                label.horizontalAlignment = HorizontalAlignmentOptions.Left;
                label.maskable = false;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                rectTransform.localPosition = UIPositionHelper.NormalizedToLocalPosition(target, normalPos) + worldOffset;
                _labels.Add(majorLabel);
            }
        }

        private void UpdatePlot()
        {
            List<Vector2> newPoints = new List<Vector2>(PointCount);
            int i = 0;
            // skip all the points less than zero (except for the last one)
            for (; i < PointCount; i++)
            {
                Vector2 point = TransformPoint(NormalizedPoints[i]);
                if (point.x >= 0)
                {
                    i = Mathf.Max(i - 1, 0);
                    break;
                }
            }

            // draw all the points until they go beyond 1
            for (; i < PointCount; i++)
            {
                Vector2 point = TransformPoint(NormalizedPoints[i]);
                newPoints.Add(point);
                // break is *after* adding to list on purpose
                if (point.x > 1) break;
            }

            _plotCurve.SetNormalizedPoints(newPoints);
            MakePlotTicksAndLabels();

            Vector2 TransformPoint(Vector2 point)
            {
                return (point - Vector2.one / 2 + PlotOffset) * PlotScale + Vector2.one / 2;
            }
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (DataPoints == null) return;
            const float ScaleFactor = 1.25f;

            bool shrinking = eventData.scrollDelta.y < 0;
            float scaleFactor = shrinking ? 1 / ScaleFactor : ScaleFactor;
            Vector2 localPointerPos = UIPositionHelper.WorldToNormalizedPosition(_canvasTransform, eventData.position);
            localPointerPos -= Vector2.one / 2; // [-0.5; 0.5]
            Vector2 offset = -localPointerPos / PlotScale * Mathf.Sign(eventData.scrollDelta.y) * (ScaleFactor - 1);
            if (!shrinking) offset /= scaleFactor;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (Mathf.Approximately(PlotScale.y, 1) && shrinking) return;
                PlotScale *= new Vector2(1, scaleFactor);
                PlotOffset += new Vector2(0, offset.y);
            }
            else
            {
                if (Mathf.Approximately(PlotScale.x, 1) && shrinking) return;
                PlotScale *= new Vector2(scaleFactor, 1);
                PlotOffset += new Vector2(offset.x, 0);
            }

            UpdatePlot();
        }

        public void OnDrag(PointerEventData eventData)
        {
            PlotOffset += eventData.delta / PlotScale / CanvasSize;

            UpdatePlot();
        }

        // without this panning the plot sometimes also drags the parent window
        public void OnPointerDown(PointerEventData eventData) { }
    }
}