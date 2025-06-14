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

        // We assume only these can change from inside the proxied effector
        if (Effector is not null) {
            BoundsEffector.BoundMarginsChanged -= PropagateBoundMargins;
            BoundsEffector.BoundsAspectChanged -= PropagateBoundsAspect;
        }

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
        PropagateShowBounds(ShowBounds);
        PropagateBoundsColor(BoundsColor);
        BoundsEffector.BoundMarginsChanged += PropagateBoundMargins;
        BoundsEffector.BoundsAspectChanged += PropagateBoundsAspect;

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
        ShowBoundsChanged += PropagateShowBounds;
        BoundsColorChanged += PropagateBoundsColor;
        
        SetBoundsShape(_boundsShape);
    }

    private void PropagateBoundMargins(float margins) { BoundsEffector.BoundMargins = margins; BoundMargins = margins; }
    private void PropagateBoundsAspect(float aspect) { BoundsEffector.BoundsAspect = aspect; BoundsAspect = aspect; }
    private void PropagateBounceType(BoundsBounceType bounceType) => BoundsEffector.BounceType = bounceType;
    private void PropagateRestitution(float restitution) => BoundsEffector.Restitution = restitution;
    private void PropagateRandomFraction(float fraction) => BoundsEffector.RandomFraction = fraction;
    private void PropagateShowBounds(bool showBounds) => BoundsEffector.ShowBounds = showBounds;
    private void PropagateBoundsColor(Color boundsColor) => BoundsEffector.BoundsColor = boundsColor;

    public override void RenderControls(ControlType controlTypes) => throw new InvalidOperationException();
}

public enum BoundsShapes
{
    Rectangle,
    Ellipse
}
