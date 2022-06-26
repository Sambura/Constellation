using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimulationTabPresenter : MonoBehaviour
{
	[Header("General")]
	[SerializeField] private SimulationController _controller;
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
	[SerializeField] private GradientPickerButton _lineColorGradientButton;
	[SerializeField] private ColorPickerButton _clearColorButton;
	[SerializeField] private Toggle _alternateColorToggle;
	[SerializeField] private MinMaxSliderWithInput _colorFadeDurationSlider;

	[Header("Simulation parameters")]
	[SerializeField] private SliderWithText _particlesCountSlider;
	[SerializeField] private SliderWithText _connectionDistanceSlider;
	[SerializeField] private SliderWithText _strongDistanceSlider;
	[SerializeField] private SliderWithText _minParticleVelocitySlider;
	[SerializeField] private SliderWithText _maxParticleVelocitySlider;

	private void Start()
	{
		// UI initialization
		_showParticlesToggle.isOn = _particles.ShowParticles;
		_particleSizeSlider.Value = _particles.ParticleSize;
		_particleColorButton.Color = _particles.ParticleColor;
		_showLinesToggle.isOn = _controller.ShowLines;
		_meshLinesToggle.isOn = _controller.MeshLines;
		_meshLineWidthSlider.Value = _controller.LineWidth;
		_particlesCountSlider.Value = _particles.ParticleCount;
		_connectionDistanceSlider.Value = _controller.ConnectionDistance;
		_strongDistanceSlider.Value = _controller.StrongDistance;
		_showTrianglesToggle.isOn = _controller.ShowTriangles;
		_triangleFillOpacitySlider.Value = _controller.TriangleFillOpacity;
		_minParticleVelocitySlider.Value = _particles.MinParticleVelocity;
		_maxParticleVelocitySlider.Value = _particles.MaxParticleVelocity;
		_clearColorButton.Color = _controller.ClearColor;
		_alphaCurveButton.Curve = _controller.AlphaCurve;
		_lineColorGradientButton.Gradient = _controller.LineColor;
		_alternateColorToggle.isOn = _controller.AlternateLineColor;
		_colorFadeDurationSlider.MinValue = _controller.MinColorFadeDuration;
		_colorFadeDurationSlider.MaxValue = _controller.MaxColorFadeDuration;

		// Set up event listeners
		_showParticlesToggle.onValueChanged.AddListener(OnShowParticlesChanged);
		_particles.ShowParticlesChanged += OnShowParticlesChanged;

		_particleSizeSlider.ValueChanged += OnParticleSizeChanged;
		_particles.ParticleSizeChanged += OnParticleSizeChanged;

		_particleColorButton.ColorChanged += OnParticleColorChanged;
		_particles.ParticleColorChanged += OnParticleColorChanged;

		_showLinesToggle.onValueChanged.AddListener(OnShowLinesChanged);
		_controller.ShowLinesChanged += OnShowLinesChanged;

		_meshLinesToggle.onValueChanged.AddListener(OnMeshLinesChanged);
		_controller.MeshLinesChanged += OnMeshLinesChanged;

		_meshLineWidthSlider.ValueChanged += OnLineWidthChanged;
		_controller.LineWidthChanged += OnLineWidthChanged;

		_particlesCountSlider.IntValueChanged += OnParticleCountChanged;
		_particles.ParticleCountChanged += OnParticleCountChanged;

		_connectionDistanceSlider.ValueChanged += OnConnectionDistanceChanged;
		_controller.ConnectionDistanceChanged += OnConnectionDistanceChanged;

		_strongDistanceSlider.ValueChanged += OnStrongDistanceChanged;
		_controller.StrongDistanceChanged += OnStrongDistanceChanged;

		_showTrianglesToggle.onValueChanged.AddListener(OnShowTrianglesChanged);
		_controller.ShowTrianglesChanged += OnShowTrianglesChanged;

		_triangleFillOpacitySlider.ValueChanged += OnTriangleFillOpacityChanged;
		_controller.TriangleFillOpacityChanged += OnTriangleFillOpacityChanged;

		_minParticleVelocitySlider.ValueChanged += OnMinParticleVelocityChanged;
		_particles.MinParticleVelocityChanged += OnMinParticleVelocityChanged;

		_maxParticleVelocitySlider.ValueChanged += OnMaxParticleVelocityChanged;
		_particles.MaxParticleVelocityChanged += OnMaxParticleVelocityChanged;

		_clearColorButton.ColorChanged += OnClearColorChanged;
		_controller.ClearColorChanged += OnClearColorChanged;

		_alphaCurveButton.CurveChanged += OnAlphaCurveChanged;
		_controller.AlphaCurveChanged += OnAlphaCurveChanged;

		_lineColorGradientButton.GradientChanged += OnLineColorChanged;
		_controller.LineColorChanged += OnLineColorChanged;

		_alternateColorToggle.onValueChanged.AddListener(OnAlternateColorChanged);
		_controller.AlternateLineColorChanged += OnAlternateColorChanged;

		_colorFadeDurationSlider.MinValueChanged += OnMinColorFadeDurationChanged;
		_controller.MinColorFadeDurationChanged += OnMinColorFadeDurationChanged;

		_colorFadeDurationSlider.MaxValueChanged += OnMaxColorFadeDurationChanged;
		_controller.MaxColorFadeDurationChanged += OnMaxColorFadeDurationChanged;
	}

	private void OnMaxColorFadeDurationChanged(float value)
	{
		_controller.MaxColorFadeDuration = value;
		_colorFadeDurationSlider.SetMaxValueWithoutNotify(value);
	}

	private void OnMinColorFadeDurationChanged(float value)
	{
		_controller.MinColorFadeDuration = value;
		_colorFadeDurationSlider.SetMinValueWithoutNotify(value);
	}

	private void OnAlternateColorChanged(bool value)
	{
		_controller.AlternateLineColor = value;
		_alternateColorToggle.SetIsOnWithoutNotify(value);
	}

	private void OnLineColorChanged(Gradient gradient)
	{
		_controller.LineColor = gradient;
		_lineColorGradientButton.Gradient = gradient;
	}

	private void OnAlphaCurveChanged(AnimationCurve value)
	{
		_controller.AlphaCurve = value;
		_alphaCurveButton.Curve = value;
	}

	private void OnClearColorChanged(Color value)
	{
		_controller.ClearColor = value;
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
		_controller.TriangleFillOpacity = value;
		_triangleFillOpacitySlider.SetValueWithoutNotify(value);
	}

	private void OnShowTrianglesChanged(bool value)
	{
		_controller.ShowTriangles = value;
		_showTrianglesToggle.SetIsOnWithoutNotify(value);
	}

	private void OnStrongDistanceChanged(float value)
	{
		value = Mathf.Min(value, _controller.ConnectionDistance * 0.9999f);
		_controller.StrongDistance = value;
		_strongDistanceSlider.SetValueWithoutNotify(value);
	}

	private void OnConnectionDistanceChanged(float value)
	{
		value = Mathf.Max(value, _controller.StrongDistance / 0.9999f);
		_controller.ConnectionDistance = value;
		_connectionDistanceSlider.SetValueWithoutNotify(value);
	}

	private void OnParticleCountChanged(int value)
	{
		_particles.ParticleCount = value;
		_particlesCountSlider.SetValueWithoutNotify(value);
	}

	private void OnLineWidthChanged(float value)
	{
		_controller.LineWidth = value;
		_meshLineWidthSlider.SetValueWithoutNotify(value);
	}

	private void OnMeshLinesChanged(bool value)
	{
		_controller.MeshLines = value;
		_meshLinesToggle.SetIsOnWithoutNotify(value);
	}

	private void OnShowLinesChanged(bool value)
	{
		_controller.ShowLines = value;
		_showLinesToggle.SetIsOnWithoutNotify(value);
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
		_showParticlesToggle.SetIsOnWithoutNotify(value);
	}

	private void OnDestroy()
	{
		//_showParticlesToggle.onValueChanged.RemoveListener(OnShowParticlesChanged);
		//_controller.ShowParticlesChanged -= OnShowParticlesChangedExternal;
		//_particleSizeSlider.ValueChanged -= OnParticleSizeChanged;
		//_controller.ParticleSizeChanged -= OnParticleSizeChangedExternal;
	}
}
