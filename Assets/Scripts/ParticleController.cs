using System.Collections.Generic;
using UnityEngine;
using Core;
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
    [SerializeField] private float _boundsAspect = 2;
    [SerializeField] private BoundsBounceType _bounceType = BoundsBounceType.RandomBounce;
    [SerializeField] private float _restitution = 1;
    [SerializeField] private float _randomFraction = 0.2f;

    // fragmentation
    private float _fragmentSize;
    private List<Particle> _particles;
    private FastList<Particle>[,] _regionMap;
    private int _xSquareOffset;
    private int _ySquareOffset;
    private int _maxSquareX;
    private int _maxSquareY;
    // effectors
    private List<EffectorModule> _rawParticleEffectors = new();
    // subset of raw particle effectors - only those that are enabled
    private readonly List<IParticleEffector> _activeParticleEffectors = new();
    // subset of active particle effectors
    private readonly List<BoundsParticleEffector> _boundsEffectors = new();
    // fallback bounds for sampling point locations (for RestartSimulation)
    private readonly RectangularBoundParticleEffector _defaultBounds = new();
    private int _activeEffectorsCount;
    private readonly List<(IParticleEffector effector, ControlType flags)> _drawableEffectors = new();

    public Dictionary<string, object> EffectorTypes { get; set; } = new() {
        { "Kinematic Effector", typeof(KinematicParticleEffector) },
        { "Bounds", typeof(BoundsParticleEffectorProxy) },
        { "Friction Effector", typeof(FrictionParticleEffector) },
        { "Attractor/Repeller", typeof(AttractionParticleEffector) },
        { "Field Analyzer", typeof(FieldAnalyzerEffector) },
    };

    #region Config properties

    [ConfigGroupMember("Simulation parameters", GroupId = "PC+sim_params")]
    [SliderProperty(0, 15000, 0, 100000000, name: "Particles count")] public int ParticleCount
    {
        get => _particlesCount;
        set { if (_particlesCount != value) { SetParticlesCount(value); ParticleCountChanged?.Invoke(value); } }
    }
    [ConfigGroupMember]
    [ListViewProperty(sourcePropertyName: nameof(EffectorTypes))]
    public List<EffectorModule> ParticleEffectors
    {
        get => new(_rawParticleEffectors);
        set
        {
            SetParticleEffectors(new List<EffectorModule>(value));
            ParticleEffectorsChanged?.Invoke(ParticleEffectors);
        }
    }
    [ConfigGroupMember]
    [MinMaxSliderProperty(0, 5, 0, 100, higherPropertyName: nameof(MaxParticleVelocity), name: "Initial velocity")]
    public float MinParticleVelocity
    {
        get => _minParticleVelocity;
        set { if (_minParticleVelocity != value) { _minParticleVelocity = value; MinParticleVelocityChanged?.Invoke(value); }; }
    }
    [ConfigGroupMember]
    [MinMaxSliderProperty] public float MaxParticleVelocity
    {
        get => _maxParticleVelocity;
        set { if (_maxParticleVelocity != value) { _maxParticleVelocity = value; MaxParticleVelocityChanged?.Invoke(value); }; }
    }

    public event System.Action<int> ParticleCountChanged;
    public event System.Action<float> MinParticleVelocityChanged;
    public event System.Action<float> MaxParticleVelocityChanged;
    public event System.Action<List<EffectorModule>> ParticleEffectorsChanged;

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
    private void SetParticleEffectors(List<EffectorModule> effectors) {
        _rawParticleEffectors = effectors;

        // Add locked kinematic effector if it's not there
        if (effectors.Count < 1 || effectors[0].Effector is not KinematicParticleEffector)
            _rawParticleEffectors.Insert(0, new EffectorModule(typeof(KinematicParticleEffector)));

        effectors[0].Enabled = true;
        effectors[0].Locked = true;
        effectors[0].Controller = this;
        (effectors[0].Effector as KinematicParticleEffector).VelocityFactor = 1;
        (effectors[0].Effector as KinematicParticleEffector).VelocityLimit = null;

        // Detach active effectors that disappeared
        foreach (IParticleEffector effector in _activeParticleEffectors) {
            var module = _rawParticleEffectors.Find(x => x.Effector == effector);

            if (module is null || !module.Enabled)
                effector.Detach();
        }

        // Attach new effectors and rebuild active list
        _activeParticleEffectors.Clear();
        _boundsEffectors.Clear();
        _drawableEffectors.Clear();
        foreach (EffectorModule module in effectors.GetRange(1, effectors.Count - 1)) {
            module.Controller = this;
            if (!module.Enabled) continue;

            IParticleEffector effector = module.Effector;
            if (effector.EffectorType.HasFlag(EffectorType.PerParticle)) {
                _activeParticleEffectors.Add(effector);
                if (effector is BoundsParticleEffector bounds)
                    _boundsEffectors.Add(bounds);
            }

            if (!effector.Initialized)
                effector.Init(this);

            ControlType flags = ControlType.None;
            for (int i = 0; i < module.QuickToggleStates.Count; i++) {
                if (module.QuickToggleStates[i] <= 0) continue;
                flags |= (ControlType)module.GetQuickToggles()[i].data;
            }
            if (flags != ControlType.None) 
                _drawableEffectors.Add((effector, flags));
        }

        _activeEffectorsCount = _activeParticleEffectors.Count;
    }

    #endregion

    public event System.Action<Particle> ParticleCreated;

    [SetComponentProperty(typeof(UIArranger), nameof(UIArranger.SelectedConfigurationName), "Middle")]
    [ConfigGroupMember] [ConfigMemberOrder(-1)]
    [InvokableMethod]
    public void RestartSimulation()
    {
        foreach (Particle particle in _particles)
            SetRandomPositionAndVelocity(particle);
    }

    private void SetRandomPositionAndVelocity(Particle particle)
    {
        particle.SetRandomVelocity();

        BoundsParticleEffector baseBounds = _boundsEffectors.Count > 0 ? _boundsEffectors[0] : _defaultBounds;
        for (int attempts = 0; attempts < 25; attempts++) {
            bool valid = true;
            particle.Position = baseBounds.SamplePoint();

            foreach (BoundsParticleEffector bounds in _boundsEffectors) {
                if (bounds.InBounds(particle.Position)) continue;
                valid = false;
                break;
            }

            if (valid) break;
        }
    }

    public float GetParticleVelocity(Particle particle)
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
    public float FragmentSize => _fragmentSize;
    public Viewport Viewport
    {
        get => _viewport;
        set { if (_viewport != value) SetViewport(value); }
    }
    public List<IParticleEffector> ActiveEffectors => _activeParticleEffectors;
    public List<BoundsParticleEffector> ActiveBoundsEffectors => _boundsEffectors;

    public event System.Action<float> FragmentSizeChanged;

    public void SetFragmentSize(float value)
    {
        _fragmentSize = value;

        if (_particles != null) DoFragmentation();
        FragmentSizeChanged?.Invoke(_fragmentSize);
    }

    private void SetViewport(Viewport viewport)
    {
        if (_viewport) 
            _viewport.CameraDimensionsChanged -= OnViewportChanged;

        _viewport = viewport;
        _viewport.CameraDimensionsChanged += OnViewportChanged;
        OnViewportChanged();
    }

    private void Awake()
    {
        _particles = new List<Particle>(_particlesCount);

        SetViewport(_viewport);
        SetParticleEffectors(new List<EffectorModule>() {
            new EffectorModule(typeof(BoundsParticleEffectorProxy))
        });

        var bounds = _boundsEffectors[0];
        bounds.BoundsAspect = _boundsAspect;
        bounds.BoundMargins = _boundMargins;
        bounds.BounceType = _bounceType;
        bounds.Restitution = _restitution;
        bounds.RandomFraction = _randomFraction;
        SetParticlesCount(_particlesCount);

        _defaultBounds.Init(this);
    }

    private void OnViewportChanged() => DoFragmentation();

    private void DoFragmentation()
    {
        if (_fragmentSize <= 0) return;
        _fragmentSize = Mathf.Max(_fragmentSize, 0.02f); // TODO: fix
        int xSquareOffset = Mathf.FloorToInt(Viewport.MaxX / _fragmentSize) + 1;
        int ySquareOffset = Mathf.FloorToInt(Viewport.MaxY / _fragmentSize) + 1;

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

    private void Update()
    {
        for (int ry = 0; ry <= _maxSquareY; ry++)
            for (int rx = 0; rx <= _maxSquareX; rx++)
                _regionMap[rx, ry].PseudoClear();

        foreach ((var effector, var flags) in _drawableEffectors)
            effector.RenderControls(flags);
        
        for (int i = 0, count = _particles.Count; i < count; i++)
        {
            Particle particle = _particles[i];
            particle.Position += particle.Velocity * Time.deltaTime;
            
            for (int j = 0; j < _activeEffectorsCount; j++)
                _activeParticleEffectors[j].AffectParticle(particle);

            GetSquare(particle.Position, out int x, out int y);
            _regionMap[x, y].Add(particle);
        }
    }

    public Vector2 GetFragmentLocation(int xIndex, int yIndex)
    {
        float x = (xIndex - _xSquareOffset + 0.5f) * _fragmentSize;
        float y = (yIndex - _ySquareOffset + 0.5f) * _fragmentSize;
        
        return new Vector2(x, y);
    }

    public void GetSquare(Vector3 location, out int sqrX, out int sqrY)
    {
        int xp = (int)System.Math.Floor(location.x / _fragmentSize) + _xSquareOffset;
        int yp = (int)System.Math.Floor(location.y / _fragmentSize) + _ySquareOffset;
        sqrX = Mathf.Clamp(xp, 0, _maxSquareX);
        sqrY = Mathf.Clamp(yp, 0, _maxSquareY);
    }
}
