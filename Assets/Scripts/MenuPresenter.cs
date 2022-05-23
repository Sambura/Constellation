using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class MenuPresenter : MonoBehaviour
{
	[Header("General")]
    [SerializeField] private SimulationController _controller;
	[SerializeField] private GameObject _menuPanel;
	[SerializeField] private GameObject _menuOpenButton;
	[SerializeField] private ColorPicker _colorPicker;
	[SerializeField] private Vector2 _colorPickerOffset;
	[Header("General appearance")]
	[SerializeField] private Toggle _showParticlesToggle;
	[SerializeField] private GameObject _showParticlesSubMenuOverlay;
	[SerializeField] private SliderWithText _particleSizeSlider;
	[SerializeField] private ColorPickerButton _particleColorButton;
	[SerializeField] private Toggle _showLinesToggle;
	[SerializeField] private GameObject _showLinesSubMenuOverlay;
	[SerializeField] private ColorPickerButton _lineTempColorButton;
	[SerializeField] private Toggle _meshLinesToggle;
	[SerializeField] private GameObject _meshLinesSubMenuOverlay;
	[SerializeField] private SliderWithText _meshLineWidthSlider;
	[SerializeField] private Toggle _showTrianglesToggle;
	[SerializeField] private GameObject _trianglesSubMenuOverlay;
	[SerializeField] private SliderWithText _triangleFillOpacitySlider;

	[Header("Simulation parameters")]
	[SerializeField] private SliderWithText _particlesCountSlider;
	[SerializeField] private SliderWithText _connectionDistanceSlider;
	[SerializeField] private SliderWithText _strongDistanceSlider;
	[SerializeField] private TextMeshProUGUI _performanceMeasureLabel;
	[SerializeField] private TextMeshProUGUI _estimatedFpsLabel;
	[SerializeField] private SliderWithText _minParticleVelocitySlider;
	[SerializeField] private SliderWithText _maxParticleVelocitySlider;

	private Action<Color> _colorPicerAction;

	public void OnMenuOpenButtonClick()
	{
		_menuPanel.SetActive(true);
		_menuOpenButton.SetActive(false);
	}

	public void OnMenuCloseButtonClick()
	{
		_menuPanel.SetActive(false);
		_menuOpenButton.SetActive(true);
	}

	private void Start()
	{
		if (_menuOpenButton.activeSelf) _menuPanel.SetActive(false);

		// UI initialization
		_showParticlesToggle.isOn = _controller.ShowParticles;
		_showParticlesSubMenuOverlay.SetActive(!_controller.ShowParticles);
		_particleSizeSlider.Value = _controller.ParticleSize;
		_particleColorButton.Color = _controller.ParticleColor;
		_showLinesToggle.isOn = _controller.ShowLines;
		_lineTempColorButton.Color = _controller.LineColorTemp;
		_meshLinesToggle.isOn = _controller.MeshLines;
		_meshLineWidthSlider.Value = _controller.LineWidth;
		_particlesCountSlider.Value = _controller.ParticleCount;
		_connectionDistanceSlider.Value = _controller.ConnectionDistance;
		_strongDistanceSlider.Value = _controller.StrongDistance;
		_performanceMeasureLabel.text = _controller.PerformanceMeasure.ToString();
		_estimatedFpsLabel.text = _controller.EstimatedFps.ToString("0.0");
		_showTrianglesToggle.isOn = _controller.ShowTriangles;
		_triangleFillOpacitySlider.Value = _controller.TriangleFillOpacity;
		_minParticleVelocitySlider.Value = _controller.MinParticleVelocity;
		_maxParticleVelocitySlider.Value = _controller.MaxParticleVelocity;

		_meshLinesSubMenuOverlay.SetActive(!_controller.MeshLines);
		_showLinesSubMenuOverlay.SetActive(!_controller.ShowLines);
		_showParticlesSubMenuOverlay.SetActive(!_controller.ShowParticles);
		_trianglesSubMenuOverlay.SetActive(!_controller.ShowTriangles);

		// Set up event listeners
		_colorPicker.ColorChanged += OnColorPickerColorChanged;

		_showParticlesToggle.onValueChanged.AddListener(OnShowParticlesChanged);
		_controller.ShowParticlesChanged += OnShowParticlesChangedExternal;

		_particleSizeSlider.ValueChanged += OnParticleSizeChanged;
		_controller.ParticleSizeChanged += OnParticleSizeChangedExternal;

		_particleColorButton.ButtonClick += OnParticleColorClick;
		_controller.ParticleColorChanged += OnParticleColorChangeExternal;

		_showLinesToggle.onValueChanged.AddListener(OnShowLinesChanged);
		_controller.ShowLinesChanged += OnShowLinesChangedExternal;

		_meshLinesToggle.onValueChanged.AddListener(OnMeshLinesChanged);
		_controller.MeshLinesChanged += OnMeshLinesChangedExternal;

		_meshLineWidthSlider.ValueChanged += OnLineWidthChanged;
		_controller.LineWidthChanged += OnLineWidthChangedExternal;

		_lineTempColorButton.ButtonClick += OnLineTempColorButtonClick;
		_controller.LineColorTempChanged += OnLineTempColorChangedExternal;

		_particlesCountSlider.ValueChanged += OnParticleCountChanged;
		_controller.ParticleCountChanged += OnParticleCountChangedExternal;

		_connectionDistanceSlider.ValueChanged += OnConnectionDistanceChanged;
		_controller.ConnectionDistanceChanged += OnConnectionDistanceChangedExternal;

		_strongDistanceSlider.ValueChanged += OnStrongDistanceChanged;
		_controller.StrongDistanceChanged += OnStrongDistanceChangedExternal;

		_showTrianglesToggle.onValueChanged.AddListener(OnShowTrianglesChanged);
		_controller.ShowTrianglesChanged += OnShowTrianglesChangedExternal;

		_triangleFillOpacitySlider.ValueChanged += OnTriangleFillOpacityChanged;
		_controller.TriangleFillOpacityChanged += OnTriangleFillOpacityChangedExternal;

		_minParticleVelocitySlider.ValueChanged += OnMinParticleVelocityChanged;
		_controller.MinParticleVelocityChanged += OnMinParticleVelocityChangedExternal;

		_maxParticleVelocitySlider.ValueChanged += OnMaxParticleVelocityChanged;
		_controller.MaxParticleVelocityChanged += OnMaxParticleVelocityChangedExternal;
	}

	private void OnMaxParticleVelocityChanged(float value)
	{
		_controller.MaxParticleVelocity = value;
	}

	private void OnMaxParticleVelocityChangedExternal(float value)
	{
		_maxParticleVelocitySlider.SetValueWithoutNotify(value);
	}

	private void OnMinParticleVelocityChanged(float value)
	{
		_controller.MinParticleVelocity = value;
	}

	private void OnMinParticleVelocityChangedExternal(float value)
	{
		_minParticleVelocitySlider.SetValueWithoutNotify(value);
	}

	private void OnTriangleFillOpacityChanged(float value)
	{
		_controller.TriangleFillOpacity = value;
	}

	private void OnTriangleFillOpacityChangedExternal(float value)
	{
		_triangleFillOpacitySlider.SetValueWithoutNotify(value);
	}

	private void OnShowTrianglesChanged(bool value)
	{
		_controller.ShowTriangles = value;
	}

	private void OnShowTrianglesChangedExternal(bool value)
	{
		_showTrianglesToggle.SetIsOnWithoutNotify(value);
		_trianglesSubMenuOverlay.SetActive(!_controller.ShowTriangles);
	}

	private void OnStrongDistanceChangedExternal(float value)
	{
		_strongDistanceSlider.SetValueWithoutNotify(value);
	}

	private void OnStrongDistanceChanged(float value)
	{
		_controller.StrongDistance = value;
	}

	private void OnConnectionDistanceChangedExternal(float value)
	{
		_connectionDistanceSlider.SetValueWithoutNotify(value);
		_performanceMeasureLabel.text = _controller.PerformanceMeasure.ToString();
		_estimatedFpsLabel.text = _controller.EstimatedFps.ToString("0.0");
	}

	private void OnConnectionDistanceChanged(float value)
	{
		_controller.ConnectionDistance = value;
	}

	private void OnParticleCountChangedExternal(int value)
	{
		_particlesCountSlider.Value = value;
		_performanceMeasureLabel.text = _controller.PerformanceMeasure.ToString();
		_estimatedFpsLabel.text = _controller.EstimatedFps.ToString("0.0");
	}

	private void OnParticleCountChanged(float value)
	{
		_controller.ParticleCount = Mathf.RoundToInt(value);
	}

	private void OnLineTempColorChangedExternal(Color color)
	{
		_lineTempColorButton.Color = color;
	}

	private void OnLineTempColorChanged(Color color)
	{
		_controller.LineColorTemp = color;
		_lineTempColorButton.Color = color;
	}

	private void OnLineTempColorButtonClick()
	{
		OpenColorPicker(_lineTempColorButton.transform, OnLineTempColorChanged, _lineTempColorButton.Color);
	}

	private void OnLineWidthChanged(float value)
	{
		_controller.LineWidth = value;
	}

	private void OnLineWidthChangedExternal(float value)
	{
		_meshLineWidthSlider.SetValueWithoutNotify(value);
	}

	private void OnMeshLinesChanged(bool value)
	{
		_controller.MeshLines = value;
	}

	private void OnMeshLinesChangedExternal(bool value)
	{
		_meshLinesToggle.SetIsOnWithoutNotify(value);
		_meshLinesSubMenuOverlay.SetActive(!value);
	}

	private void OnShowLinesChanged(bool value)
	{
		_controller.ShowLines = value;
	}

	private void OnShowLinesChangedExternal(bool value)
	{
		_showLinesToggle.SetIsOnWithoutNotify(value);
		_showLinesSubMenuOverlay.SetActive(!value);
	}

	private void OnParticleColorChangeExternal(Color color)
	{
		_particleColorButton.Color = color;
	}

	private void OnParticleColorChanged(Color color)
	{
		_controller.ParticleColor = color;
		_particleColorButton.Color = color;
	}

	private void OnColorPickerColorChanged(Color color) => _colorPicerAction(color);

	private void OnParticleColorClick()
	{
		OpenColorPicker(_particleColorButton.transform, OnParticleColorChanged, _particleColorButton.Color);
	}

	private void OnParticleSizeChanged(float value)
	{
		_controller.ParticleSize = value;
	}

	private void OnParticleSizeChangedExternal(float value)
	{
		_particleSizeSlider.SetValueWithoutNotify(value);
	}

	private void OnShowParticlesChanged(bool value)
	{
		_controller.ShowParticles = value;
	}

	private void OnShowParticlesChangedExternal(bool value)
	{
		_showParticlesToggle.SetIsOnWithoutNotify(value);
		_showParticlesSubMenuOverlay.SetActive(!value);
	}

	private void OpenColorPicker(Transform button, Action<Color> colorPickAction, Color initialColor)
	{
		_colorPicker.transform.position = button.position + (Vector3)_colorPickerOffset;
		_colorPicker.gameObject.SetActive(true);
		_colorPicerAction = colorPickAction;
		_colorPicker.Color = initialColor;
	}

	private void OnDestroy()
	{
		_showParticlesToggle.onValueChanged.RemoveListener(OnShowParticlesChanged);
		_controller.ShowParticlesChanged -= OnShowParticlesChangedExternal;
		_particleSizeSlider.ValueChanged -= OnParticleSizeChanged;
		_controller.ParticleSizeChanged -= OnParticleSizeChangedExternal;
	}
}
