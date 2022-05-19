using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private float GetParticleVelocity(Particle particle)
    {
        return Random.Range(_minParticleVelocity, _maxParticleVelocity);
    }

    private void Awake()
    {
        float yBound = _mainCamera.orthographicSize;
        float xBound = _mainCamera.aspect * _mainCamera.orthographicSize;

        _particles = new List<Particle>(_particlesCount);
        _lines = new List<Line>();

        for (int i = 0; i < _particlesCount; i++)
        {
            Particle particle = Instantiate(_particlePrefab, transform).GetComponent<Particle>();
            particle.VelocityDelegate = GetParticleVelocity;
            particle.XBound = xBound;
            particle.YBound = yBound;
            particle.Visible = _showParticles;
            particle.Color = _particlesColor;
            particle.transform.localScale = Vector3.one * _particlesScale;
            if (_randomizeInitialPosition)
            {
                particle.transform.position = new Vector3(Random.Range(-xBound, xBound), Random.Range(-yBound, yBound));
            }

            _particles.Add(particle);
        }

        _sqrConnect = _connectionDistance * _connectionDistance;
        _sqrStrong = _strongDistance * _strongDistance;
    }

    private void Update()
    {
        if (_showLines == false) return;

        int lineIndex = 0;
        for (int i = 0; i < _particles.Count; i++)
        {
            for (int j = i + 1; j < _particles.Count; j++)
            {
                Particle p1 = _particles[i], p2 = _particles[j];
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

        for (; lineIndex < _lines.Count; lineIndex++)
		{
            _lines[lineIndex].Enabled = false;
		}
    }
}
