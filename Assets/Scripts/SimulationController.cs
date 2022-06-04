using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core;
using SimpleGraphics;

public class SimulationController : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private BatchRenderer _drawer;
    [SerializeField] private Viewport _viewport;
    [SerializeField] private Fragmentator _fragmentator;

    [Header("Prefabs")]
    [SerializeField] private GameObject _particlePrefab;

    [Header("Initialization parameters")]
    [SerializeField] private bool _randomizeInitialPosition = true;

    [Header("Simulation parameters")]
    [SerializeField] private float _particlesScale = 0.1f;
    [SerializeField] private float _linesWidth = 0.1f;
    [SerializeField] private bool _meshLines = false;
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
    [SerializeField] private Color _clearColor = Color.black;
    [SerializeField] [Range(0,1)] private float _triangleFillOpacity;

    [Header("Rendering")]
    [SerializeField] private int _renderQueueIndex = 0;

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
    public Color ClearColor
    {
        get => _clearColor;
        set { if (_clearColor != value) { SetClearColor(value); ClearColorChanged?.Invoke(value); }; }
    }

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
    public event System.Action<Color> ClearColorChanged;

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
            particle.Viewport = _viewport;
            particle.Color = ParticleColor;
            particle.Visible = ShowParticles;
            particle.Size = ParticleSize;
            particle.SetRandomVelocity();
            if (_randomizeInitialPosition)
            {
                particle.Position = new Vector3(Random.Range(-_viewport.MaxX, _viewport.MaxX), 
                    Random.Range(-_viewport.MaxY, _viewport.MaxY));
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

        _fragmentator.SetConnectionDistance(value);
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
    private void SetClearColor(Color value)
	{
        _clearColor = value;
        _backgroundBatch.quads[0].color = value;
	}

    private List<Particle> _particles;
    private float _connectionDistanceSquared;
    private float _lineIntensityDenominator;
    private float _triangleColorOffset;
    private float _triangleColorCoefficient;
    private Gradient _currentLineColorGradient;
    private GradientColorKey[] _currentLineGradientColorKeys;
    private GradientAlphaKey[] _currentLineGradientAlphaKeys;
    // rendering stuff
    private SimpleDrawBatch _backgroundBatch;
    private SimpleDrawBatch _mainBatch;

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

    private void UpdateBackgroundQuad()
	{
        float extents = 0.1f;
        float width = _viewport.MaxX + extents;
        float height = _viewport.MaxY + extents;
        _backgroundBatch.quads[0].SetCoords(-width, -height, -width, height, width, height, width, -height);
    }

    private void Awake()
    {
        _particles = new List<Particle>(_particlesCount);

        _fragmentator.Viewport = _viewport;
        _fragmentator.Particles = _particles;
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

        _backgroundBatch = new SimpleDrawBatch();
        _backgroundBatch.quads = new FastList<QuadEntry>();
        _backgroundBatch.quads.Add(new QuadEntry());

        _viewport.CameraDimensionsChanged += UpdateBackgroundQuad;
        UpdateBackgroundQuad();
        SetClearColor(_clearColor);

        _mainBatch = new SimpleDrawBatch();
        _mainBatch.lines = new FastList<LineEntry>();
        _mainBatch.meshLines = new FastList<MeshLineEntry>();
        _mainBatch.triangles = new FastList<TriangleEntry>();

        _drawer.AddBatch(_renderQueueIndex, _backgroundBatch);
        _drawer.AddBatch(_renderQueueIndex + 1, _mainBatch);
    }

    private void Update()
    {
        _mainBatch.lines.PseudoClear();
        _mainBatch.triangles.PseudoClear();
        _mainBatch.meshLines.PseudoClear();

        if (_showLines == false && _showTriangles == false) return;

        var regionMap = _fragmentator.RegionMap;
        int maxSquareX = _fragmentator.MaxSquareX;
        int maxSquareY = _fragmentator.MaxSquareY;

        for (int ry = 0; ry <= maxSquareY; ry++)
        {
            for (int rx = 0; rx <= maxSquareX; rx++)
            {
                FastList<Particle> current = regionMap[rx, ry];
                if (current._count == 0) continue;

                int xFrom = rx == 0 ? 0 : rx - 1, xTo = rx == maxSquareX ? rx : rx + 1;
                int yFrom = ry, yTo = ry == maxSquareY ? ry : ry + 1;

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
                            FastList<Particle> available = regionMap[x, y];

                            for (int j = 0; j < available._count; j++)
                                for (int i = 0; i < current._count; i++)
                                    DrawLine(current[i], available[j]);
                        }
                    }
                }

                if (_showTriangles)
                {
                    FastList<Particle> surrounding = new FastList<Particle>();
                    for (int x = xFrom; x <= xTo; x++)
                    {
                        for (int y = yFrom; y <= yTo; y++)
                        {
                            if (x <= rx && y == ry) continue;
                            surrounding.AddRange(regionMap[x, y]);
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
        _mainBatch.triangles.Add(new TriangleEntry(
            pos1.x, pos1.y, pos2.x, pos2.y, pos3.x, pos3.y, color
            ));
    }
}
