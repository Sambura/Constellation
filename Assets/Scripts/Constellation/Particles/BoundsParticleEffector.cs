using UnityEngine;
using System;
using ConfigSerialization.Structuring;
using ConfigSerialization;
using Core.Json;

/// <summary>
/// Abstract class for effectors that simulate a boundary that confines particles' movements to a certain area
/// </summary>
public abstract class BoundsParticleEffector : IParticleEffector
{
    protected ParticleController ParticleController;

    public virtual string Name { get; set; } = "Bounds";
    public bool Initialized { get; protected set; }
    public ControlType ControlType { get; } = ControlType.Both;
    public ControlType DefaultControlType { get; } = ControlType.Visualizers;
    public EffectorType EffectorType { get; } = EffectorType.PerParticle;

    protected double _boundMargins = 0;
    protected double _boundsAspect = 2;
    protected BoundsBounceType _bounceType = BoundsBounceType.RandomBounce;
    protected float _restitution = 1;
    protected float _randomFraction = 0.2f;
    protected bool _showBounds = false;
    protected Color _boundsColor = new Color(0, 0.4584198f, 1);

    protected float _horizontalBase;
    protected float _verticalBase;

    [SliderProperty(-5, 5, -30, 30, "0.0")]
    public float BoundMargins
    {
        get => (float)_boundMargins;
        set { if ((float)_boundMargins != value && !float.IsNaN(value)) { _boundMargins = value; RecalculateBounds(); BoundMarginsChanged?.Invoke(value); }; }
    }
    [SliderProperty(0, 3, 0, 100)]
    public float BoundsAspect
    {
        get => (float)_boundsAspect;
        set { if ((float)_boundsAspect != value && !float.IsNaN(value)) { _boundsAspect = value; RecalculateBounds(); BoundsAspectChanged?.Invoke(value); }; }
    }
    [ConfigGroupToggle(null, 1, new object[] { 1, 2 }, null, DoNotReorder = true)]
    [SetComponentProperty(typeof(UIArranger), nameof(UIArranger.SelectedConfigurationName), "Extended")]
    [ConfigProperty]
    public BoundsBounceType BounceType
    {
        get => _bounceType;
        set { if (_bounceType != value) { _bounceType = value; BounceTypeChanged?.Invoke(value); }; }
    }
    [ConfigGroupMember(groupIndex: 1)]
    [SliderProperty(0, 2)]
    public float Restitution
    {
        get => _restitution;
        set { if (_restitution != value) { _restitution = value; RestitutionChanged?.Invoke(value); }; }
    }
    [ConfigGroupMember(groupIndex: 2)]
    [SliderProperty(0, 1)]
    public float RandomFraction
    {
        get => _randomFraction;
        set { if (_randomFraction != value) { _randomFraction = value; RandomFractionChanged?.Invoke(value); }; }
    }

    // VISUAL config
    [ConfigGroupMember("Visual config", SetIndent = false)]
    [ConfigProperty]
    public bool ShowBounds
    {
        get => _showBounds;
        set { if (_showBounds != value) { _showBounds = value; ShowBoundsChanged?.Invoke(value); }; }
    }
    [ConfigGroupMember]
    [ConfigProperty]
    public Color BoundsColor
    {
        get => _boundsColor;
        set { if (_boundsColor != value) { _boundsColor = value; BoundsColorChanged?.Invoke(value); }; }
    }

    [NoJsonSerialization] public bool ForceShowBounds { get; set; } = false;
    public float HorizontalBase => _horizontalBase;
    public float VerticalBase => _verticalBase;

    public event Action<float> BoundMarginsChanged;
    public event Action<float> BoundsAspectChanged;
    public event Action<BoundsBounceType> BounceTypeChanged;
    public event Action<float> RestitutionChanged;
    public event Action<float> RandomFractionChanged;
    public event Action<bool> ShowBoundsChanged;
    public event Action<Color> BoundsColorChanged;
    public event Action BoundsChanged;

    // We operate in double here to enable BoundsFromHalfSize function to perform lossless (lossless-er?) conversion
    // which is particularly important for current GraphicControls implementation
    protected double BoundMarginsD {
        get => _boundMargins;
        set { if (_boundMargins != value && !double.IsNaN(value)) { _boundMargins = value; RecalculateBounds(); BoundMarginsChanged?.Invoke(BoundMargins); }; }
    }
    protected double BoundsAspectD {
        get => _boundsAspect;
        set { if (_boundsAspect != value && !double.IsNaN(value)) { _boundsAspect = value; RecalculateBounds(); BoundsAspectChanged?.Invoke(BoundsAspect); }; }
    }

    protected virtual void RecalculateBounds()
    {
        if (!Initialized) return;

        Viewport viewport = ParticleController.Viewport;
        _verticalBase = (float)(viewport.MaxY - _boundMargins);
        if (_boundsAspect < 1)
            _horizontalBase = (float)((viewport.MaxY - _boundMargins) * _boundsAspect);
        else
            _horizontalBase = (float)(viewport.MaxY * Core.MathUtility.LerpUnclamped(1, viewport.Aspect, _boundsAspect - 1) - _boundMargins);
        _horizontalBase = Mathf.Max(_horizontalBase, 0);
        BoundsChanged?.Invoke();
    }

    protected virtual void BoundsFromHalfSize(Vector2 halfSize)
    {
        if (halfSize.x < 0) halfSize.x = 0;
        if (halfSize.y < 0) halfSize.y = 0;

        Viewport viewport = ParticleController.Viewport;
        double boundMargins = (double)viewport.MaxY - halfSize.y; 

        bool viewportAspect = halfSize.x > halfSize.y;
        double aspect = (double)halfSize.x / (viewport.MaxY - boundMargins);
        if (viewportAspect) {
            double Y = viewport.MaxY, A = viewport.Aspect;
            aspect = (halfSize.x + boundMargins - 2 * Y + Y * A) / (Y * A - Y);
        }

        BoundsAspectD = Math.Max(aspect, 0);
        BoundMarginsD = boundMargins;
    }

    public abstract void AffectParticle(Particle p);

    public virtual void Init(ParticleController controller) {
        ParticleController = controller;
        ParticleController.Viewport.CameraDimensionsChanged += RecalculateBounds;
        Initialized = true;
        RecalculateBounds();
    }

    public virtual void Detach() {
        if (!Initialized) return;
        ParticleController.Viewport.CameraDimensionsChanged -= RecalculateBounds;
        Initialized = false;
    }

    public abstract bool InBounds(Vector2 position);

    public virtual Vector2 SamplePoint() {
        return new Vector2(UnityEngine.Random.Range(-_horizontalBase, _horizontalBase), 
                           UnityEngine.Random.Range(-_verticalBase, _verticalBase));
    }

    public abstract void RenderControls(ControlType controlTypes);
}

public enum BoundsBounceType
{
    RandomBounce,
    ElasticBounce,
    HybridBounce,
    Wrap
}
