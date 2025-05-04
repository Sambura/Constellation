using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves particles according to their current velocity value.
/// Unused for now?
/// </summary>
public sealed class KineticParticleEffector : IParticleEffector
{
    private ParticleController _particleController;

    public int Priority { get; set; } = 100;
    public string Name { get; set; } = "Kinetic Effector";

    public void AffectParticle(Particle p) {
        p.Position += p.Velocity * Time.deltaTime;
    }

    public void Init(ParticleController controller)
    {
        _particleController = controller;
    }

    public void Detach() { }
}
