using System.Collections;
using UnityEngine;
using Core;
using SimpleGraphics;

public class MainVisualizator : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private BatchRenderer _drawer;
    [SerializeField] private Viewport _viewport;
    [SerializeField] private ParticleController _particleController;

    [Header("Simulation parameters")]
    [SerializeField] private float _linesWidth = 0.1f;
    [SerializeField] private bool _meshLines = false;
    [SerializeField] private float _connectionDistance = 60f;
    [SerializeField] private float _strongDistance = 10f;
    [SerializeField] private AnimationCurve _alphaCurve;
    [SerializeField] private Gradient _lineColor;
    [SerializeField] private bool _showLines = true;
    [SerializeField] private bool _showTriangles = true;
    [SerializeField] private bool _alternateLineColor = true;
    [SerializeField] private float _colorMinHue = 0;
    [SerializeField] private float _colorMaxHue = 1;
    [SerializeField] private float _colorMinSaturation = 0;
    [SerializeField] private float _colorMaxSaturation = 1;
    [SerializeField] private float _colorMinValue = 0.3f;
    [SerializeField] private float _colorMaxValue = 1;
    [SerializeField] private float _colorMinFadeDuration = 2;
    [SerializeField] private float _colorMaxFadeDuration = 4;
    [SerializeField] private Color _clearColor = Color.black;
    [SerializeField][Range(0, 1)] private float _triangleFillOpacity;

    [Header("Rendering")]
    [SerializeField] private int _renderQueueIndex = 0;

    [ConfigProperty]
    public bool ShowLines
    {
        get => _showLines;
        set { if (_showLines != value) { _showLines = value; ShowLinesChanged?.Invoke(value); }; }
    }
    [ConfigProperty]
    public bool MeshLines
    {
        get => _meshLines;
        set { if (_meshLines != value) { _meshLines = value; MeshLinesChanged?.Invoke(value); }; }
    }
    [ConfigProperty]
    public Gradient LineColor
	{
        get => _lineColor;
        set { if (_lineColor != value) { SetLineColor(value); LineColorChanged?.Invoke(value); } }
    }
    [ConfigProperty]
    public float LineWidth
	{
        get => _linesWidth;
        set { if (_linesWidth != value) { _linesWidth = value; LineWidthChanged?.Invoke(value); }; }
    }
    [ConfigProperty]
    public float ConnectionDistance
    {
        get => _connectionDistance;
        set { if (_connectionDistance != value) { SetConnectionDistance(value); ConnectionDistanceChanged?.Invoke(value); }; }
    }
    [ConfigProperty]
    public float StrongDistance
    {
        get => _strongDistance;
        set { if (_strongDistance != value) { SetStrongDistance(value); StrongDistanceChanged?.Invoke(value); }; }
    }
    [ConfigProperty]
    public bool ShowTriangles
	{
        get => _showTriangles;
        set { if (_showTriangles != value) { _showTriangles = value; ShowTrianglesChanged?.Invoke(value); }; }
    }
    [ConfigProperty]
	public float TriangleFillOpacity
	{
        get => _triangleFillOpacity;
        set { if (_triangleFillOpacity != value) { SetTriangleFillOpacity(value); TriangleFillOpacityChanged?.Invoke(value); }; }
    }
    [ConfigProperty]
	public Color ClearColor
    {
        get => _clearColor;
        set { if (_clearColor != value) { SetClearColor(value); ClearColorChanged?.Invoke(value); }; }
    }
    [ConfigProperty]
	public AnimationCurve AlphaCurve
	{
        get => _alphaCurve;
        set { if (_alphaCurve != value) { SetAlphaCurve(value); AlphaCurveChanged?.Invoke(value); } }
	}
    [ConfigProperty]
	public bool AlternateLineColor
	{
        get => _alternateLineColor;
        set { if (_alternateLineColor != value) { SetAlternateLineColor(value); AlternateLineColorChanged?.Invoke(value); } }
	}
    [ConfigProperty]
    public float MinColorFadeDuration
	{
        get => _colorMinFadeDuration;
        set { if (_colorMinFadeDuration != value) { _colorMinFadeDuration = value; RestartColorChanger(); MinColorFadeDurationChanged?.Invoke(MinColorFadeDuration); } }
	}
    [ConfigProperty]
	public float MaxColorFadeDuration
    {
        get => _colorMaxFadeDuration;
        set { if (_colorMaxFadeDuration != value) { _colorMaxFadeDuration = value; RestartColorChanger(); MaxColorFadeDurationChanged?.Invoke(MaxColorFadeDuration); } }
    }

    public event System.Action<bool> ShowLinesChanged;
    public event System.Action<bool> MeshLinesChanged;
    public event System.Action<Gradient> LineColorChanged;
    public event System.Action<float> LineWidthChanged;
    public event System.Action<float> ConnectionDistanceChanged;
    public event System.Action<float> StrongDistanceChanged;
    public event System.Action<bool> ShowTrianglesChanged;
    public event System.Action<float> TriangleFillOpacityChanged;
    public event System.Action<Color> ClearColorChanged;
    public event System.Action<AnimationCurve> AlphaCurveChanged;
    public event System.Action<bool> AlternateLineColorChanged;
    public event System.Action<float> MinColorFadeDurationChanged;
    public event System.Action<float> MaxColorFadeDurationChanged;

    private void RestartColorChanger()
	{
        if (_colorChanger != null)
		{
            StopCoroutine(_colorChanger);
            _colorChanger = StartCoroutine(ColorChanger());
		}
	}

    private void SetAlphaCurve(AnimationCurve value)
	{
        _alphaCurve = value;

        Keyframe[] alphaKeys = value.keys; // Is is a copy
        InvertAnimationCurveKeys(alphaKeys);

        _alphaCurveInternal.keys = alphaKeys;
    }

    private void SetLineColor(Gradient value)
    {
        _lineColor = value;
        if (_alternateLineColor) return;

        _currentLineGradientColorKeys = value.colorKeys;
        InvertGradientKeys(_currentLineGradientColorKeys);
        _currentLineColorGradient.colorKeys = _currentLineGradientColorKeys;
    }

    private void SetAlternateLineColor(bool value)
    {
        _alternateLineColor = value;

        if (value == false)
        {
            if (_colorChanger != null)
            {
                StopCoroutine(_colorChanger);
                _colorChanger = null;
            }
            SetLineColor(_lineColor);
            return;
        }

        _currentLineGradientColorKeys = new GradientColorKey[] { new GradientColorKey(_lineColor.Evaluate(1), 1) };
        _colorChanger = StartCoroutine(ColorChanger());
    }

    private void SetConnectionDistance(float value)
	{
        _connectionDistance = value;
        _connectionDistanceSquared = _connectionDistance * _connectionDistance;
        _lineIntensityDenominator = _connectionDistance - _strongDistance;
        _triangleColorCoefficient = _triangleFillOpacity / _lineIntensityDenominator;

        _particleController.SetConnectionDistance(value);
    }
    private void SetStrongDistance(float value)
    {
        _strongDistance = value;
        _lineIntensityDenominator = _connectionDistance - _strongDistance;
        _triangleColorCoefficient = _triangleFillOpacity / _lineIntensityDenominator;
    }

    private void SetTriangleFillOpacity(float value)
	{
        _triangleFillOpacity = value;
        _triangleColorOffset = 1 - _triangleFillOpacity;
        _triangleColorCoefficient = _triangleFillOpacity / _lineIntensityDenominator;
    }
    
    private void SetClearColor(Color value)
	{
        _clearColor = value;
        _backgroundBatch.quads[0].color = value;
	}

    private float _connectionDistanceSquared;
    private float _lineIntensityDenominator;
    private float _triangleColorOffset;
    private float _triangleColorCoefficient;
    private Gradient _currentLineColorGradient;
    private GradientColorKey[] _currentLineGradientColorKeys;
    private FastList<Particle>[] _neighbours;
    private AnimationCurve _alphaCurveInternal;
    private Coroutine _colorChanger;
    // rendering stuff
    private SimpleDrawBatch _backgroundBatch;
    private SimpleDrawBatch _mainBatch;
    // stats
    private int _lineDrawCalls;
    private int _actualLineDrawCalls;
    private int _triangleDrawCalls;
    private int _actualTriangleDrawCalls;
    private bool _colorChangerIterationFlag;

    public int LineDrawCalls => _lineDrawCalls;
    public int ActualLineDrawCalls => _actualLineDrawCalls;
    public int TriangleDrawCalls => _triangleDrawCalls;
    public int ActualTriangleDrawCalls => _actualTriangleDrawCalls;
    public bool ColorChangerIterationFlag => _colorChangerIterationFlag;

    private void InvertGradientKeys(GradientColorKey[] keys)
	{
        for (int i = 0; i < keys.Length; i++)
            keys[i].time = 1 - keys[i].time;
    }

    private void InvertGradientKeys(GradientAlphaKey[] keys)
    {
        for (int i = 0; i < keys.Length; i++)
            keys[i].time = 1 - keys[i].time;
    }

    private void InvertAnimationCurveKeys(Keyframe[] keys)
	{
        for (int i = 0; i < keys.Length; i++)
        {
            keys[i].time = 1 - keys[i].time;
            (keys[i].inTangent, keys[i].outTangent) = (-keys[i].outTangent, -keys[i].inTangent);
            (keys[i].inWeight, keys[i].outWeight) = (keys[i].outWeight, keys[i].inWeight);
            switch (keys[i].weightedMode)
			{
                case WeightedMode.In:
                    keys[i].weightedMode = WeightedMode.Out;
                    break;
                case WeightedMode.Out:
                    keys[i].weightedMode = WeightedMode.In;
                    break;
            }
        }
    }

    private void UpdateBackgroundQuad()
	{
        float extents = 0.1f;
        float width = _viewport.MaxX + extents;
        float height = _viewport.MaxY + extents;
        _backgroundBatch.quads[0].SetCoords(-width, -height, -width, height, width, height, width, -height);
    }

    private void Awake()
    {
        _neighbours = new FastList<Particle>[4];
        _backgroundBatch = new SimpleDrawBatch();
        _backgroundBatch.quads = new FastList<QuadEntry>();
        _mainBatch = new SimpleDrawBatch();
        _mainBatch.lines = new FastList<LineEntry>();
        _mainBatch.meshLines = new FastList<MeshLineEntry>();
        _mainBatch.triangles = new FastList<TriangleEntry>();
        _alphaCurveInternal = new AnimationCurve();
        _currentLineColorGradient = new Gradient();

        SetConnectionDistance(_connectionDistance);
        SetStrongDistance(_strongDistance);
        SetTriangleFillOpacity(_triangleFillOpacity);

        SetAlphaCurve(_alphaCurve);
        SetLineColor(_lineColor);
        SetAlternateLineColor(_alternateLineColor);

        _backgroundBatch.quads.Add(new QuadEntry());

        _viewport.CameraDimensionsChanged += UpdateBackgroundQuad;
        UpdateBackgroundQuad();
        SetClearColor(_clearColor);

        _drawer.AddBatch(_renderQueueIndex, _backgroundBatch);
        _drawer.AddBatch(_renderQueueIndex + 1, _mainBatch);
    }

    private void Update()
    {
        _mainBatch.lines.PseudoClear();
        _mainBatch.triangles.PseudoClear();
        _mainBatch.meshLines.PseudoClear();

        _lineDrawCalls = 0;
        _actualLineDrawCalls = 0;
        _triangleDrawCalls = 0;
        _actualTriangleDrawCalls = 0;

        if (_showLines == false && _showTriangles == false) return;

        var regionMap = _particleController.RegionMap;
        int maxSquareX = _particleController.MaxSquareX;
        int maxSquareY = _particleController.MaxSquareY;

        for (int ry = 0; ry <= maxSquareY; ry++)
        {
            for (int rx = 0; rx <= maxSquareX; rx++)
            {
                FastList<Particle> current = regionMap[rx, ry];
                if (current._count == 0) continue;

                int xFrom = rx == 0 ? 0 : rx - 1, xTo = rx == maxSquareX ? rx : rx + 1;
                int yFrom = ry, yTo = ry == maxSquareY ? ry : ry + 1;

                // Draw lines between a point in the current cell and the point in one of the 4 neighbour cells
                // . - skipped, C - current cell, # - *neighbour* cell
                //
                // ......
                // ..CC##
                // ######
                //
                int neighbourCount = 0;
                for (int x = xFrom; x <= xTo; x++) {
                    for (int y = yFrom; y <= yTo; y++) {
                        if (x <= rx && y == ry) continue;
                        _neighbours[neighbourCount] = regionMap[x, y];
                        neighbourCount++;
                    }
                }

                if (_showLines)
                {
                    // Draw lines between two points in the current cell
                    for (int i = 0; i < current._count; i++)
                        for (int j = i + 1; j < current._count; j++)
                            DrawLine(current[i], current[j]);

                    for (int neighbourIndex = 0; neighbourIndex < neighbourCount; neighbourIndex++)
                    {
                        FastList<Particle> neighbour = _neighbours[neighbourIndex];

                        for (int j = 0; j < neighbour._count; j++)
                            for (int i = 0; i < current._count; i++)
                                DrawLine(current[i], neighbour[j]);
                    }
                }

                if (_showTriangles)
                {
                    bool zerosNeighbour = neighbourCount == 4;

                    for (int i = 0; i < current._count; i++)
                    {
                        for (int j = i + 1; j < current._count; j++)
                        {
                            // Triangles indide the current cell
                            // no heuristics available
                            for (int k = j + 1; k < current._count; k++)
                                DrawTriangle(current[i], current[j], current[k]);
                            // Triangles with 2 points inside the current cell
                            // no heuristics available
                            for (int neighbourIndex = 0; neighbourIndex < neighbourCount; neighbourIndex++)
                            {
                                FastList<Particle> neighbour = _neighbours[neighbourIndex];

                                for (int k = 0; k < neighbour._count; k++)
                                    DrawTriangle(current[i], current[j], neighbour[k]);
                            }
                        }
                        // Triangles with 1 point inside the current cell

                        // Triangles with 2 points in the same neighbour cell
                        for (int nIndex1 = 0; nIndex1 < neighbourCount; nIndex1++)
                        {
                            FastList<Particle> neighbour1 = _neighbours[nIndex1];

                            for (int j = 0; j < neighbour1._count; j++)
                                for (int k = j + 1; k < neighbour1._count; k++)
                                    DrawTriangle(current[i], neighbour1[j], neighbour1[k]);
                        }

                        // Triangles with 2 points in 2 different neighbour cells
                        for (int nIndex1 = 0; nIndex1 < neighbourCount; nIndex1++)
                        {
                            FastList<Particle> neighbour1 = _neighbours[nIndex1];
                            
                            for (int nIndex2 = nIndex1 + 1; nIndex2 < neighbourCount; nIndex2++)
                            {
                                // Cells that are 1 unit apart cannot connect
                                if (zerosNeighbour && nIndex2 > 1) continue;
                                FastList<Particle> neighbour2 = _neighbours[nIndex2];

                                for (int j = 0; j < neighbour1._count; j++)
                                    for (int k = 0; k < neighbour2._count; k++)
                                        DrawTriangle(current[i], neighbour1[j], neighbour2[k]);
                            }

                            zerosNeighbour = false;
                        }
                    }
                }
            }
        }
    }

    private IEnumerator ColorChanger()
	{
        while (true)
		{
            Color oldColor = _currentLineGradientColorKeys[0].color;
            Color newColor = Random.ColorHSV(_colorMinHue, _colorMaxHue, _colorMinSaturation, _colorMaxSaturation, _colorMinValue, _colorMaxValue);
            float fadeTime = Random.Range(_colorMinFadeDuration, _colorMaxFadeDuration);
            float startTime = Time.time;
            float progress = 0;
            if (fadeTime == 0)
			{
                fadeTime = 0.01f;
                startTime -= 1;
			}

            while (progress < 1)
			{
                _colorChangerIterationFlag = false;
                progress = (Time.time - startTime) / fadeTime;
                Color currentColor = Color.Lerp(oldColor, newColor, progress);
                _currentLineGradientColorKeys[0].color = currentColor;
                _currentLineColorGradient.colorKeys = _currentLineGradientColorKeys;

                yield return null;
			}
		}
	}

    private void DrawLine(Particle p1, Particle p2)
	{
        Vector3 pos1 = p1.Position, pos2 = p2.Position;

        float diffX = pos1.x - pos2.x;
        float diffY = pos1.y - pos2.y;
        float distanceSquared = diffX * diffX + diffY * diffY;
        if (distanceSquared > _connectionDistanceSquared) return;
        float distance = (float)System.Math.Sqrt(distanceSquared);

        float intensity = (distance - _strongDistance) / _lineIntensityDenominator;
        Color color = _currentLineColorGradient.Evaluate(intensity);
        color.a = _alphaCurveInternal.Evaluate(intensity);

        if (_meshLines)
            _mainBatch.meshLines.Add(new MeshLineEntry(pos1.x, pos1.y, pos2.x, pos2.y, _linesWidth, color));
        else
            _mainBatch.lines.Add(new LineEntry(pos1.x, pos1.y, pos2.x, pos2.y, color));
    }

    private void DrawTriangle(Particle p1, Particle p2, Particle p3)
    {
        Vector3 pos1 = p1.Position, pos2 = p2.Position, pos3 = p3.Position;

        float diffX1 = pos1.x - pos2.x, diffY1 = pos1.y - pos2.y;
        float side1Sqr = diffX1 * diffX1 + diffY1 * diffY1;
        if (side1Sqr > _connectionDistanceSquared) return;
        float diffX2 = pos1.x - pos3.x, diffY2 = pos1.y - pos3.y;
        float side2Sqr = diffX2 * diffX2 + diffY2 * diffY2;
        if (side2Sqr > _connectionDistanceSquared) return;
        float diffX3 = pos2.x - pos3.x, diffY3 = pos2.y - pos3.y;
        float side3Sqr = diffX3 * diffX3 + diffY3 * diffY3;
        if (side3Sqr > _connectionDistanceSquared) return;

        float maxSideSqr = System.Math.Max(System.Math.Max(side1Sqr, side2Sqr), side3Sqr);
        float maxSide = (float)System.Math.Sqrt(maxSideSqr);

        float intensity = _triangleColorOffset + _triangleColorCoefficient * (maxSide - _strongDistance);

        Color color = _currentLineColorGradient.Evaluate(intensity);
        color.a = _alphaCurveInternal.Evaluate(intensity);

        _mainBatch.triangles.Add(new TriangleEntry(
            pos1.x, pos1.y, pos2.x, pos2.y, pos3.x, pos3.y, color
            ));
    }
}
