using UnityEngine;
using System;
using Core;
using SimpleGraphics;
using ConfigSerialization;
using ConfigSerialization.Structuring;
using System.Collections.Generic;
using System.Collections;

public class FragmentationVisualization : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private ImmediateBatchRenderer _renderer;
    [SerializeField] private ObjectMeshRenderer _newRenderer;
    [SerializeField] private ParticleController _fragmentator;
    [SerializeField] private Material _lineMat;
    [SerializeField] private Material _cellMat;
    // [SerializeField] private int _renderQueueIndex = 1000;

    [Header("Debug")]
    [SerializeField] private bool _showCellBorders = false;
    [SerializeField] private Color _cellBorderColor = Color.red;
    [SerializeField] private bool _showCells = false;
    [SerializeField] private Color _cellColor = Color.yellow;
    [SerializeField] private bool _showVelocities = false;
    [SerializeField] private Color _velocityColor = Color.green;

    private SimpleDrawBatch _renderBatch;

    [ConfigGroupToggle(1)] [ConfigGroupMember("Fragmentation visualization")]
    [ConfigProperty]
    public bool ShowCellBorders
    {
        get => _showCellBorders;
        set { if (_showCellBorders != value) { SetShowCellBorders(value); ShowCellBordersChanged?.Invoke(value); }; }
    }
    [ConfigGroupMember(1, 0)]
    [ColorPickerButtonProperty(true, "Select color", "Color")]
    public Color CellBorderColor
    {
        get => _cellBorderColor;
        set { if (_cellBorderColor != value) { _cellBorderColor = value; CellBorderColorChanged?.Invoke(value); }; }
    }
    [ConfigGroupToggle(2)] [ConfigGroupMember]
    [ConfigProperty]
    public bool ShowCells
    {
        get => _showCells;
        set { if (_showCells != value) { SetShowCells(value); ShowCellsChanged?.Invoke(value); }; }
    }
    [ConfigGroupMember(2, 0)]
    [ColorPickerButtonProperty(true, "Select color", "Color")]
    public Color CellColor
    {
        get => _cellColor;
        set { if (_cellColor != value) { _cellColor = value; CellColorChanged?.Invoke(value); }; }
    }
    [ConfigGroupToggle(4)]
    [ConfigGroupMember]
    [ConfigProperty]
    public bool ShowVelocities
    {
        get => _showVelocities;
        set { if (_showVelocities != value) { SetShowVelocities(value); ShowVelocitiesChanged?.Invoke(value); }; }
    }
    [ConfigGroupMember(4, 0)]
    [ColorPickerButtonProperty(true, "Select color", "Color")]
    public Color VelocityColor
    {
        get => _velocityColor;
        set { if (_velocityColor != value) { _velocityColor = value; VelocityColorChanged?.Invoke(value); }; }
    }
    /// <summary>
    /// Assume this is only set by user through UI
    /// </summary>
    [ConfigGroupMember]
    [ConfigProperty(hasEvent: false, AllowPolling = false)]
    [Core.Json.NoJsonSerialization(AllowFromJson = true)]
    public bool VisualizeBoundsChange { get; set; } = true;

    public event Action<bool> ShowCellBordersChanged;
    public event Action<Color> CellBorderColorChanged;
    public event Action<bool> ShowCellsChanged;
    public event Action<Color> CellColorChanged;
    public event Action<bool> ShowVelocitiesChanged;
    public event Action<Color> VelocityColorChanged;

    private AnalyticsCore _analyticsCore;
    private readonly List<BoundsParticleEffector> _registeredEffectors = new();
    private readonly List<BoundsParticleEffector> _flashingBounds = new();

    private void SetShowCellBorders(bool value)
    {
        _showCellBorders = value;
        UpdateRenderBatch();
    }

    private void SetShowCells(bool value)
    {
        _showCells = value;
        UpdateRenderBatch();
    }

    private void SetShowVelocities(bool value)
    {
        _showVelocities = value;
        UpdateRenderBatch();
    }

    private void UpdateRenderBatch() => this.enabled = _showCellBorders || _showCells || _showVelocities;

    // private void OnEnable() =>_renderer.AddBatch(_renderQueueIndex, _renderBatch);
    // private void OnDisable() => _renderer.RemoveBatch(_renderQueueIndex, _renderBatch);

    private void Awake()
    {
        _renderBatch = new SimpleDrawBatch();
        _renderBatch.lines = new FastList<LineEntry>();
        _renderBatch.quads = new FastList<QuadEntry>();

        UpdateRenderBatch();
        _fragmentator.ParticleEffectorsChanged += OnEffectorsChanged;
        _analyticsCore = FindFirstObjectByType<AnalyticsCore>(FindObjectsInactive.Include);
        OnEffectorsChanged(_fragmentator.ParticleEffectors);
    }

    private void Start()
    {
        _newRenderer ??= ObjectMeshRenderer.Instance;
    }

    private void OnEffectorsChanged(List<EffectorModule> modules)
    {
        var newBoundEffectors = _fragmentator.ActiveBoundsEffectors;
        int matches = 0;

        // compare old and new bound effectors. If same - do nothing
        foreach (var effector in newBoundEffectors) {
            if (_registeredEffectors.Contains(effector))
                matches++;
        }
        if (_registeredEffectors.Count == newBoundEffectors.Count && newBoundEffectors.Count == matches) return;

        // bound effectors changed: rebuild registered list
        foreach (var effector in _registeredEffectors)
            effector.BoundsChanged -= OnBoundsChanged;

        _registeredEffectors.Clear();
        _registeredEffectors.AddRange(newBoundEffectors);

        foreach (var effector in newBoundEffectors)
            effector.BoundsChanged += OnBoundsChanged;

        OnBoundsChanged();
    }

    private void OnBoundsChanged() {
        if (!VisualizeBoundsChange || _analyticsCore.BenchmarkInProgress) return;
        StartCoroutine(BoundsFlash());
        this.enabled = true;
    }

    private IEnumerator BoundsFlash() {
        int effectorCount = _registeredEffectors.Count;
        _flashingBounds.AddRange(_registeredEffectors);
        foreach (var bounds in _registeredEffectors) bounds.ForceShowBounds = true;
        yield return new WaitForSeconds(0.25f);
        // assuming our bounds are first in the list (which should be true i think?)
        for (int i = 0; i < effectorCount; i++) {
            _flashingBounds[0].ForceShowBounds = false;
            _flashingBounds.RemoveAt(0);
        }
        foreach (var bounds in _flashingBounds) bounds.ForceShowBounds = true;
    }

    private void Update()
    {
        _renderBatch.lines.PseudoClear();
        _renderBatch.quads.PseudoClear();

        float connectionDistance = _fragmentator.FragmentSize;

        if (_showCells)
        {
            var regionMap = _fragmentator.RegionMap;
            int maxSquareX = _fragmentator.MaxSquareX;
            int maxSquareY = _fragmentator.MaxSquareY;
            int xSquareOffset = _fragmentator.XSquareOffset;
            int ySquareOffset = _fragmentator.YSquareOffset;
            float averagePerCell = _fragmentator.AveragePerCell;

            for (int ry = 0; ry <= maxSquareY; ry++) {
                for (int rx = 0; rx <= maxSquareX; rx++) {
                    int currentCount = regionMap[rx, ry]._count;
                    float x = (rx - xSquareOffset) * connectionDistance;
                    float y = (ry - ySquareOffset) * connectionDistance;
                    Color currentColor = _cellColor;
                    currentColor.a *= currentCount / averagePerCell;
                    // _renderBatch.quads.Add(new QuadEntry(
                    //     x, y, x + connectionDistance, y, x + connectionDistance, y + connectionDistance, x, y + connectionDistance, currentColor
                    //     ));
                    _newRenderer.DrawQuad(x, y, x + connectionDistance, y, x + connectionDistance, y + connectionDistance, x, y + connectionDistance, _cellMat, currentColor);
                }
            }
        }

        if (_showCellBorders)
        {
            Viewport viewport = _fragmentator.Viewport;
            float maxX = viewport.MaxX, maxY = viewport.MaxY;

            for (float x = -Mathf.Floor(maxX / connectionDistance) * connectionDistance; x < maxX; x += connectionDistance)
            {
                //_renderBatch.lines.Add(new LineEntry(x, -maxY, x, maxY, _cellBorderColor));
                _newRenderer.DrawLine(x, -maxY, x, maxY, _lineMat, _cellBorderColor);
            }

            for (float y = -Mathf.Floor(maxY / connectionDistance) * connectionDistance; y < maxY; y += connectionDistance)
            {
                //_renderBatch.lines.Add(new LineEntry(-maxX, y, maxX, y, _cellBorderColor));
                _newRenderer.DrawLine(-maxX, y, maxX, y, _lineMat, _cellBorderColor);
            }
        }

        if (_showVelocities) {
            List<Particle> particles = _fragmentator.Particles;
            for (int i = 0, count = particles.Count; i < count; i++) {
                Vector2 source = particles[i].Position;
                float speed = particles[i].Velocity.magnitude;
                float size = Mathf.Log(speed + 1.2f) / Mathf.Log(2) / 5;

                _newRenderer.DrawArrow(source, (size / speed) * particles[i].Velocity, _lineMat, _velocityColor);
            }
        }
    }
}