using UnityEngine;
using System.Collections.Generic;
using ConfigSerialization;
using ConfigSerialization.Structuring;

public sealed class AttractionParticleEffector : IParticleEffector
{
    private ParticleController _particleController;
    private bool _useRadius = true;
    private Vector2 _position = Vector2.zero;
    private float _exponentMinusOne = -1;
    private List<float> _intensityMap = new();

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
    public ControlType ControlType { get; } = ControlType.Both;
    public ControlType DefaultControlType { get; } = ControlType.Interactable;
    public EffectorType EffectorType { get; } = EffectorType.PerParticle;

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

    public void RenderControls(ControlType controlTypes)
    {
        if (_useRadius && controlTypes != ControlType.None)
            Radius = GraphicControls.CircleRadius(_position, Radius, Color.yellow, interactable: controlTypes.HasFlag(ControlType.Interactable));

        if (controlTypes.HasFlag(ControlType.Interactable)) {
            _position = GraphicControls.TranslatePosition(_position);
        }

        // needlessly complicated code just to visualize pull intensity vs. distance
        if (!controlTypes.HasFlag(ControlType.Visualizers)) return;
        const int maxInterRings = 40;
        const int sampleCount = 1000;
        Color interColor = new Color(1, 0, 1, 0.6f);
        float maxRadius = UseRadius ? Radius : _particleController.Viewport.GetRadius(_position);
        int interRings = Mathf.CeilToInt(maxInterRings * maxRadius / _particleController.Viewport.Radius);
        float sampleStep = maxRadius / (sampleCount + 1);
        _intensityMap.Clear();

        float totalIntensity = 0;
        for (float radius = sampleStep; radius < maxRadius; radius += sampleStep) {
            float intensity = UseRadius ? IntensityCurve.Evaluate(radius / maxRadius) : Mathf.Pow(radius, _exponentMinusOne + 1);
            _intensityMap.Add(intensity);
            if (_intensityMap.Count <= 1) continue;
            float lastIntensity = _intensityMap[^2];
            totalIntensity += (Mathf.Min(intensity, lastIntensity) + Mathf.Abs(intensity - lastIntensity) / 2) * sampleStep;
        }

        float currentRadius = 0;
        float currentIntensity = 0;
        int currentIndex = 0;
        for (int i = 0; i < interRings; i++) {
            // (i+1) to avoid drawing 0-radius ring. (interRings+1) to avoid drawing maxRadius ring (?)
            float targetIntensity = totalIntensity * (i + 1) / (interRings + 1);
            do {
                float intensity = _intensityMap[currentIndex];
                float nextIntensity = _intensityMap[currentIndex + 1];

                float area = (Mathf.Min(intensity, nextIntensity) + Mathf.Abs(intensity - nextIntensity) / 2) * sampleStep;
                currentIntensity += area;
                currentIndex = Mathf.Clamp(currentIndex + 1, 0, _intensityMap.Count - 2);
                currentRadius += sampleStep;
            } while (currentIntensity < targetIntensity);

            GraphicControls.CircleRadius(_position, currentRadius, interColor, dashed: true, interactable: false);
        }
    }
}
