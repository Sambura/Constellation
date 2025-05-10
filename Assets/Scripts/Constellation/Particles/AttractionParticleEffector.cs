using UnityEngine;
using ConfigSerialization;
using ConfigSerialization.Structuring;

public sealed class AttractionParticleEffector : IParticleEffector
{
    private ParticleController _particleController;
    private bool _useRadius = true;
    private Vector2 _position = Vector2.zero;
    private float _exponentMinusOne = -1;

    [SliderProperty(-10, 10, hasEvent: false, AllowPolling = true)]
    public float Strength { get; set; } = 1;
    [SliderProperty(-10, 10, hasEvent: false, AllowPolling = true)]
    public float LocationX { get => _position.x; set => _position.x = value; }
    [SliderProperty(-10, 10, hasEvent: false, AllowPolling = true)]
    public float LocationY { get => _position.y; set => _position.y = value; }
    [ConfigProperty(name: "Radius")]
    [ConfigGroupToggle(1, 2)]
    public bool UseRadius { get => _useRadius; set { if (_useRadius != value) { _useRadius = value; UseRadiusChanged?.Invoke(value); } } }
    [ConfigGroupMember(1)]
    [SliderProperty(0, 15, 0, name: "Value", hasEvent: false, AllowPolling = true)]
    public float Radius { get; set; } = 5;
    [ConfigGroupMember(1)]
    [ConfigProperty(name: "Intensity", hasEvent: false, AllowPolling = true)]
    public AnimationCurve IntensityCurve { get; set; } = new AnimationCurve(new Keyframe(1, 1), new Keyframe(0, 0));
    [ConfigGroupMember(2)]
    [SliderProperty(-2, 2, name: "Intensity exponent", hasEvent: false, AllowPolling = true)]
    public float Exponent { get => _exponentMinusOne + 1; set => _exponentMinusOne = value - 1; }

    public event System.Action<bool> UseRadiusChanged;

    public string Name { get; set; } = "Attractor";
    public bool Initialized { get; private set; }

    public void AffectParticle(Particle p) {
        Vector2 toParticle = (Vector2)p.Position - _position;
        float distance = toParticle.magnitude;
        Vector2 acceleration;

        if (_useRadius) {
            if (distance > Radius) return;
            
            acceleration = -IntensityCurve.Evaluate(distance / Radius) * Strength / distance * toParticle;
            p.Velocity += (Vector3)acceleration * Time.deltaTime;
            return;
        }

        // _exponentMinusOne is responsible for both raising to intensity exponent and dividing vector by its magnitude
        acceleration = -Mathf.Pow(distance, _exponentMinusOne) * Strength * toParticle;
        p.Velocity += (Vector3)(acceleration * Time.deltaTime);
    }

    public void Init(ParticleController controller)
    {
        _particleController = controller;
        Initialized = true;
    }

    public void Detach() { Initialized = false; }
}
