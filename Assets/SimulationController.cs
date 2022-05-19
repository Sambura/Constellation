using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SimulationController : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private Camera _mainCamera;

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

    private List<Particle> _particles;
    private List<Line> _lines;
    private float _sqrConnect;
    private float _sqrStrong;
    private float _xBound;
    private float _yBound;
    private Dictionary<(int, int), List<Particle>> _regionMap;
    private int _xSquareOffset;
    private int _ySquareOffset;

    private float GetParticleVelocity(Particle particle)
    {
        return Random.Range(_minParticleVelocity, _maxParticleVelocity);
    }

    private void Awake()
    {
        _yBound = _mainCamera.orthographicSize;
        _xBound = _mainCamera.aspect * _mainCamera.orthographicSize;

        _particles = new List<Particle>(_particlesCount);
        _lines = new List<Line>();
        _regionMap = new Dictionary<(int, int), List<Particle>>();

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

        _particles[0].Color = Color.green;

        _sqrConnect = _connectionDistance * _connectionDistance;
        _sqrStrong = _strongDistance * _strongDistance;
    }

    private void Update()
    {
        if (_showLines == false) return;

        for (int i = 0; i < _particles.Count; i++)
        {
            Particle p = _particles[i];
            (int, int) square = GetSquare(p.transform.position);
            if (i == 0) Debug.Log(square);
            if (_regionMap.ContainsKey(square))
                _regionMap[square].Add(p);
            else
                _regionMap.Add(square, new List<Particle>() { p });
        }

        var regionMapCopy = _regionMap.Select(x => (x.Key, x.Value)).ToList();

        int lineIndex = 0;
        foreach (var region in regionMapCopy)
		{
            List<Particle> toConnect = region.Value;
            List<Particle> available = new List<Particle>();
            (int, int) coords = region.Key;
            _regionMap.Remove(coords);
            for (int i = -1; i < 2; i++)
			{
                for (int j = -1; j < 2; j++)
				{
                    if (i == 0 && j == 0) continue;
                    if (_regionMap.TryGetValue((coords.Item1 + i, coords.Item2 + j), out List<Particle> value))
					{
                        available.AddRange(value);
					}
				}
			}

            for (int i = 0; i < toConnect.Count; i++)
            {
                for (int j = i + 1; j < toConnect.Count; j++)
                {
                    Particle p1 = toConnect[i], p2 = toConnect[j];
                    float sqrDistance = Vector2.SqrMagnitude(p1.transform.position - p2.transform.position);
                    if (sqrDistance > _sqrConnect) continue;
                    float intensity = 1 - (sqrDistance - _sqrStrong) / (_sqrConnect - _sqrStrong);

                    if (lineIndex >= _lines.Count)
                    {
                        Line newLine = Instantiate(_linePrefab, transform).GetComponent<Line>();
                        newLine.LineWidth = _linesWidth;
                        _lines.Add(newLine);
                    }

                    Line line = _lines[lineIndex];

                    line.Color = _lineColor.Evaluate(intensity);
                    line[0] = p1.transform.position;
                    line[1] = p2.transform.position;
                    line.Enabled = true;

                    lineIndex++;
                }
            }

            for (int i = 0; i < toConnect.Count; i++)
            {
                for (int j = 0; j < available.Count; j++)
                {
                    Particle p1 = toConnect[i], p2 = available[j];
                    float sqrDistance = Vector2.SqrMagnitude(p1.transform.position - p2.transform.position);
                    if (sqrDistance > _sqrConnect) continue;
                    float intensity = 1 - (sqrDistance - _sqrStrong) / (_sqrConnect - _sqrStrong);

                    if (lineIndex >= _lines.Count)
                    {
                        Line newLine = Instantiate(_linePrefab, transform).GetComponent<Line>();
                        newLine.LineWidth = _linesWidth;
                        _lines.Add(newLine);
                    }

                    Line line = _lines[lineIndex];

                    line.Color = _lineColor.Evaluate(intensity);
                    line[0] = p1.transform.position;
                    line[1] = p2.transform.position;
                    line.Enabled = true;

                    lineIndex++;
                }
            }
        }

        for (; lineIndex < _lines.Count; lineIndex++)
		{
            _lines[lineIndex].Enabled = false;
		}

        //////////////////////////////////////
        
        for (float x = -Mathf.Floor(_xBound / _connectionDistance) * _connectionDistance; x < _xBound; x += _connectionDistance)
		{
            Debug.DrawLine(new Vector3(x, -_yBound), new Vector3(x, _yBound), Color.red);
		}

        for (float y = -Mathf.Floor(_yBound / _connectionDistance) * _connectionDistance; y < _yBound; y += _connectionDistance)
        {
            Debug.DrawLine(new Vector3(-_xBound, y), new Vector3(_xBound, y), Color.red);
        }

        return;
        Color color = Color.yellow;
        foreach (var pair in _regionMap)
		{
            float intensity = pair.Value.Count / (float)_particlesCount;
            color.a = Mathf.Clamp01(intensity * 10);
            DebugFillSquare(pair.Key.Item1, pair.Key.Item2, _connectionDistance, color);
		}
    }

    private (int, int) GetSquare(Vector3 location)
	{
        int xp = Mathf.FloorToInt(location.x / _connectionDistance);
        int yp = Mathf.FloorToInt(location.y / _connectionDistance);
        return (xp, yp);
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
