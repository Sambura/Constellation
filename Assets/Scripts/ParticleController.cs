using System.Collections.Generic;
using UnityEngine;
using Core;

public class ParticleController : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private Viewport _viewport;

    [Header("Prefabs")]
    [SerializeField] private GameObject _particlePrefab;

    [Header("Initialization parameters")]
    [SerializeField] private bool _randomizeInitialPosition = true;

    [Header("Particles parameters")]
    [SerializeField] private float _particlesScale = 0.1f;
    [SerializeField] private int _particlesCount = 100;
    [SerializeField] private Color _particlesColor = Color.white;
    [SerializeField] private bool _showParticles = true;
    [SerializeField] private float _minParticleVelocity = 0;
    [SerializeField] private float _maxParticleVelocity = 1;

    [ConfigProperty] 
    public bool ShowParticles
    {
        get => _showParticles;
        set { if (_showParticles != value) { SetShowParticles(value); ShowParticlesChanged?.Invoke(value); } }
    }
    [ConfigProperty] 
    public float ParticleSize
    {
        get => _particlesScale;
        set { if (_particlesScale != value) { SetParticleSize(value); ParticleSizeChanged?.Invoke(value); } }
    }
    [ConfigProperty]
    public Color ParticleColor
    {
        get => _particlesColor;
        set { if (_particlesColor != value) { SetParticleColor(value); ParticleColorChanged?.Invoke(value); } }
    }
    [ConfigProperty]
    public int ParticleCount
    {
        get => _particlesCount;
        set { if (_particlesCount != value) { SetParticlesCount(value); ParticleCountChanged?.Invoke(value); } }
    }
    [ConfigProperty]
    public float MinParticleVelocity
    {
        get => _minParticleVelocity;
        set { if (_minParticleVelocity != value) { SetMinParticleVelocity(value); MinParticleVelocityChanged?.Invoke(value); }; }
    }
    [ConfigProperty]
	public float MaxParticleVelocity
    {
        get => _maxParticleVelocity;
        set { if (_maxParticleVelocity != value) { SetMaxParticleVelocity(value); MaxParticleVelocityChanged?.Invoke(value); }; }
    }

    public event System.Action<bool> ShowParticlesChanged;
    public event System.Action<float> ParticleSizeChanged;
    public event System.Action<Color> ParticleColorChanged;
    public event System.Action<int> ParticleCountChanged;
    public event System.Action<float> MinParticleVelocityChanged;
    public event System.Action<float> MaxParticleVelocityChanged;

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
    private void SetMinParticleVelocity(float value)
    {
        _minParticleVelocity = value;

        foreach (Particle p in _particles)
        {
            float magnitude = p.Velocity.magnitude;
            if (magnitude >= _minParticleVelocity) continue;
            if (magnitude == 0)
            {
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

    private float GetParticleVelocity(Particle particle)
    {
        return Random.Range(_minParticleVelocity, _maxParticleVelocity);
    }

    public int CellCount => (_maxSquareX + 1) * (_maxSquareY + 1);
    public float AveragePerCell => (float)_particles.Count / CellCount;

    public List<Particle> Particles
	{
        get => _particles;
	}
    public FastList<Particle>[,] RegionMap => _regionMap;
    public int XSquareOffset => _xSquareOffset;
    public int YSquareOffset => _ySquareOffset;
    public int MaxSquareX => _maxSquareX;
    public int MaxSquareY => _maxSquareY;
    public float ConnectionDistance => _connectionDistance;
    public Viewport Viewport
    {
        get => _viewport;
        set
		{
            _viewport = value;
            _viewport.CameraDimensionsChanged += DoFragmentation;
            DoFragmentation();
        }
    }

    public void SetConnectionDistance(float value)
	{
        _connectionDistance = value;

        if (_particles != null) DoFragmentation();
    }

	private void Awake()
	{
        _particles = new List<Particle>(_particlesCount);

        SetParticlesCount(_particlesCount);
        DoFragmentation();
    }

	private void DoFragmentation()
	{
        if (_connectionDistance <= 0) return;
        int xSquareOffset = Mathf.FloorToInt(Viewport.MaxX / _connectionDistance) + 1;
        int ySquareOffset = Mathf.FloorToInt(Viewport.MaxY / _connectionDistance) + 1;

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

    private float _connectionDistance;
    private List<Particle> _particles;
    private FastList<Particle>[,] _regionMap;
    private int _xSquareOffset;
    private int _ySquareOffset;
    private int _maxSquareX;
    private int _maxSquareY;

	private void Update()
    {
        for (int ry = 0; ry <= _maxSquareY; ry++)
            for (int rx = 0; rx <= _maxSquareX; rx++)
                _regionMap[rx, ry].PseudoClear();

        for (int i = 0, count = _particles.Count; i < count; i++)
        {
            Particle p = _particles[i];
            GetSquare(p.Position, out int x, out int y);
            _regionMap[x, y].Add(p);
        }
    }

    private void GetSquare(Vector3 location, out int sqrX, out int sqrY)
    {
        int xp = (int)System.Math.Floor(location.x / _connectionDistance) + _xSquareOffset;
        int yp = (int)System.Math.Floor(location.y / _connectionDistance) + _ySquareOffset;
        sqrX = Mathf.Clamp(xp, 0, _maxSquareX);
        sqrY = Mathf.Clamp(yp, 0, _maxSquareY);
    }
}
