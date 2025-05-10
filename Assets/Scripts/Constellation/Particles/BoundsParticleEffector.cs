using UnityEngine;
using System;
using ConfigSerialization.Structuring;
using ConfigSerialization;

/// <summary>
/// Abstract class for effectors that simulate a boundary that confines particles' movements to a certain area
/// </summary>
public abstract class BoundsParticleEffector : IParticleEffector
{
    protected ParticleController ParticleController;

    public virtual string Name { get; set; } = "Bounds";
    public bool Initialized { get; protected set; }

    protected float _boundMargins = 0;
    protected float _boundsAspect = 2;
    protected BoundsBounceType _bounceType = BoundsBounceType.RandomBounce;
    protected float _restitution = 1;
    protected float _randomFraction = 0.2f;
    protected float _horizontalBase;
    protected float _verticalBaes;

    [SliderProperty(-5, 5, -30, 30, "0.0")]
    public float BoundMargins
    {
        get => _boundMargins;
        set { if (_boundMargins != value) { _boundMargins = value; RecalculateBounds(); BoundMarginsChanged?.Invoke(value); }; }
    }
    [SliderProperty(0, 3, 0, 100)]
    public float BoundsAspect
    {
        get => _boundsAspect;
        set { if (_boundsAspect != value) { _boundsAspect = value; RecalculateBounds(); BoundsAspectChanged?.Invoke(value); }; }
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

    public float HorizontalBase => _horizontalBase;
    public float VerticalBase => _verticalBaes;

    public event Action<float> BoundMarginsChanged;
    public event Action<float> BoundsAspectChanged;
    public event Action<BoundsBounceType> BounceTypeChanged;
    public event Action<float> RestitutionChanged;
    public event Action<float> RandomFractionChanged;
    public event Action BoundsChanged;

    protected virtual void RecalculateBounds()
    {
        if (!Initialized) return;

        Viewport viewport = ParticleController.Viewport;
        _verticalBaes = viewport.MaxY - _boundMargins;
        if (BoundsAspect < 1)
            _horizontalBase = (viewport.MaxY - _boundMargins) * BoundsAspect;
        else
            _horizontalBase = viewport.MaxY * Mathf.LerpUnclamped(1, viewport.Aspect, BoundsAspect - 1) - _boundMargins;
        _horizontalBase = Mathf.Max(_horizontalBase, 0);
        BoundsChanged?.Invoke();
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
                           UnityEngine.Random.Range(-_verticalBaes, _verticalBaes));
    }
}

public enum BoundsBounceType
{
    RandomBounce,
    ElasticBounce,
    HybridBounce,
    Wrap
}
