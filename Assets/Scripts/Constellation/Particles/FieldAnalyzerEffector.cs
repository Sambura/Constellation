using System;
using UnityEngine;
using ConfigSerialization;

public sealed class FieldAnalyzerEffector : IParticleEffector
{
    private ParticleController _particleController;
    private Particle _probe = new Particle();
    private float _maxVelocity = 2;
    private float _minVelocity = 0.3f;

    [SliderProperty(0.5f, 10, 0, hasEvent: false)] public float Density { get; set; } = 2;
    [MinMaxSliderProperty(0, 5, 0, name: "Velocity range", higherPropertyName: nameof(MaxVelocity))]
    public float MinVelocity {
        get => _minVelocity;
        set { if (_minVelocity != value) { _minVelocity = value; MinVelocityChanged?.Invoke(value); } }
    }
    [MinMaxSliderProperty(0, 5, 0)]
    public float MaxVelocity {
        get => _maxVelocity;
        set { if (_maxVelocity != value) { _maxVelocity = value; MaxVelocityChanged?.Invoke(value); } }
    }
    [ConfigProperty(AllowPolling = true)]
    public bool ShowZeros { get; set; } = true;
    [ConfigProperty(AllowPolling = true)]
    public Gradient Gradient { get; set; } = new Gradient() { colorKeys = new GradientColorKey[] {
            new GradientColorKey(Color.green, 0),
            new GradientColorKey(Color.yellow, 0.33f),
            new GradientColorKey(Color.red, 0.67f), 
            new GradientColorKey(Color.magenta, 1) 
        }, 
        alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1, 0) }
    };
    [SliderProperty(0, 1, 0, 1, AllowPolling = true)]
    public float Opacity {
        get => Gradient.alphaKeys[0].alpha; 
        set {
            Gradient.alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(value, 0) };
        }
    }

    public event Action<float> MinVelocityChanged;
    public event Action<float> MaxVelocityChanged;

    public string Name { get; set; } = "Field Analyzer";
    public bool Initialized { get; private set; }
    public ControlType ControlType { get; } = ControlType.Visualizers;
    public ControlType DefaultControlType { get; } = ControlType.Visualizers;
    public EffectorType EffectorType { get; } = EffectorType.Passive;

    public void AffectParticle(Particle p) {}

    public void Init(ParticleController controller)
    {
        _particleController = controller;
        _probe.VelocityDelegate = controller.GetParticleVelocity;
        Initialized = true;
    }

    public void Detach() { Initialized = false; }

    public void RenderControls(ControlType controlTypes) {
        if (!controlTypes.HasFlag(ControlType.Visualizers)) return;

        int hProbes = Mathf.RoundToInt(_particleController.Viewport.Height * Density);
        int wProbes = Mathf.RoundToInt(_particleController.Viewport.Width * Density);
        float width = _particleController.Viewport.Width / wProbes;
        float height = _particleController.Viewport.Height / hProbes;
        float zeroSize = _particleController.Viewport.UnitsPerPixel / 2;

        float maxSize = Mathf.Min(width, height);
        float minSize = maxSize / 6;

        float minValue = MinVelocity;
        float maxValue = MaxVelocity;

        for (int x = 0; x < wProbes; x++) {
            for (int y = 0; y < hProbes; y++) {
                Vector2 pos = new Vector2(-_particleController.Viewport.MaxX + width / 2 + width * x, 
                                          -_particleController.Viewport.MaxY + height / 2 + height * y);

                _probe.Velocity = Vector3.zero;
                _probe.Position = pos;

                foreach (var effector in _particleController.ActiveEffectors)
                    effector.AffectParticle(_probe);

                Vector2 velocity = (_probe.Velocity + (_probe.Position - (Vector3)pos) / Time.deltaTime) / Time.deltaTime;
                float value = velocity.magnitude;
                if (value <= 0) {
                    if (ShowZeros) {
                        GraphicControls.Line(pos, Vector2.one * zeroSize, Gradient.Evaluate(0), out float _, interactable: false);
                        GraphicControls.Line(pos, new Vector2(1, -1) * zeroSize, Gradient.Evaluate(0), out float _, interactable: false);
                    }
                    continue;
                }
                float relative = 4 * (value - minValue) / (maxValue - minValue);
                velocity = velocity.normalized * Mathf.Lerp(minSize, maxSize, relative);
                Color color = Gradient.Evaluate(Mathf.Max(0, (relative - 1) / 3));
                GraphicControls.Arrow(pos, velocity, color, interactable: false);
            }
        }
    }
}
