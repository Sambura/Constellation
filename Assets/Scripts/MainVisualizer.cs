using System.Collections;
using UnityEngine;
using SimpleGraphics;
using ConfigSerialization;
using ConfigSerialization.Structuring;
using System.Collections.Generic;

public class MainVisualizer : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private ImmediateBatchRenderer _drawer;
    [SerializeField] private Viewport _viewport;
    [SerializeField] private ParticleController _particleController;

    [Header("Static properties")]
    [SerializeField] private Material _particleMaterial;

    [Header("General appearance parameters")]
    [SerializeField] private bool _showParticles = true;
    [SerializeField] private float _particlesSize = 0.1f;
    [SerializeField] private Color _particlesColor = Color.white;
    [SerializeField] private float _lineWidth = 0.1f;
    [SerializeField] private bool _meshLines = false;
    [SerializeField] private float _connectionDistance = 60f;
    [SerializeField] private float _strongDistance = 10f;
    [SerializeField] private AnimationCurve _alphaCurve;
    [SerializeField] private Gradient _lineColor;
    [SerializeField] private bool _showLines = true;
    [SerializeField] private bool _showTriangles = true;
    [SerializeField] private Color _clearColor = Color.black;
    [SerializeField][Range(0, 1)] private float _triangleFillOpacity;
    [SerializeField] private Texture2D _particleSprite;

    [Header("Main visualizer parameters")]
    [SerializeField] private bool _alternateLineColor = true;
    [SerializeField] private float _colorMinHue = 0;
    [SerializeField] private float _colorMaxHue = 1;
    [SerializeField] private float _colorMinSaturation = 0;
    [SerializeField] private float _colorMaxSaturation = 1;
    [SerializeField] private float _colorMinValue = 0.3f;
    [SerializeField] private float _colorMaxValue = 1;
    [SerializeField] private float _colorMinFadeDuration = 2;
    [SerializeField] private float _colorMaxFadeDuration = 4;
    [SerializeField] private string _selectedVisualizer = "Classic";

    [Header("Visualizer implementations")]
    [SerializeField] private List<VisualizerImplementation> _visualizers;

    [System.Serializable]
    struct VisualizerImplementation {
        public string Name;
        public MonoBehaviour Visualizer;
    }

    #region Config properties

    [ConfigGroupToggle(2)]
    [ConfigGroupMember("General appearance")] [ConfigProperty] public bool ShowParticles
    {
        get => _showParticles;
        set { if (_showParticles != value) { _showParticles = value; ShowParticlesChanged?.Invoke(value); } }
    }
    [SetComponentProperty(typeof(UIArranger), nameof(UIArranger.SelectedConfigurationName), "Compact")]
    [ConfigGroupMember(2, 0)] [SliderProperty(0, 0.1f, 0, 10, name: "Size")] public float ParticleSize
    {
        get => _particlesSize;
        set { if (_particlesSize != value) { SetParticleSize(value); ParticleSizeChanged?.Invoke(value); } }
    }
    [ConfigGroupMember(2)] [ColorPickerButtonProperty(true, "Select Particles Color", "Color")] public Color ParticleColor
    {
        get => _particlesColor;
        set { if (_particlesColor != value) { SetParticleColor(value); ParticleColorChanged?.Invoke(value); } }
    }
    [ConfigGroupMember(2)] [ConfigProperty("Sprite")] public Texture2D ParticleSprite
    {
        get => _particleSprite;
        set { if (_particleSprite != value) { _particleSprite = value; ParticleSpriteChanged?.Invoke(value); } }
    }
    [ConfigGroupToggle(3)]
    [ConfigGroupMember] [ConfigProperty] public bool ShowLines
    {
        get => _showLines;
        set { if (_showLines != value) { _showLines = value; ShowLinesChanged?.Invoke(value); }; }
    }
    [ConfigGroupToggle(4)]
    [ConfigGroupMember(3, 0)] [ConfigProperty] public bool MeshLines
    {
        get => _meshLines;
        set { if (_meshLines != value) { _meshLines = value; MeshLinesChanged?.Invoke(value); }; }
    }
    [SetComponentProperty(typeof(UIArranger), nameof(UIArranger.SelectedConfigurationName), "Compact")]
    [ConfigGroupMember(4, 3)] [SliderProperty(0.001f, 0.05f, 0, 0.5f, "0.0000", name: "Width")] public float LineWidth
    {
        get => _lineWidth;
        set { if (_lineWidth != value) { _lineWidth = value; LineWidthChanged?.Invoke(value); }; }
    }
    [ConfigGroupToggle(5)]
    [ConfigGroupMember] [ConfigProperty] public bool ShowTriangles
    {
        get => _showTriangles;
        set { if (_showTriangles != value) { _showTriangles = value; ShowTrianglesChanged?.Invoke(value); }; }
    }
    [ConfigGroupMember(5, 0)] [SliderProperty(name: "Fill opacity")] public float TriangleFillOpacity
    {
        get => _triangleFillOpacity;
        set { if (_triangleFillOpacity != value) { _triangleFillOpacity = value; TriangleFillOpacityChanged?.Invoke(value); }; }
    }
    [ConfigGroupMember] [CurvePickerButtonProperty("Modify Alpha Curve")] public AnimationCurve AlphaCurve
    {
        get => _alphaCurve;
        set { if (_alphaCurve != value) { _alphaCurve = value; AlphaCurveChanged?.Invoke(value); } }
    }
    [ConfigGroupToggle(7, 6)]
    [ConfigGroupMember] [RadioButtonsProperty(new string[] { "Color gradient", "Alternating Color" })] public bool AlternateLineColor
    {
        get => _alternateLineColor;
        set { if (_alternateLineColor != value) { SetAlternateLineColor(value); AlternateLineColorChanged?.Invoke(value); } }
    }
    [ConfigGroupMember(6, 0)] [GradientPickerButtonProperty("Set Line Color Gradient", "Gradient")] public Gradient LineColor
    {
        get => _lineColor;
        set { if (_lineColor != value) { SetLineColor(value); LineColorChanged?.Invoke(value); } }
    }
    [ConfigGroupMember(7, 0)] [MinMaxSliderProperty(0, 10, 0, 300, "0.00 s", @"([-+]?[0-9]*\.?[0-9]+) *s?", name: "Fade duration", higherPropertyName: nameof(MaxColorFadeDuration))]
    public float MinColorFadeDuration
    {
        get => _colorMinFadeDuration;
        set { if (_colorMinFadeDuration != value) { _colorMinFadeDuration = value; RestartColorChanger(); MinColorFadeDurationChanged?.Invoke(MinColorFadeDuration); } }
    }
    [ConfigGroupMember(7)] [MinMaxSliderProperty] public float MaxColorFadeDuration
    {
        get => _colorMaxFadeDuration;
        set { if (_colorMaxFadeDuration != value) { _colorMaxFadeDuration = value; RestartColorChanger(); MaxColorFadeDurationChanged?.Invoke(MaxColorFadeDuration); } }
    }
    [ConfigGroupMember] [ColorPickerButtonProperty(true, name: "Background clear color")] public Color ClearColor
    {
        get => _clearColor;
        set { if (_clearColor != value) { _clearColor = value; ClearColorChanged?.Invoke(value); }; }
    }
    [ConfigMemberOrder(1)] [ConfigGroupMember(1, GroupId = "PC+sim_params")] [MinMaxSliderProperty] public float ConnectionDistance {
        get => _connectionDistance;
        set { if (_connectionDistance != value) { SetConnectionDistance(value); ConnectionDistanceChanged?.Invoke(value); }; }
    }
    [ConfigMemberOrder(1)]
    [ConfigGroupMember(1)] [MinMaxSliderProperty(0, 3, 0, 100, "0.00", lowerLabel: "Strong", higherPropertyName: nameof(ConnectionDistance), name: "Connection distance", minMaxSpacing: 1e-3f)]
    public float StrongDistance
    {
        get => _strongDistance;
        set { if (_strongDistance != value) { _strongDistance = value; StrongDistanceChanged?.Invoke(value); }; }
    }
    // TODO: implement dynamic options for dropdowns?
    [ConfigGroupMember] 
    [Core.Json.NoJsonSerialization(AllowFromJson = true)]
    [DropdownProperty(new object[] { "None", "Classic", "Meshed" }, new string[] { "Disable rendering", "Classic (slower)", "Meshed (faster)" }, name: "Renderer")]
    public string SelectedVisualizer
    {
        get => _selectedVisualizer;
        set { if (_selectedVisualizer != value) { SetSelectedVisualizer(value); SelectedVisualizerChanged?.Invoke(value); } }
    }

    public event System.Action<bool> ShowParticlesChanged;
    public event System.Action<Texture2D> ParticleSpriteChanged;
    public event System.Action<float> ParticleSizeChanged;
    public event System.Action<Color> ParticleColorChanged;
    public event System.Action<bool> ShowLinesChanged;
    public event System.Action<bool> MeshLinesChanged;
    public event System.Action<Gradient> LineColorChanged;
    public event System.Action<float> LineWidthChanged;
    public event System.Action<float> ConnectionDistanceChanged;
    public event System.Action<float> StrongDistanceChanged;
    public event System.Action<bool> ShowTrianglesChanged;
    public event System.Action<float> TriangleFillOpacityChanged;
    public event System.Action<Color> ClearColorChanged;
    public event System.Action<AnimationCurve> AlphaCurveChanged;
    public event System.Action<bool> AlternateLineColorChanged;
    public event System.Action<float> MinColorFadeDurationChanged;
    public event System.Action<float> MaxColorFadeDurationChanged;
    public event System.Action<string> SelectedVisualizerChanged;

    #endregion

    public ParticleController ParticleController { get; set; }
    public Gradient ActualLineColor => _currentLineColorGradient;
    public event System.Action<Gradient> ActualLineColorChanged;
    public VisualizerBase Visualizer => _visualizers.Find(x => x.Name == SelectedVisualizer).Visualizer as VisualizerBase;
    public bool ColorBufferClearEnabled {
        get => _colorBufferClearEnabled;
        set { _colorBufferClearEnabled = value; SetSelectedVisualizer(SelectedVisualizer); }
    }

    private Gradient _currentLineColorGradient;
    private GradientColorKey[] _currentLineGradientColorKeys;
    private Coroutine _colorChanger;
    private bool _colorBufferClearEnabled = false;

    private void RestartColorChanger()
    {
        if (_colorChanger != null) {
            StopCoroutine(_colorChanger);
            _colorChanger = StartCoroutine(ColorChanger());
        }
    }

    private void SetParticleSize(float value)
    {
        _particlesSize = value;
        if (ParticleController.Particles is null) return;

        foreach (Particle p in ParticleController.Particles)
            p.Size = value;
    }

    private void SetParticleColor(Color value)
    {
        _particlesColor = value;
        if (ParticleController.Particles is null) return;

        foreach (Particle p in ParticleController.Particles)
            p.Color = value;
    }

    private void SetLineColor(Gradient value)
    {
        _lineColor = value;
        if (_alternateLineColor) return;

        _currentLineGradientColorKeys = value.colorKeys;
        _currentLineColorGradient.colorKeys = _currentLineGradientColorKeys;
        ActualLineColorChanged?.Invoke(ActualLineColor);
    }

    private void SetAlternateLineColor(bool value)
    {
        _alternateLineColor = value;

        if (value == false)
        {
            if (_colorChanger != null)
            {
                StopCoroutine(_colorChanger);
                _colorChanger = null;
            }
            SetLineColor(_lineColor);
            return;
        }

        _currentLineGradientColorKeys = new GradientColorKey[] { new GradientColorKey(_lineColor.Evaluate(1), 1) };
        _colorChanger = StartCoroutine(ColorChanger());
    }

    private void SetConnectionDistance(float distance) {
        _connectionDistance = distance;

        ParticleController.SetFragmentSize(distance);
    }

    private void SetSelectedVisualizer(string value)
    {
        _selectedVisualizer = value;
        MonoBehaviour toSelect = this;

        foreach (var impl in _visualizers) {
            if (impl.Name == value)
                toSelect = impl.Visualizer;

            if (impl.Visualizer is null) continue;
            impl.Visualizer.enabled = false;            
        }

        if (toSelect == this) {
            Debug.LogWarning($"Failed to find visualizer: `{value}`");
            return;
        }
        if (toSelect is null) return;

        toSelect.enabled = true;
        (toSelect as VisualizerBase).SetClearColorBufferEnabled(ColorBufferClearEnabled);
    }

    private void Awake()
    {
        ParticleController = FindFirstObjectByType<ParticleController>();
        _currentLineColorGradient = new Gradient();

        SetParticleSize(_particlesSize);
        SetParticleColor(_particlesColor);
        SetLineColor(_lineColor);
        SetAlternateLineColor(_alternateLineColor);
        SetConnectionDistance(_connectionDistance);

        ParticleController.ParticleCreated += OnParticleCreated;
    }

    private void OnDisable() {
        foreach (var impl in _visualizers) {
            if (impl.Visualizer is null) continue;
            impl.Visualizer.enabled = false;
        }
    }

    private void OnEnable() {
        SetSelectedVisualizer(_selectedVisualizer);
    }

    private void OnParticleCreated(Particle particle)
    {
        // particle.Visible = _showParticles;
        particle.Color = _particlesColor;
        particle.Size = _particlesSize;
    }

    private IEnumerator ColorChanger()
    {
        while (true)
        {
            Color oldColor = _currentLineGradientColorKeys[0].color;
            Color newColor = Random.ColorHSV(_colorMinHue, _colorMaxHue, _colorMinSaturation, _colorMaxSaturation, _colorMinValue, _colorMaxValue);
            float fadeTime = Random.Range(_colorMinFadeDuration, _colorMaxFadeDuration);
            float startTime = Time.time;
            float progress = 0;
            if (fadeTime == 0)
            {
                fadeTime = 0.001f;
                startTime -= 1;
            }

            while (progress < 1)
            {
                progress = (Time.time - startTime) / fadeTime;
                Color currentColor = Color.Lerp(oldColor, newColor, progress);
                _currentLineGradientColorKeys[0].color = currentColor;
                _currentLineColorGradient.colorKeys = _currentLineGradientColorKeys;
                ActualLineColorChanged?.Invoke(ActualLineColor);

                yield return null;
            }
        }
    }
}
