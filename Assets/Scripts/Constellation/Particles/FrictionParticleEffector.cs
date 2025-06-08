using UnityEngine;
using ConfigSerialization;
using ConfigSerialization.Structuring;

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
    [ConfigGroupMember("Visual config", SetIndent = false)]
    [SliderProperty(0, 2, 0, hasEvent: false, AllowPolling = true)]
    public float ArrowScale { get; set; } = 1;
    [ConfigGroupMember]
    [ConfigProperty(hasEvent: false, AllowPolling = true)]
    public Color ArrowColor { get; set; } = Color.red;

    public string Name { get; set; } = "Friction Effector";
    public bool Initialized { get; private set; }
    public ControlType ControlType { get; } = ControlType.Visualizers;
    public ControlType DefaultControlType { get; } = ControlType.None;
    public EffectorType EffectorType { get; } = EffectorType.PerParticle;

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

    public void RenderControls(ControlType controlTypes) {
        if (!controlTypes.HasFlag(ControlType.Visualizers)) return;

        for (int i = 0; i < _particleController.ParticleCount; i++) {
            Particle p = _particleController.Particles[i];
            Vector2 velocity = p.Velocity;
            float magnitude = velocity.magnitude;
            float accelerationMagnitude = Mathf.Clamp(Coefficient + magnitude * QuadraticCoefficient, -magnitude, magnitude);
            Vector2 acceleration = -3 * ArrowScale * accelerationMagnitude * velocity;
            GraphicControls.Arrow(p.Position, acceleration, ArrowColor, interactable: false);
        }
    }
}
