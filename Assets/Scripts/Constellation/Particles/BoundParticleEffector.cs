using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abstract class for effectors that simulate a boundary that confines particles' movements to a certain area
/// </summary>
public abstract class BoundParticleEffector : IParticleEffector
{
    protected ParticleController ParticleController;

    public int Priority { get; set; } = 1000;
    public virtual string Name { get; set; } = "Bounds Effector";

    protected float HorizontalBase;
    protected float VerticalBase;
    protected ParticleController.BoundsBounceType BounceType;
    protected float Restitution;
    protected float RandomFraction;

    public abstract void AffectParticle(Particle p);

    public virtual void Init(ParticleController controller) {
        ParticleController = controller;
        ParticleController.BounceTypeChanged += BounceTypeProxy;
        ParticleController.RestitutionChanged += FloatProxy;
        ParticleController.RandomFractionChanged += FloatProxy;
        ParticleController.BoundsChanged += OnBoundsUpdated;
        OnBoundsUpdated();
    }

    public virtual void Detach() {
        ParticleController.BounceTypeChanged -= BounceTypeProxy;
        ParticleController.RestitutionChanged -= FloatProxy;
        ParticleController.RandomFractionChanged -= FloatProxy;
        ParticleController.BoundsChanged -= OnBoundsUpdated;
    }

    public abstract bool InBounds(Vector2 position);

    public virtual Vector2 SamplePoint() {
        return new Vector2(Random.Range(-HorizontalBase, HorizontalBase), Random.Range(-VerticalBase, VerticalBase));
    }

    private void FloatProxy(float value) => OnBoundsUpdated();
    private void BounceTypeProxy(ParticleController.BoundsBounceType value) => OnBoundsUpdated();

    protected virtual void OnBoundsUpdated()
    {
        HorizontalBase = ParticleController.HorizontalBase;
        VerticalBase = ParticleController.VerticalBase;
        BounceType = ParticleController.BounceType;
        Restitution = ParticleController.Restitution;
        RandomFraction = ParticleController.RandomFraction;
    }
}
