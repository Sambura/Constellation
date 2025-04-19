using UnityEngine;

public class VisualizerBase : MonoBehaviour
{
    protected bool _showParticles;
    protected float _particlesSize;
    protected Color _particlesColor;
    protected float _lineWidth;
    protected bool _meshLines;
    protected float _connectionDistance;
    protected float _strongDistance;
    protected AnimationCurve _alphaCurve;
    protected Gradient _lineColor;
    protected bool _showLines;
    protected bool _showTriangles;
    protected Color _clearColor;
    protected float _triangleFillOpacity;
    protected Texture2D _particleSprite;
    protected Gradient _actualLineColor;

    public MainVisualizer MainVisualizer { get; set; }
    public ParticleController ParticleController { get; set; }
    public bool RenderParticles { get; set; }
    public bool RenderLines { get; set; }
    public bool RenderTriangles { get; set; }
    public bool RenderBackground { get; set; }

    protected virtual void Awake() {
        MainVisualizer = FindObjectOfType<MainVisualizer>();
        ParticleController = FindObjectOfType<ParticleController>();
        if (MainVisualizer == this) return;

        MainVisualizer.ShowParticlesChanged += SetShowParticles;
        MainVisualizer.ParticleSizeChanged += SetParticleSize;
        MainVisualizer.ParticleColorChanged += SetParticleColor;
        MainVisualizer.LineWidthChanged += SetLineWidth;
        MainVisualizer.MeshLinesChanged += SetMeshLines;
        MainVisualizer.ConnectionDistanceChanged += SetConnectionDistance;
        MainVisualizer.StrongDistanceChanged += SetStrongDistance;
        MainVisualizer.AlphaCurveChanged += SetAlphaCurve;
        MainVisualizer.LineColorChanged += SetLineColor;
        MainVisualizer.ShowLinesChanged += SetShowLines;
        MainVisualizer.ShowTrianglesChanged += SetShowTriangles;
        MainVisualizer.ClearColorChanged += SetClearColor;
        MainVisualizer.TriangleFillOpacityChanged += SetTriangleFillOpacity;
        MainVisualizer.ParticleSpriteChanged += SetParticleSprite;
        MainVisualizer.ActualLineColorChanged += SetActualLineColor;

        SetShowParticles(MainVisualizer.ShowParticles);
        SetParticleSize(MainVisualizer.ParticleSize);
        SetParticleColor(MainVisualizer.ParticleColor);
        SetLineWidth(MainVisualizer.LineWidth);
        SetMeshLines(MainVisualizer.MeshLines);
        SetConnectionDistance(MainVisualizer.ConnectionDistance);
        SetStrongDistance(MainVisualizer.StrongDistance);
        SetAlphaCurve(MainVisualizer.AlphaCurve);
        SetLineColor(MainVisualizer.LineColor);
        SetShowLines(MainVisualizer.ShowLines);
        SetShowTriangles(MainVisualizer.ShowTriangles);
        SetClearColor(MainVisualizer.ClearColor);
        SetTriangleFillOpacity(MainVisualizer.TriangleFillOpacity);
        SetParticleSprite(MainVisualizer.ParticleSprite);
        SetActualLineColor(MainVisualizer.ActualLineColor);
    }

    protected virtual void SetShowParticles(bool value) => _showParticles = value;
    protected virtual void SetParticleSize(float value) => _particlesSize = value;
    protected virtual void SetParticleColor(Color value) => _particlesColor = value;
    protected virtual void SetLineWidth(float value) => _lineWidth = value;
    protected virtual void SetMeshLines(bool value) => _meshLines = value;
    protected virtual void SetConnectionDistance(float value) => _connectionDistance = value;
    protected virtual void SetStrongDistance(float value) => _strongDistance = value;
    protected virtual void SetAlphaCurve(AnimationCurve value) => _alphaCurve = value;
    protected virtual void SetLineColor(Gradient value) => _lineColor = value; 
    protected virtual void SetShowLines(bool value) => _showLines = value;
    protected virtual void SetShowTriangles(bool value) => _showTriangles = value;
    protected virtual void SetClearColor(Color value) => _clearColor = value;
    protected virtual void SetTriangleFillOpacity(float value) => _triangleFillOpacity = value;
    protected virtual void SetParticleSprite(Texture2D value) => _particleSprite = value;
    protected virtual void SetActualLineColor(Gradient value) => _actualLineColor = value;
}
