using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationController : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private SpriteRenderer _background;
    [SerializeField] private GraphicMeshDrawer _drawer;

    [Header("Prefabs")]
    [SerializeField] private GameObject _particlePrefab;
    [SerializeField] private GameObject _linePrefab;

    [Header("Main parameters")]
    [SerializeField] private bool _randomizeInitialPosition = true;
    [SerializeField] private float _particlesScale = 0.1f;
    [SerializeField] private float _linesWidth = 0.1f;

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
    private List<Line> _lines;
    private float _xBound;
    private float _yBound;
    private List<Particle>[,] _regionMap;
    private int _xSquareOffset;
    private int _ySquareOffset;
    private int _maxSquareX;
    private int _maxSquareY;
    private float _intensityDenominator;
    private Gradient _currentLineColor;
    private GradientColorKey[] _currentColorKeys;
    private GradientAlphaKey[] _currentAlphaKeys;

    private float GetParticleVelocity(Particle particle)
    {
        return Random.Range(_minParticleVelocity, _maxParticleVelocity);
    }

    private void Awake()
    {
        _yBound = _mainCamera.orthographicSize;
        _xBound = _mainCamera.aspect * _mainCamera.orthographicSize;

        _currentLineColor = _lineColor;
        _currentColorKeys = new GradientColorKey[] { new GradientColorKey(_lineColor.Evaluate(1), 1) };
        _currentAlphaKeys = _lineColor.alphaKeys;

        _particles = new List<Particle>(_particlesCount);
        _lines = new List<Line>();

        for (int i = 0; i < _particlesCount; i++)
        {
            Particle particle = Instantiate(_particlePrefab, transform).GetComponent<Particle>();
            particle.VelocityDelegate = GetParticleVelocity;
            particle.XBound = _xBound;
            particle.YBound = _yBound;
            particle.Visible = _showParticles;
            particle.Color = _particlesColor;
            particle.transform.localScale = Vector3.one * _particlesScale;
            if (_randomizeInitialPosition)
            {
                particle.transform.position = new Vector3(Random.Range(-_xBound, _xBound), Random.Range(-_yBound, _yBound));
            }

            _particles.Add(particle);
        }

        _intensityDenominator = _connectionDistance - _strongDistance;
        _xSquareOffset = Mathf.FloorToInt(_xBound / _connectionDistance) + 1;
        _ySquareOffset = Mathf.FloorToInt(_yBound / _connectionDistance) + 1;
        _maxSquareX = _xSquareOffset * 2 - 1;
        _maxSquareY = _ySquareOffset * 2 - 1;
        _regionMap = new List<Particle>[_maxSquareX + 1, _maxSquareY + 1];
        int cellsCount = (_maxSquareX + 1) * (_maxSquareY + 1);
        int averagePerSquare = Mathf.CeilToInt((float)_particlesCount / cellsCount);
        for (int i = 0; i <= _maxSquareX; i++)
		{
            for (int j = 0; j <= _maxSquareY; j++)
			{
                _regionMap[i, j] = new List<Particle>(averagePerSquare);
			}
		}

        if (_changeLinesColor)
        {
            _currentLineColor = new Gradient();
            StartCoroutine(ColorChanger());
        }

        _background.color = _backgroundColor;
        _background.transform.localScale = new Vector3(_xBound * 2 + 1, _yBound * 2 + 1, 1);

        Application.targetFrameRate = _targetFps;
    }

    private void Update()
    {
        if (_showLines == false) return;

        for (int i = 0; i < _particlesCount; i++)
        {
            Particle p = _particles[i];
            (int, int) square = GetSquare(p.transform.position);
            _regionMap[square.Item1, square.Item2].Add(p);
        }

        //int lineIndex = 0;
        for (int ry = 0; ry <= _maxSquareY; ry++) {
            for (int rx = 0; rx <= _maxSquareX; rx++)
            {
                List<Particle> current = _regionMap[rx, ry];
                if (current.Count == 0) continue;

                for (int i = 0; i < current.Count; i++)
                    for (int j = i + 1; j < current.Count; j++)
                        HandleParticleConnection(current[i], current[j]/*, ref lineIndex*/);

                for (int x = -1; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        if (x == 0 && y == 0) continue;
                        int newX = rx + x, newY = ry + y;
                        if (newX < 0 || newX > _maxSquareX || newY > _maxSquareY) continue;
                        List<Particle> available = _regionMap[newX, newY];

                        for (int j = 0; j < available.Count; j++)
                            for (int i = 0; i < current.Count; i++)
                                HandleParticleConnection(current[i], available[j]/*, ref lineIndex*/);
                    }
                }

                _regionMap[rx, ry].Clear();
            }
        }
        /*
        for (; lineIndex < _lines.Count; lineIndex++)
		{
            _lines[lineIndex].Enabled = false;
		}
        */
        //////////////////////////////////////
        return;
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

        return;
        for (int i = 0; i <= _maxSquareX; i++)
		{
            for (int j = 0; j <= _maxSquareY; j++)
			{
                float x = (i - _xSquareOffset) * _connectionDistance;
                float y = (j - _ySquareOffset) * _connectionDistance;
                DebugFillSquare(x, y, _connectionDistance, cellColor);
			}
		}
    }

    private IEnumerator ColorChanger()
	{
        while (true)
		{
            Color oldColor = _currentColorKeys[0].color;
            Color newColor = Random.ColorHSV(_colorMinHue, _colorMaxHue, _colorMinSaturation, _colorMaxSaturation, _colorMinValue, _colorMaxValue);
            float fadeTime = Random.Range(_colorMinFadeDuration, _colorMaxFadeDuration);
            float startTime = Time.time;
            float progress = 0;

            while (progress < 1)
			{
                progress = (Time.time - startTime) / fadeTime;
                Color currentColor = Color.Lerp(oldColor, newColor, progress);
                _currentColorKeys[0].color = currentColor;
                _currentLineColor.SetKeys(_currentColorKeys, _currentAlphaKeys);

                yield return null;
			}
		}
	}

    private void HandleParticleConnection(Particle p1, Particle p2/*, ref int lineIndex*/)
	{
        Vector3 pos1 = p1.Position, pos2 = p2.Position;

        float diffX = pos1.x - pos2.x;
        float diffY = pos1.y - pos2.y;
        float distance = (float)System.Math.Sqrt(diffX * diffX + diffY * diffY);
        if (distance > _connectionDistance) return;

        float intensity = 1 - (distance - _strongDistance) / _intensityDenominator;
        _drawer.DrawLineGL(pos1, pos2, _currentLineColor.Evaluate(intensity));
    }

    private (int, int) GetSquare(Vector3 location)
	{
        int xp = Mathf.FloorToInt(location.x / _connectionDistance) + _xSquareOffset;
        int yp = Mathf.FloorToInt(location.y / _connectionDistance) + _ySquareOffset;
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
