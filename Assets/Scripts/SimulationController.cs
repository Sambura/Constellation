using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationController : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private GraphicMeshDrawer _drawer;

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

    private List<Particle> _particles;
    private float _xBound;
    private float _yBound;
    private List<Particle>[,] _regionMap;
    private int _xSquareOffset;
    private int _ySquareOffset;
    private int _maxSquareX;
    private int _maxSquareY;
    private float _intensityDenominator;
    private Gradient _currentLineColorGradient;
    private GradientColorKey[] _currentLineGradientColorKeys;
    private GradientAlphaKey[] _currentLineGradientAlphaKeys;

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

    public float PerformanceMeasure =>
        CellCount * (AveragePerCell - 1) * AveragePerCell / 2 + // for each cell
        (_maxSquareX - 1) * _maxSquareY * AveragePerCell * AveragePerCell * 4 + // for most cells: 4 directions
        _maxSquareY * AveragePerCell * AveragePerCell * 3 +
        _maxSquareY * AveragePerCell * AveragePerCell * 2 +
        _maxSquareX * AveragePerCell * AveragePerCell
        ;

    private float k => (38 - 220) / (1 / 106921.9f - 1 / 524.4f);
    private float b => 220 - k * (1 / 524.4f);
    public float EstimatedFps => b + k / PerformanceMeasure; 
    // 524.4 => 220
    // 106921.9 => 38

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
        _intensityDenominator = _connectionDistance - _strongDistance;

        int xSquareOffset = Mathf.FloorToInt(_xBound / _connectionDistance) + 1;
        int ySquareOffset = Mathf.FloorToInt(_yBound / _connectionDistance) + 1;
        if (_xSquareOffset == xSquareOffset && _ySquareOffset == ySquareOffset) return;

        _xSquareOffset = xSquareOffset;
        _ySquareOffset = ySquareOffset;
        _maxSquareX = _xSquareOffset * 2 - 1;
        _maxSquareY = _ySquareOffset * 2 - 1;
        _regionMap = new List<Particle>[_maxSquareX + 1, _maxSquareY + 1];

        for (int i = 0; i <= _maxSquareX; i++)
            for (int j = 0; j <= _maxSquareY; j++)
                _regionMap[i, j] = new List<Particle>(Mathf.CeilToInt(2 * AveragePerCell));
    }
    private void SetStrongDistance(float value)
    {
        _strongDistance = value;
        _intensityDenominator = _connectionDistance - _strongDistance;
    }

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

        _drawer.SetBgPositionAndSize(0, 0, _xBound * 2 + 1, _yBound * 2 + 1);
        _drawer.DrawBackgroundGL(new Color(_backgroundColor.r, _backgroundColor.g, _backgroundColor.b));

        Application.targetFrameRate = _targetFps;
    }

    private void Update()
    {
        _drawer.DrawBackgroundGL(_backgroundColor);
        if (_showLines == false) return;

        for (int i = 0; i < _particles.Count; i++)
        {
            Particle p = _particles[i];
            (int, int) square = GetSquare(p.Position);
            _regionMap[square.Item1, square.Item2].Add(p);
        }

        for (int ry = 0; ry <= _maxSquareY; ry++) {
            for (int rx = 0; rx <= _maxSquareX; rx++)
            {
                List<Particle> current = _regionMap[rx, ry];
                if (current.Count == 0) continue;

				if (false || true) { // debug cells draw
                    float x = (rx - _xSquareOffset) * _connectionDistance;
                    float y = (ry - _ySquareOffset) * _connectionDistance;
                    Color currentColor = new Color(1, 1, 0, 10f * AveragePerCell * current.Count / _particlesCount);
                    DebugFillSquare(x, y, _connectionDistance, currentColor);
                }

                for (int i = 0; i < current.Count; i++)
                    for (int j = i + 1; j < current.Count; j++)
                        HandleParticleConnection(current[i], current[j]);

                for (int x = -1; x < 2; x++) {
                    for (int y = 0; y < 2; y++)
                    {
                        if (x == 0 && y == 0) continue;
                        int newX = rx + x, newY = ry + y;
                        if (newX < 0 || newX > _maxSquareX || newY > _maxSquareY) continue;
                        List<Particle> available = _regionMap[newX, newY];

                        for (int j = 0; j < available.Count; j++)
                            for (int i = 0; i < current.Count; i++)
                                HandleParticleConnection(current[i], available[j]);
                    }
                }

                _regionMap[rx, ry].Clear();
            }
        }

        //return;
        Color cellBorderColor = new Color(1, 0, 0, 0.5f);
        Color cellColor = new Color(1, 1, 0, 0.2f);
        for (float x = -Mathf.Floor(_xBound / _connectionDistance) * _connectionDistance; x < _xBound; x += _connectionDistance)
		{
            Debug.DrawLine(new Vector3(x, -_yBound), new Vector3(x, _yBound), cellBorderColor);
		}
        
        for (float y = -Mathf.Floor(_yBound / _connectionDistance) * _connectionDistance; y < _yBound; y += _connectionDistance)
        {
            Debug.DrawLine(new Vector3(-_xBound, y), new Vector3(_xBound, y), cellBorderColor);
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

    private void HandleParticleConnection(Particle p1, Particle p2)
	{
        Vector3 pos1 = p1.Position, pos2 = p2.Position;

        float diffX = pos1.x - pos2.x;
        float diffY = pos1.y - pos2.y;
        float distance = (float)System.Math.Sqrt(diffX * diffX + diffY * diffY);
        if (distance > _connectionDistance) return;

        float intensity = (distance - _strongDistance) / _intensityDenominator;
        Color color = _currentLineColorGradient.Evaluate(intensity);

        if (_meshLines)
            _drawer.DrawMeshLineGL(pos1, pos2, color, _linesWidth);
        else
            _drawer.DrawLineGL(pos1, pos2, color);
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
