using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleController : MonoBehaviour
{
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
    private List<GameObject> _lines;

    private float GetParticleVelocity(Particle particle)
	{
        return Random.Range(_minParticleVelocity, _maxParticleVelocity);
	}

    private void Awake()
    {
        float xBound = 10;
        float yBound = 5;

        _particles = new List<Particle>(_particlesCount);
        _lines = new List<GameObject>();

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
    }

    private void Update()
    {
        foreach (GameObject line in _lines)
            Destroy(line);

        float sqrConnect = _connectionDistance * _connectionDistance;
        float sqrStrong = _strongDistance * _strongDistance;

        for (int i = 0; i < _particles.Count; i++)
		{
            for (int j = i + 1; j < _particles.Count; j++)
			{
                Particle p1 = _particles[i], p2 = _particles[j];
                float sqrDistance = Vector2.SqrMagnitude(p1.transform.position - p2.transform.position);
                if (sqrDistance > sqrConnect) continue;

                float intensity = 1 - (sqrDistance - sqrStrong) / (sqrConnect - sqrStrong);
                GameObject lineObject = Instantiate(_linePrefab, transform);
                LineRenderer line = lineObject.GetComponent<LineRenderer>();
                line.widthMultiplier = _linesWidth;
                Color color = _lineColor.Evaluate(intensity);
                Gradient gradient = new Gradient();
                gradient.SetKeys(new GradientColorKey[] { new GradientColorKey(color, 0) }, 
                                 new GradientAlphaKey[] { new GradientAlphaKey(1, 0) });
                line.colorGradient = gradient;
                line.SetPositions(new Vector3[] { p1.transform.position, p2.transform.position });

                _lines.Add(lineObject);
			}
		}
    }
}
