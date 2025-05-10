using UnityEngine;
using ConfigSerialization;
using System;
using ConfigSerialization.Structuring;

public sealed class BoundsParticleEffectorProxy : BoundsParticleEffector, IParticleEffectorProxy
{
    public override string Name { get; set; } = "Bounds";

    [Core.Json.NoJsonSerialization]
    public IParticleEffector Effector { get; set; }

    public event Action<IParticleEffector> EffectorChanged;
    
    private BoundsShapes _boundsShape = BoundsShapes.Rectangle;

    [SetComponentProperty(typeof(UIArranger), nameof(UIArranger.SelectedConfigurationName), "Extended")]
    [ConfigMemberOrder(0)]
    [ConfigProperty]
    public BoundsShapes BoundsShape
    {
        get => _boundsShape;
        set { if (_boundsShape != value) { SetBoundsShape(value); BoundsShapeChanged?.Invoke(value); }; }
    }

    public event Action<BoundsShapes> BoundsShapeChanged;

    public BoundsParticleEffector BoundsEffector => (BoundsParticleEffector)Effector;

    private void SetBoundsShape(BoundsShapes value)
    {
        _boundsShape = value;

        Effector = _boundsShape switch {
            BoundsShapes.Rectangle => new RectangularBoundParticleEffector(),
            BoundsShapes.Ellipse => new EllipticalBoundParticleEffector(),
            _ => throw new ArgumentException("Unknown bounds shape"),
        };

        PropagateBoundMargins(BoundMargins);
        PropagateBoundsAspect(BoundsAspect);
        PropagateBounceType(BounceType);
        PropagateRestitution(Restitution);
        PropagateRandomFraction(RandomFraction);

        EffectorChanged?.Invoke(Effector);
    }

    public override void Init(ParticleController controller) => throw new InvalidOperationException();
    public override void Detach() => throw new InvalidOperationException();
    public override void AffectParticle(Particle p) => throw new InvalidOperationException();
    public override bool InBounds(Vector2 position) => throw new InvalidOperationException();
    public override Vector2 SamplePoint() => throw new InvalidOperationException();
    protected override void RecalculateBounds() { }

    public void InitProxy() {
        Initialized = true;
        BoundMarginsChanged += PropagateBoundMargins;
        BoundsAspectChanged += PropagateBoundsAspect;
        BounceTypeChanged += PropagateBounceType;
        RestitutionChanged += PropagateRestitution;
        RandomFractionChanged += PropagateRandomFraction;
        
        SetBoundsShape(_boundsShape);
    }

    private void PropagateBoundMargins(float margins) => BoundsEffector.BoundMargins = margins;
    private void PropagateBoundsAspect(float aspect) => BoundsEffector.BoundsAspect = aspect;
    private void PropagateBounceType(BoundsBounceType bounceType) => BoundsEffector.BounceType = bounceType;
    private void PropagateRestitution(float restitution) => BoundsEffector.Restitution = restitution;
    private void PropagateRandomFraction(float fraction) => BoundsEffector.RandomFraction = fraction;
}

public enum BoundsShapes
{
    Rectangle,
    Ellipse
}
