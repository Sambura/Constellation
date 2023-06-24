using System.Collections.Generic;
using UnityEngine;
using Core;
using static Core.MathUtility;
using ConfigSerialization;
using ConfigSerialization.Structuring;

public class ParticleController : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private Viewport _viewport;

    [Header("Particles simulation parameters")]
    [SerializeField] private int _particlesCount = 100;
    [SerializeField] private float _minParticleVelocity = 0;
    [SerializeField] private float _maxParticleVelocity = 1;
    [SerializeField] private float _boundMargins = 0;
    //[SerializeField] private bool _warp = true;

    public event System.Action<Particle> ParticleCreated;

	#region Config properties

	[ConfigGroupMember("Simulation parameters", GroupId = "PC+sim_params")]
	[SliderProperty(0, 2500, 0, 100000, name: "Particles count")] public int ParticleCount
    {
        get => _particlesCount;
        set { if (_particlesCount != value) { SetParticlesCount(value); ParticleCountChanged?.Invoke(value); } }
    }
    [ConfigGroupMember]
    [MinMaxSliderProperty(0, 5, 0, 100, "0.00", higherPropertyName: nameof(MaxParticleVelocity), name: "Particle velocity")]
    public float MinParticleVelocity
    {
        get => _minParticleVelocity;
        set { if (_minParticleVelocity != value) { SetMinParticleVelocity(value); MinParticleVelocityChanged?.Invoke(value); }; }
    }
    [ConfigGroupMember]
    [MinMaxSliderProperty] public float MaxParticleVelocity
    {
        get => _maxParticleVelocity;
        set { if (_maxParticleVelocity != value) { SetMaxParticleVelocity(value); MaxParticleVelocityChanged?.Invoke(value); }; }
    }
    [ConfigGroupMember]
    [SliderProperty(-5, 5, -30, 30, "0.0")] public float BoundMargins
    {
        get => _boundMargins;
        set { if (_boundMargins != value) { SetBoundMargins(value); BoundMarginsChanged?.Invoke(value); }; }
    }

    public event System.Action<int> ParticleCountChanged;
    public event System.Action<float> MinParticleVelocityChanged;
    public event System.Action<float> MaxParticleVelocityChanged;
    public event System.Action<float> BoundMarginsChanged;

    private void SetParticlesCount(int value)
    {
        _particlesCount = value;

        while (_particles.Count > value)
        {
            int index = _particles.Count - 1;
            _particles.RemoveAt(index);
        }

        while (_particles.Count < value)
        {
			Particle particle = new Particle
			{
				VelocityDelegate = GetParticleVelocity
			};
			SetRandomPositionAndVelocity(particle);

            _particles.Add(particle);

            ParticleCreated?.Invoke(particle);
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
    private void SetBoundMargins(float value)
    {
        _boundMargins = value;
        RecalculateBounds();
    }
	#endregion

	[SetComponentProperty(typeof(UIArranger), nameof(UIArranger.SelectedConfigurationName), "Middle")]
    [ConfigGroupMember] [ConfigMemberOrder(-1)]
    [InvokableMethod("Restart simulation")]
    public void ReinitializeParticles()
    {
        foreach (Particle particle in _particles)
            SetRandomPositionAndVelocity(particle);
    }

    private void SetRandomPositionAndVelocity(Particle particle)
    {
        particle.SetRandomVelocity();

        particle.Position = new Vector3(Random.Range(_left, _right),
            Random.Range(_bottom, _top));

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
            _viewport.CameraDimensionsChanged += OnViewportChanged;
            OnViewportChanged();
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

        RecalculateBounds();
        SetParticlesCount(_particlesCount);
        DoFragmentation();
    }

    private void OnViewportChanged()
	{
		RecalculateBounds();
		DoFragmentation();
	}

	private void RecalculateBounds()
	{
		_left = -_viewport.MaxX + _boundMargins;
		_right = _viewport.MaxX - _boundMargins;
		_bottom = -_viewport.MaxY + _boundMargins;
		_top = _viewport.MaxY - _boundMargins;
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
    private float _left;
    private float _right;
    private float _top;
    private float _bottom;

    private void Update()
    {
        foreach (var particle in _particles)
		{
            particle.Position += particle.Velocity * Time.deltaTime;

            bool leftHit = particle.Position.x < _left;
            bool rightHit = particle.Position.x > _right;
            bool bottomHit = particle.Position.y < _bottom;
            bool topHit = particle.Position.y > _top;

            //if (Warp)
            //{
            //    if (leftHit || rightHit && _position.x * _velocity.x > 0) _position.x = -_position.x;
            //    if (bottomHit || topHit && _position.y * _velocity.y > 0) _position.y = -_position.y;
            //}
            //else
            //{
            if ((leftHit || rightHit) && particle.Position.x * particle.Velocity.x > 0)
            {
                particle.SetRandomVelocity(leftHit ? -Angle90 : Angle90, leftHit ? Angle90 : Angle270);
            }
            if ((bottomHit || topHit) && particle.Position.y * particle.Velocity.y > 0)
            {
                particle.SetRandomVelocity(bottomHit ? Angle0 : Angle180, bottomHit ? Angle180 : Angle360);
            }
            //}
        }
        
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
