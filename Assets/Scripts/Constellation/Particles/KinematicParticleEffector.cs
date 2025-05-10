using ConfigSerialization;
using UnityEngine;

/// <summary>
/// Moves particles according to their current velocity value.
/// Provides velocity scaling and limiting
/// </summary>
public sealed class KinematicParticleEffector : IParticleEffector
{
    private ParticleController _particleController;
    private float _velocityFactor = 0;
    private float? _velocityLimit = null;
    private float _velocityLimitSqr = float.PositiveInfinity;

    [SliderProperty(-2, 2, hasEvent: false, AllowPolling = true)] 
    public float VelocityFactor { get => _velocityFactor; set => _velocityFactor = value; }
    [SliderProperty(0, 50, 0, hasEvent: false, AllowPolling = true, Name = "Limit Velocity")]
    public float? VelocityLimit { get => _velocityLimit; set { _velocityLimit = value; _velocityLimitSqr = value.HasValue ? value.Value * value.Value : float.PositiveInfinity; } }

    public string Name { get; set; } = "Kinematic Effector";
    public bool Initialized { get; private set; }

    public void AffectParticle(Particle p) {
        if (_velocityLimit.HasValue) {
            float vSqr = p.Velocity.sqrMagnitude;

            if (vSqr > _velocityLimitSqr) {
                p.Velocity = _velocityLimit.Value / System.MathF.Sqrt(vSqr) * p.Velocity;
            }
        }

        p.Position += _velocityFactor * Time.deltaTime * p.Velocity;
    }

    public void Init(ParticleController controller)
    {
        _particleController = controller;
        Initialized = true;
    }

    public void Detach() { Initialized = false; }
}
