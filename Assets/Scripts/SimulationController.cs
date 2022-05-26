using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core;

public class SimulationController : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private SimpleGraphics _drawer;

    [Header("Prefabs")]
    [SerializeField] private GameObject _particlePrefab;

    [Header("Main parameters")]
    [SerializeField] private bool _randomizeInitialPosition = true;
    [SerializeField] private float _particlesScale = 0.1f;
    [SerializeField] private float _linesWidth = 0.1f;
    [SerializeField] private bool _meshLines = false;

    [Header("Simulation parameters")]
    [SerializeField] private int _particlesCount = 100;
    [SerializeField] private float _connectionDistance = 60f;
    [SerializeField] private float _strongDistance = 10f;
    [SerializeField] private Color _particlesColor = Color.white;
    [SerializeField] private Gradient _lineColor;
    [SerializeField] private bool _showParticles = true;
    [SerializeField] private bool _showLines = true;
    [SerializeField] private bool _showTriangles = true;
    [SerializeField] private float _minParticleVelocity = 0;
    [SerializeField] private float _maxParticleVelocity = 1;
    [SerializeField] private bool _changeLinesColor = true;
    [SerializeField] private float _colorMinHue = 0;
    [SerializeField] private float _colorMaxHue = 1;
    [SerializeField] private float _colorMinSaturation = 0;
    [SerializeField] private float _colorMaxSaturation = 1;
    [SerializeField] private float _colorMinValue = 0.3f;
    [SerializeField] private float _colorMaxValue = 1;
    [SerializeField] private float _colorMinFadeDuration = 2;
    [SerializeField] private float _colorMaxFadeDuration = 4;
    [SerializeField] private Color _backgroundColor = Color.black;
    [SerializeField] private int _targetFps;
    [SerializeField] [Range(0,1)] private float _triangleFillOpacity;

    [Header("Debug")]
    [SerializeField] private bool _showCellBorders = false;
    [SerializeField] private Color _cellBorderColor = Color.red;
    [SerializeField] private bool _showCells = false;
    [SerializeField] private Color _cellColor = Color.yellow;
    [SerializeField] [Range(0, 0.000001f)] private float _linesPerformanceImpact;
    [SerializeField] [Range(0, 0.01f)] private float _performaceBias;
    [SerializeField] [Range(0, 0.000001f)] private float _particlesPerformanceImpact;

    public bool ShowParticles
	{
        get => _showParticles;
        set { if (_showParticles != value) { SetShowParticles(value); ShowParticlesChanged?.Invoke(value); } }
	}
    public float ParticleSize
	{
        get => _particlesScale;
        set { if (_particlesScale != value) { SetParticleSize(value); ParticleSizeChanged?.Invoke(value); } }
    }
    public Color ParticleColor
    {
        get => _particlesColor;
        set { if (_particlesColor != value) { SetParticleColor(value); ParticleColorChanged?.Invoke(value); } }
    }
    public bool ShowLines
	{
        get => _showLines;
        set { if (_showLines != value) { _showLines = value; ShowLinesChanged?.Invoke(value); }; }
	}
    public bool MeshLines
	{
        get => _meshLines;
        set { if (_meshLines != value) { _meshLines = value; MeshLinesChanged?.Invoke(value); }; }
    }
    public Color LineColorTemp
	{
        get => _currentLineGradientColorKeys[0].color;
        set { if (_currentLineGradientColorKeys[0].color != value) { SetLineColorTemp(value); LineColorTempChanged?.Invoke(value); } }
    }
    public float LineWidth
	{
        get => _linesWidth;
        set { if (_linesWidth != value) { _linesWidth = value; LineWidthChanged?.Invoke(value); }; }
    }
    public int ParticleCount
	{
        get => _particlesCount;
        set { if (_particlesCount != value) { SetParticlesCount(value); ParticleCountChanged?.Invoke(value); } }
    }
    public float ConnectionDistance
    {
        get => _connectionDistance;
        set { if (_connectionDistance != value) { SetConnectionDistance(value); ConnectionDistanceChanged?.Invoke(value); }; }
    }
    public float StrongDistance
    {
        get => _strongDistance;
        set { if (_strongDistance != value) { SetStrongDistance(value); StrongDistanceChanged?.Invoke(value); }; }
    }
    public bool ShowTriangles
	{
        get => _showTriangles;
        set { if (_showTriangles != value) { _showTriangles = value; ShowTrianglesChanged?.Invoke(value); }; }
    }
    public float TriangleFillOpacity
	{
        get => _triangleFillOpacity;
        set { if (_triangleFillOpacity != value) { SetTriangleFillOpacity(value); TriangleFillOpacityChanged?.Invoke(value); }; }
    }
    public float MinParticleVelocity
	{
        get => _minParticleVelocity;
        set { if (_minParticleVelocity != value) { SetMinParticleVelocity(value); MinParticleVelocityChanged?.Invoke(value); }; }
    }
    public float MaxParticleVelocity
	{
        get => _maxParticleVelocity;
        set { if (_maxParticleVelocity != value) { SetMaxParticleVelocity(value); MaxParticleVelocityChanged?.Invoke(value); }; }
    }

    public bool ShowCellBorders
	{
        get => _showCellBorders;
        set { if (_showCellBorders != value) { _showCellBorders = value; ShowCellBordersChanged?.Invoke(value); }; }
	}
    public bool ShowCells
    {
        get => _showCells;
        set { if (_showCells != value) { _showCells = value; ShowCellsChanged?.Invoke(value); }; }
    }

    public float LineIterationsEstimated => (
        CellCount * (AveragePerCell - 1 + 1) * AveragePerCell / 2 +
        (_maxSquareX - 1) * _maxSquareY * AveragePerCell * AveragePerCell * 4 +
        _maxSquareY * AveragePerCell * AveragePerCell * 3 +
        _maxSquareY * AveragePerCell * AveragePerCell * 2 +
        _maxSquareX * AveragePerCell * AveragePerCell) * 1.15f;

    public float EstimatedFps => 1 / (_performaceBias + LineIterationsEstimated * _linesPerformanceImpact + _particlesPerformanceImpact * _particlesCount); 

    public event System.Action<bool> ShowParticlesChanged;
    public event System.Action<float> ParticleSizeChanged;
    public event System.Action<Color> ParticleColorChanged;
    public event System.Action<bool> ShowLinesChanged;
    public event System.Action<bool> MeshLinesChanged;
    public event System.Action<Color> LineColorTempChanged;
    public event System.Action<float> LineWidthChanged;
    public event System.Action<int> ParticleCountChanged;
    public event System.Action<float> ConnectionDistanceChanged;
    public event System.Action<float> StrongDistanceChanged;
    public event System.Action<bool> ShowTrianglesChanged;
    public event System.Action<float> TriangleFillOpacityChanged;
    public event System.Action<float> MinParticleVelocityChanged;
    public event System.Action<float> MaxParticleVelocityChanged;
    public event System.Action<bool> ShowCellBordersChanged;
    public event System.Action<bool> ShowCellsChanged;

    public int CellCount => (_maxSquareX + 1) * (_maxSquareY + 1);
    private float AveragePerCell =>(float)_particlesCount / CellCount;

    private void SetShowParticles(bool value)
	{
        _showParticles = value;
        foreach (Particle p in _particles)
            p.Visible = value;
	}
    private void SetParticleSize(float value)
    {
        _particlesScale = value;
        foreach (Particle p in _particles)
            p.Size = value;
    }
    private void SetParticleColor(Color value)
    {
        _particlesColor = value;
        foreach (Particle p in _particles)
            p.Color = value;
    }

    private void SetLineColorTemp(Color value)
	{
        for (int i = 0; i < _currentLineGradientColorKeys.Length; i++)
            _currentLineGradientColorKeys[i].color = value;

        _currentLineColorGradient.SetKeys(_currentLineGradientColorKeys, _currentLineGradientAlphaKeys);
	}

    private void SetParticlesCount(int value)
    {
        _particlesCount = value;

        while (_particles.Count > value)
        {
            int index = _particles.Count - 1;
            Destroy(_particles[index].gameObject);
            _particles.RemoveAt(index);
        }

        while (_particles.Count < value)
        {
            Particle particle = Instantiate(_particlePrefab).GetComponent<Particle>();
            particle.VelocityDelegate = GetParticleVelocity;
            particle.XBound = _xBound;
            particle.YBound = _yBound;
            particle.Color = ParticleColor;
            particle.Visible = ShowParticles;
            particle.Size = ParticleSize;
            particle.SetRandomVelocity();
            if (_randomizeInitialPosition)
            {
                particle.Position = new Vector3(Random.Range(-_xBound, _xBound), Random.Range(-_yBound, _yBound));
            }

            _particles.Add(particle);
        }
    }
    private void SetConnectionDistance(float value)
	{
        _connectionDistance = value;
        _connectionDistanceSquared = _connectionDistance * _connectionDistance;
        _lineIntensityDenominator = _connectionDistance - _strongDistance;
        _triangleColorCoefficient = _triangleFillOpacity / _lineIntensityDenominator;

        if (_connectionDistance <= 0) return;
        int xSquareOffset = Mathf.FloorToInt(_xBound / _connectionDistance) + 1;
        int ySquareOffset = Mathf.FloorToInt(_yBound / _connectionDistance) + 1;
        if (_xSquareOffset == xSquareOffset && _ySquareOffset == ySquareOffset) return;

        _xSquareOffset = xSquareOffset;
        _ySquareOffset = ySquareOffset;
        _maxSquareX = _xSquareOffset * 2 - 1;
        _maxSquareY = _ySquareOffset * 2 - 1;
        _regionMap = new FastList<Particle>[_maxSquareX + 1, _maxSquareY + 1];

        for (int i = 0; i <= _maxSquareX; i++)
            for (int j = 0; j <= _maxSquareY; j++)
                _regionMap[i, j] = new FastList<Particle>(Mathf.CeilToInt(2 * AveragePerCell));
    }
    private void SetStrongDistance(float value)
    {
        _strongDistance = value;
        _lineIntensityDenominator = _connectionDistance - _strongDistance;
    }

    private void SetTriangleFillOpacity(float value)
	{
        _triangleFillOpacity = value;
        _triangleColorOffset = 1 - _triangleFillOpacity;
        _triangleColorCoefficient = _triangleFillOpacity / _lineIntensityDenominator;
    }
    
    private void SetMinParticleVelocity(float value)
	{
        _minParticleVelocity = value;

        foreach (Particle p in _particles) {
            float magnitude = p.Velocity.magnitude;
            if (magnitude >= _minParticleVelocity) continue;
            if (magnitude == 0) {
                p.SetRandomVelocity();
                continue;
            }
            p.Velocity = p.Velocity / magnitude * _minParticleVelocity;
        }
	}
    private void SetMaxParticleVelocity(float value)
    {
        _maxParticleVelocity = value;

        foreach (Particle p in _particles)
        {
            float magnitude = p.Velocity.magnitude;
            if (magnitude <= _maxParticleVelocity) continue;

            p.Velocity = Vector3.ClampMagnitude(p.Velocity, _maxParticleVelocity);
        }
    }

    private List<Particle> _particles;
    private float _xBound;
    private float _yBound;
    private FastList<Particle>[,] _regionMap;
    private int _xSquareOffset;
    private int _ySquareOffset;
    private int _maxSquareX;
    private int _maxSquareY;
    private float _lineIntensityDenominator;
    private float _triangleColorOffset;
    private float _triangleColorCoefficient;
    private Gradient _currentLineColorGradient;
    private GradientColorKey[] _currentLineGradientColorKeys;
    private GradientAlphaKey[] _currentLineGradientAlphaKeys;
    private float _connectionDistanceSquared;
    // rendering stuff
    private SimpleGraphics.SimpleDrawBatch _backgroundBatch;
    private SimpleGraphics.SimpleDrawBatch _mainBatch;
    private SimpleGraphics.SimpleDrawBatch _overlayBatch;

    private float GetParticleVelocity(Particle particle)
    {
        return Random.Range(_minParticleVelocity, _maxParticleVelocity);
    }

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

    private void Awake()
    {
        _yBound = _mainCamera.orthographicSize;
        _xBound = _mainCamera.aspect * _mainCamera.orthographicSize;
        _particles = new List<Particle>(_particlesCount);

		SetConnectionDistance(_connectionDistance);
        SetStrongDistance(_strongDistance);
        SetTriangleFillOpacity(_triangleFillOpacity);
        SetParticlesCount(_particlesCount);

        _currentLineColorGradient = new Gradient();
        _currentLineGradientAlphaKeys = _lineColor.alphaKeys;
        InvertGradientKeys(_currentLineGradientAlphaKeys);

        if (_changeLinesColor)
        {
            _currentLineGradientColorKeys = new GradientColorKey[] { new GradientColorKey(_lineColor.Evaluate(1), 1) };
            StartCoroutine(ColorChanger());
        } else
		{
            _currentLineGradientColorKeys = _lineColor.colorKeys;
            InvertGradientKeys(_currentLineGradientColorKeys);
            _currentLineColorGradient.SetKeys(_currentLineGradientColorKeys, _currentLineGradientAlphaKeys);
        }

        float bgWidth = _xBound * 2 + 1;
        float bgHeight = _yBound * 2 + 1;
        SimpleGraphics.QuadEntry backgroundQuad = new SimpleGraphics.QuadEntry(
            -bgWidth/2, -bgHeight/2, -bgWidth/2, bgHeight/2, bgWidth/2, bgHeight/2, bgWidth/2, -bgHeight/2, _backgroundColor
            );

        _backgroundBatch = new SimpleGraphics.SimpleDrawBatch();
        _backgroundBatch.quads = new FastList<SimpleGraphics.QuadEntry>();
        _backgroundBatch.quads.Add(backgroundQuad);

        _mainBatch = new SimpleGraphics.SimpleDrawBatch();
        _mainBatch.lines = new FastList<SimpleGraphics.LineEntry>();
        _mainBatch.meshLines = new FastList<SimpleGraphics.MeshLineEntry>();
        _mainBatch.triangles = new FastList<SimpleGraphics.TriangleEntry>();

        _overlayBatch = new SimpleGraphics.SimpleDrawBatch();
        _overlayBatch.lines = new FastList<SimpleGraphics.LineEntry>();
        _overlayBatch.quads = new FastList<SimpleGraphics.QuadEntry>();

        Application.targetFrameRate = _targetFps;
    }

    private void Update()
    {
        _drawer.AddBatch(_backgroundBatch);
        if (_showLines || _showTriangles)
        {
            _mainBatch.lines.PseudoClear();
            _mainBatch.triangles.PseudoClear();
            _mainBatch.meshLines.PseudoClear();
            _drawer.AddBatch(_mainBatch);
        }
        if (_showCellBorders || _showCells)
        {
            _overlayBatch.lines.PseudoClear();
            _overlayBatch.quads.PseudoClear();
            _drawer.AddBatch(_overlayBatch);
        }

        for (int i = 0; i < _particles.Count; i++)
        {
            Particle p = _particles[i];
            (int, int) square = GetSquare(p.Position);
            _regionMap[square.Item1, square.Item2].Add(p);
        }

        for (int ry = 0; ry <= _maxSquareY; ry++) {
            for (int rx = 0; rx <= _maxSquareX; rx++)
            {
                FastList<Particle> current = _regionMap[rx, ry];
                if (current._count == 0) continue;

				if (_showCells) { // debug cells draw
                    float x = (rx - _xSquareOffset) * _connectionDistance;
                    float y = (ry - _ySquareOffset) * _connectionDistance;
                    Color currentColor = _cellColor;
                    currentColor.a *= current._count / AveragePerCell;
                    _overlayBatch.quads.Add(new SimpleGraphics.QuadEntry(
                        x, y, x + _connectionDistance, y, x + _connectionDistance, y + _connectionDistance, x, y + _connectionDistance, currentColor
                        ));
                }

                int xFrom = rx == 0 ? 0 : rx - 1, xTo = rx == _maxSquareX ? rx : rx + 1;
                int yFrom = ry, yTo = ry == _maxSquareY ? ry : ry + 1;

                if (_showLines)
                {
                    // Draw lines between two points in the current cell
                    for (int i = 0; i < current._count; i++)
                        for (int j = i + 1; j < current._count; j++)
                            DrawLine(current[i], current[j]);

                    // Draw lines between a point in the current cell and the point in one of the 4 surrounding cells
                    // . - skipped, C - current cell, # - not skipped
                    //
                    // ......
                    // ..CC##
                    // ######
                    //
                    for (int x = xFrom; x <= xTo; x++)
                    {
                        for (int y = yFrom; y <= yTo; y++)
                        {
                            if (x <= rx && y == ry) continue;
                            FastList<Particle> available = _regionMap[x, y];

                            for (int j = 0; j < available._count; j++)
                                for (int i = 0; i < current._count; i++)
                                    DrawLine(current[i], available[j]);
                        }
                    }
                }

                if (_showTriangles)
                {
                    FastList<Particle> surrounding = new FastList<Particle>();
                    for (int x = xFrom; x <= xTo; x++) {
                        for (int y = yFrom; y <= yTo; y++)
                        {
                            if (x <= rx && y == ry) continue;
                            surrounding.AddRange(_regionMap[x, y]);
                        }
                    }

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
                            for (int k = 0; k < surrounding._count; k++)
                                DrawTriangle(current[i], current[j], surrounding[k]);
                        }
                        // Triangles with 1 point inside the current cell
                        for (int j = 0; j < surrounding._count; j++)
                            for (int k = j + 1; k < surrounding._count; k++)
                                DrawTriangle(current[i], surrounding[j], surrounding[k]);
                    }
                }

                _regionMap[rx, ry].PseudoClear();
            }
        }

        if (_showCellBorders)
        {
            for (float x = -Mathf.Floor(_xBound / _connectionDistance) * _connectionDistance; x < _xBound; x += _connectionDistance)
            {
                _overlayBatch.lines.Add(new SimpleGraphics.LineEntry(x, -_yBound, x, _yBound, _cellBorderColor));
            }

            for (float y = -Mathf.Floor(_yBound / _connectionDistance) * _connectionDistance; y < _yBound; y += _connectionDistance)
            {
                _overlayBatch.lines.Add(new SimpleGraphics.LineEntry(-_xBound, y, _xBound, y, _cellBorderColor));
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

            while (progress < 1)
			{
                progress = (Time.time - startTime) / fadeTime;
                Color currentColor = Color.Lerp(oldColor, newColor, progress);
                _currentLineGradientColorKeys[0].color = currentColor;
                _currentLineColorGradient.SetKeys(_currentLineGradientColorKeys, _currentLineGradientAlphaKeys);

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

        if (_meshLines)
            _mainBatch.meshLines.Add(new SimpleGraphics.MeshLineEntry(pos1.x, pos1.y, pos2.x, pos2.y, _linesWidth, color));
        else
            _mainBatch.lines.Add(new SimpleGraphics.LineEntry(pos1.x, pos1.y, pos2.x, pos2.y, color));
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
        _mainBatch.triangles.Add(new SimpleGraphics.TriangleEntry(
            pos1.x, pos1.y, pos2.x, pos2.y, pos3.x, pos3.y, color
            ));
    }

    private (int, int) GetSquare(Vector3 location)
	{
        int xp = (int)System.Math.Floor(location.x / _connectionDistance) + _xSquareOffset;
        int yp = (int)System.Math.Floor(location.y / _connectionDistance) + _ySquareOffset;
        return (Mathf.Clamp(xp, 0, _maxSquareX), Mathf.Clamp(yp, 0, _maxSquareY));
    }

    private void DebugFillSquare(float x, float y, float size, Color color)
	{
        float density = 10;
        float step = 1 / density;

        for (float i = 0; i < size; i += step)
		{
            Debug.DrawLine(new Vector3(x + i, y), new Vector3(x, y + i), color);
		}

        for (float i = 0; i < size; i += step)
        {
            Debug.DrawLine(new Vector3(x + _connectionDistance, y + i), new Vector3(x + i, y +_connectionDistance), color);
        }
    }
}
