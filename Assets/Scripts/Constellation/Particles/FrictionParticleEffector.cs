using UnityEngine;
using ConfigSerialization;

/// <summary>
/// Constant deceleration effector
/// </summary>
public sealed class FrictionParticleEffector : IParticleEffector
{
    private ParticleController _particleController;

    [SliderProperty(-1, 5, name: "Linear", hasEvent: false, AllowPolling = true)] 
    public float Coefficient { get; set; } = 0.01f;
    [SliderProperty(-1, 5, name: "Quadratic", hasEvent: false, AllowPolling = true)] 
    public float QuadraticCoefficient { get; set; } = 0.05f;

    public string Name { get; set; } = "Friction Effector";
    public bool Initialized { get; private set; }

    public void AffectParticle(Particle p) {
        Vector2 velocity = p.Velocity;
        float magnitude = velocity.magnitude;
        float accelerationMagnitude = Mathf.Clamp(Time.deltaTime * (Coefficient + magnitude * QuadraticCoefficient), -magnitude, magnitude);
        Vector2 acceleration = accelerationMagnitude * velocity;

        p.Velocity -= (Vector3)acceleration;
    }

    public void Init(ParticleController controller)
    {
        _particleController = controller;
        Initialized = true;
    }

    public void Detach() { Initialized = false; }
}
