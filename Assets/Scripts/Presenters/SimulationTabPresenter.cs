using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimulationTabPresenter : MonoBehaviour
{
	[Header("General")]
	[SerializeField] private MainVisualizer _vizualizator;
	[SerializeField] private ParticleController _particles;

	[Header("General appearance")]
	[SerializeField] private Toggle _showParticlesToggle;
	[SerializeField] private SliderWithText _particleSizeSlider;
	[SerializeField] private ColorPickerButton _particleColorButton;
	[SerializeField] private Toggle _showLinesToggle;
	[SerializeField] private Toggle _meshLinesToggle;
	[SerializeField] private SliderWithText _meshLineWidthSlider;
	[SerializeField] private Toggle _showTrianglesToggle;
	[SerializeField] private SliderWithText _triangleFillOpacitySlider;
	[SerializeField] private CurvePickerButton _alphaCurveButton;
	[SerializeField] private Toggle _gradientColorToggle;
	[SerializeField] private Toggle _alternateColorToggle;
	[SerializeField] private GradientPickerButton _lineColorGradientButton;
	[SerializeField] private MinMaxSliderWithInput _colorFadeDurationSlider;
	[SerializeField] private ColorPickerButton _clearColorButton;
	[SerializeField] private Button _restartSimulationButton;

	[Header("Simulation parameters")]
	[SerializeField] private SliderWithText _particlesCountSlider;
	[SerializeField] private SliderWithText _connectionDistanceSlider;
	[SerializeField] private SliderWithText _strongDistanceSlider;
	[SerializeField] private SliderWithText _minParticleVelocitySlider;
	[SerializeField] private SliderWithText _maxParticleVelocitySlider;
	[SerializeField] private SliderWithText _boundMarginsSlider;

	private void Start()
	{
		// UI initialization
		_showParticlesToggle.isOn = _particles.ShowParticles;
		_particleSizeSlider.Value = _particles.ParticleSize;
		_particleColorButton.Color = _particles.ParticleColor;
		_showLinesToggle.isOn = _vizualizator.ShowLines;
		_meshLinesToggle.isOn = _vizualizator.MeshLines;
		_meshLineWidthSlider.Value = _vizualizator.LineWidth;
		_particlesCountSlider.Value = _particles.ParticleCount;
		_connectionDistanceSlider.Value = _vizualizator.ConnectionDistance;
		_strongDistanceSlider.Value = _vizualizator.StrongDistance;
		_showTrianglesToggle.isOn = _vizualizator.ShowTriangles;
		_triangleFillOpacitySlider.Value = _vizualizator.TriangleFillOpacity;
		_minParticleVelocitySlider.Value = _particles.MinParticleVelocity;
		_maxParticleVelocitySlider.Value = _particles.MaxParticleVelocity;
		_clearColorButton.Color = _vizualizator.ClearColor;
		_alphaCurveButton.Curve = _vizualizator.AlphaCurve;
		_lineColorGradientButton.Gradient = _vizualizator.LineColor;
		_alternateColorToggle.isOn = _vizualizator.AlternateLineColor;
		_colorFadeDurationSlider.MinValue = _vizualizator.MinColorFadeDuration;
		_colorFadeDurationSlider.MaxValue = _vizualizator.MaxColorFadeDuration;
		_boundMarginsSlider.Value = _particles.BoundMargins;

		// Set up event listeners
		_showParticlesToggle.onValueChanged.AddListener(OnShowParticlesChanged);
		_particles.ShowParticlesChanged += OnShowParticlesChanged;

		_particleSizeSlider.ValueChanged += OnParticleSizeChanged;
		_particles.ParticleSizeChanged += OnParticleSizeChanged;

		_particleColorButton.ColorChanged += OnParticleColorChanged;
		_particles.ParticleColorChanged += OnParticleColorChanged;

		_showLinesToggle.onValueChanged.AddListener(OnShowLinesChanged);
		_vizualizator.ShowLinesChanged += OnShowLinesChanged;

		_meshLinesToggle.onValueChanged.AddListener(OnMeshLinesChanged);
		_vizualizator.MeshLinesChanged += OnMeshLinesChanged;

		_meshLineWidthSlider.ValueChanged += OnLineWidthChanged;
		_vizualizator.LineWidthChanged += OnLineWidthChanged;

		_particlesCountSlider.IntValueChanged += OnParticleCountChanged;
		_particles.ParticleCountChanged += OnParticleCountChanged;

		_connectionDistanceSlider.ValueChanged += OnConnectionDistanceChanged;
		_vizualizator.ConnectionDistanceChanged += OnConnectionDistanceChanged;

		_strongDistanceSlider.ValueChanged += OnStrongDistanceChanged;
		_vizualizator.StrongDistanceChanged += OnStrongDistanceChanged;

		_showTrianglesToggle.onValueChanged.AddListener(OnShowTrianglesChanged);
		_vizualizator.ShowTrianglesChanged += OnShowTrianglesChanged;

		_triangleFillOpacitySlider.ValueChanged += OnTriangleFillOpacityChanged;
		_vizualizator.TriangleFillOpacityChanged += OnTriangleFillOpacityChanged;

		_minParticleVelocitySlider.ValueChanged += OnMinParticleVelocityChanged;
		_particles.MinParticleVelocityChanged += OnMinParticleVelocityChanged;

		_maxParticleVelocitySlider.ValueChanged += OnMaxParticleVelocityChanged;
		_particles.MaxParticleVelocityChanged += OnMaxParticleVelocityChanged;

		_clearColorButton.ColorChanged += OnClearColorChanged;
		_vizualizator.ClearColorChanged += OnClearColorChanged;

		_alphaCurveButton.CurveChanged += OnAlphaCurveChanged;
		_vizualizator.AlphaCurveChanged += OnAlphaCurveChanged;

		_lineColorGradientButton.GradientChanged += OnLineColorChanged;
		_vizualizator.LineColorChanged += OnLineColorChanged;

		_alternateColorToggle.onValueChanged.AddListener(OnAlternateColorChanged);
		_vizualizator.AlternateLineColorChanged += OnAlternateColorChanged;

		_colorFadeDurationSlider.MinValueChanged += OnMinColorFadeDurationChanged;
		_vizualizator.MinColorFadeDurationChanged += OnMinColorFadeDurationChanged;

		_colorFadeDurationSlider.MaxValueChanged += OnMaxColorFadeDurationChanged;
		_vizualizator.MaxColorFadeDurationChanged += OnMaxColorFadeDurationChanged;

		_boundMarginsSlider.ValueChanged += OnBoundMarginsChanged;
		_particles.BoundMarginsChanged += OnBoundMarginsChanged;

		_restartSimulationButton.onClick.AddListener(OnRestartSimulationButtonClick);
	}

	private void OnRestartSimulationButtonClick()
	{
		_particles.ReinitializeParticles();
	}

	private void OnMaxColorFadeDurationChanged(float value)
	{
		_vizualizator.MaxColorFadeDuration = value;
		_colorFadeDurationSlider.SetMaxValueWithoutNotify(value);
	}

	private void OnMinColorFadeDurationChanged(float value)
	{
		_vizualizator.MinColorFadeDuration = value;
		_colorFadeDurationSlider.SetMinValueWithoutNotify(value);
	}

	private void OnAlternateColorChanged(bool value)
	{
		_vizualizator.AlternateLineColor = value;
		_alternateColorToggle.isOn = value;
		_gradientColorToggle.isOn = !value;
	}

	private void OnLineColorChanged(Gradient gradient)
	{
		_vizualizator.LineColor = gradient;
		_lineColorGradientButton.Gradient = gradient;
	}

	private void OnAlphaCurveChanged(AnimationCurve value)
	{
		_vizualizator.AlphaCurve = value;
		_alphaCurveButton.Curve = value;
	}

	private void OnClearColorChanged(Color value)
	{
		_vizualizator.ClearColor = value;
		_clearColorButton.Color = value;
	}

	private void OnMaxParticleVelocityChanged(float value)
	{
		value = Mathf.Max(value, _particles.MinParticleVelocity);
		_particles.MaxParticleVelocity = value;
		_maxParticleVelocitySlider.SetValueWithoutNotify(value);
	}

	private void OnMinParticleVelocityChanged(float value)
	{
		value = Mathf.Min(value, _particles.MaxParticleVelocity);
		_particles.MinParticleVelocity = value;
		_minParticleVelocitySlider.SetValueWithoutNotify(value);
	}

	private void OnTriangleFillOpacityChanged(float value)
	{
		_vizualizator.TriangleFillOpacity = value;
		_triangleFillOpacitySlider.SetValueWithoutNotify(value);
	}

	private void OnShowTrianglesChanged(bool value)
	{
		_vizualizator.ShowTriangles = value;
		_showTrianglesToggle.isOn = value;
	}

	private void OnStrongDistanceChanged(float value)
	{
		value = Mathf.Min(value, _vizualizator.ConnectionDistance * 0.9999f);
		_vizualizator.StrongDistance = value;
		_strongDistanceSlider.SetValueWithoutNotify(value);
	}

	private void OnConnectionDistanceChanged(float value)
	{
		value = Mathf.Max(value, _vizualizator.StrongDistance / 0.9999f);
		_vizualizator.ConnectionDistance = value;
		_connectionDistanceSlider.SetValueWithoutNotify(value);
	}

	private void OnParticleCountChanged(int value)
	{
		_particles.ParticleCount = value;
		_particlesCountSlider.SetValueWithoutNotify(value);
	}

	private void OnLineWidthChanged(float value)
	{
		_vizualizator.LineWidth = value;
		_meshLineWidthSlider.SetValueWithoutNotify(value);
	}

	private void OnMeshLinesChanged(bool value)
	{
		_vizualizator.MeshLines = value;
		_meshLinesToggle.isOn = value;
	}

	private void OnShowLinesChanged(bool value)
	{
		_vizualizator.ShowLines = value;
		_showLinesToggle.isOn = value;
	}

	private void OnParticleColorChanged(Color color)
	{
		_particles.ParticleColor = color;
		_particleColorButton.Color = color;
	}

	private void OnParticleSizeChanged(float value)
	{
		_particles.ParticleSize = value;
		_particleSizeSlider.SetValueWithoutNotify(value);
	}

	private void OnShowParticlesChanged(bool value)
	{
		_particles.ShowParticles = value;
		_showParticlesToggle.isOn = value;
	}

	private void OnBoundMarginsChanged(float value)
	{
		_particles.BoundMargins = value;
		_boundMarginsSlider.SetValueWithoutNotify(value);
	}

	private void OnDestroy()
	{
		//_showParticlesToggle.onValueChanged.RemoveListener(OnShowParticlesChanged);
		//_controller.ShowParticlesChanged -= OnShowParticlesChangedExternal;
		//_particleSizeSlider.ValueChanged -= OnParticleSizeChanged;
		//_controller.ParticleSizeChanged -= OnParticleSizeChangedExternal;
	}
}
