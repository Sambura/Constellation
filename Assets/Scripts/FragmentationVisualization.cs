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
    [SerializeField] private bool _showBounds = false;
    [SerializeField] private Color _boundsColor = Color.blue;
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
    [ConfigGroupToggle(3)] [ConfigGroupMember]
    [ConfigProperty]
    public bool ShowBounds
    {
        get => _showBounds;
        set { if (_showBounds != value) { SetShowBounds(value); ShowBoundsChanged?.Invoke(value); }; }
    }
    [ConfigGroupMember(3, 0)]
    [ColorPickerButtonProperty(true, "Select color", "Color")]
    public Color BoundsColor
    {
        get => _boundsColor;
        set { if (_boundsColor != value) { _boundsColor = value; BoundsColorChanged?.Invoke(value); }; }
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
    public event Action<bool> ShowBoundsChanged;
    public event Action<Color> BoundsColorChanged;
    public event Action<bool> ShowVelocitiesChanged;
    public event Action<Color> VelocityColorChanged;

    private int _maxLinesForBounds = 200;
    private bool _boundsFlash = false;
    private Coroutine _boundsFlashCoroutine;
    private AnalyticsCore _analyticsCore;
    private List<BoundsParticleEffector> _registeredEffectors = new List<BoundsParticleEffector>(); 

    private void SetShowCellBorders(bool value)
    {
        _showCellBorders = value;
        UpdateRenderBatch();
        OnFragmentSizeChanged(_fragmentator.FragmentSize);
    }

    private void SetShowCells(bool value)
    {
        _showCells = value;
        UpdateRenderBatch();
        OnFragmentSizeChanged(_fragmentator.FragmentSize);
    }

    private void SetShowBounds(bool value)
    {
        _showBounds = value;
        UpdateRenderBatch();
        OnFragmentSizeChanged(_fragmentator.FragmentSize);
    }

    private void SetShowVelocities(bool value)
    {
        _showVelocities = value;
        UpdateRenderBatch();
        OnFragmentSizeChanged(_fragmentator.FragmentSize);
    }

    private void OnFragmentSizeChanged(float fragmentSize) {
        int count = _showBounds || _boundsFlash ? _maxLinesForBounds : 0;
        count *= _registeredEffectors.Count;
        if (_showCellBorders) {
            count += Mathf.CeilToInt((float)_fragmentator.Viewport.Width / _fragmentator.FragmentSize) + 2;
            count += Mathf.CeilToInt((float)_fragmentator.Viewport.Height / _fragmentator.FragmentSize) + 2;
        }
        if (_showVelocities) count += _fragmentator.ParticleCount * 3;
        _newRenderer.ReserveLineCount(_lineMat, count);
        _newRenderer.ReserveQuadCount(_cellMat, _showCells ? _fragmentator.CellCount : 0);
    }

    private void UpdateRenderBatch() => this.enabled = _showCellBorders || _showCells || _showBounds || _showVelocities;

    // private void OnEnable() =>_renderer.AddBatch(_renderQueueIndex, _renderBatch);
    // private void OnDisable() => _renderer.RemoveBatch(_renderQueueIndex, _renderBatch);

    private void OnEnable() { if (_newRenderer != null) _newRenderer.enabled = true; }
    private void OnDisable() { if (_newRenderer != null) _newRenderer.enabled = false; }

    private void Awake()
    {
        _renderBatch = new SimpleDrawBatch();
        _renderBatch.lines = new FastList<LineEntry>();
        _renderBatch.quads = new FastList<QuadEntry>();

        UpdateRenderBatch();
        _fragmentator.FragmentSizeChanged += OnFragmentSizeChanged;
        _fragmentator.ParticleEffectorsChanged += OnEffectorsChanged;
        OnFragmentSizeChanged(_fragmentator.FragmentSize);
        _analyticsCore = FindFirstObjectByType<AnalyticsCore>(FindObjectsInactive.Include);
        OnEffectorsChanged(_fragmentator.ParticleEffectors);
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
        if (_boundsFlashCoroutine is { }) StopCoroutine(_boundsFlashCoroutine);
        _boundsFlashCoroutine = StartCoroutine(BoundsFlash());
        this.enabled = true;
    }

    private IEnumerator BoundsFlash() {
        _boundsFlash = true;
        OnFragmentSizeChanged(_fragmentator.FragmentSize);
        yield return new WaitForSeconds(0.25f);
        _boundsFlash = false;
        yield return null; // ObjectMeshRenderer is garbage so we need to wait here not to break it
        // to set enabled = false; and destroy any drawings not needed anymore
        UpdateRenderBatch();
        OnFragmentSizeChanged(_fragmentator.FragmentSize);
        _boundsFlashCoroutine = null;
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

        if (_showBounds || _boundsFlash)
        {
            foreach (var bounds in _registeredEffectors) {
                if (bounds is RectangularBoundParticleEffector)
                {
                    (float left, float right, float bottom, float top) = (-bounds.HorizontalBase, bounds.HorizontalBase, -bounds.VerticalBase, bounds.VerticalBase);
                    // _renderBatch.lines.Add(new LineEntry(left, bottom, left, top, _boundsColor));
                    // _renderBatch.lines.Add(new LineEntry(right, bottom, right, top, _boundsColor));
                    // _renderBatch.lines.Add(new LineEntry(left, bottom, right, bottom, _boundsColor));
                    // _renderBatch.lines.Add(new LineEntry(left, top, right, top, _boundsColor));
                    _newRenderer.DrawLine(left, bottom, left, top, _lineMat, _boundsColor);
                    _newRenderer.DrawLine(right, bottom, right, top, _lineMat, _boundsColor);
                    _newRenderer.DrawLine(left, bottom, right, bottom, _lineMat, _boundsColor);
                    _newRenderer.DrawLine(left, top, right, top, _lineMat, _boundsColor);
                } else if (bounds is EllipticalBoundParticleEffector)
                {
                    int pointCount = _maxLinesForBounds;
                    Vector2 prev = new Vector2(bounds.HorizontalBase, 0);

                    for (int i = 1; i <= pointCount; i++) {
                        Vector2 pos = GetEllipsePoint(Mathf.PI * 2 * i / pointCount);
                        _newRenderer.DrawLine(prev.x, prev.y, pos.x, pos.y, _lineMat, _boundsColor);
                        prev = pos;
                    }

                    Vector2 GetEllipsePoint(float angle)
                    {
                        float a = bounds.HorizontalBase;
                        float b = bounds.VerticalBase;
                        float s = Mathf.Sin(angle);
                        float c = Mathf.Cos(angle);

                        float d = Mathf.Sqrt(Mathf.Pow(a * s, 2) + Mathf.Pow(b * c, 2));
                        return new Vector2(c, s) * a * b / d;
                    }
                }
            }
        }

        if (_showVelocities) {
            List<Particle> particles = _fragmentator.Particles;
            for (int i = 0, count = particles.Count; i < count; i++) {
                Vector2 source = particles[i].Position;
                float speed = particles[i].Velocity.magnitude;
                float size = Mathf.Log(speed + 1.2f) / Mathf.Log(2) / 5;
                Vector2 dest = particles[i].Position + size * particles[i].Velocity / speed;
                Vector2 direction = -particles[i].Velocity / speed * size / 4;
                Vector2 normal = new Vector2(-direction.y, direction.x) * 0.7f;
                Vector2 arrow1 = dest + normal + direction;
                Vector2 arrow2 = dest - normal + direction;

                _newRenderer.DrawLine(source.x, source.y, dest.x, dest.y, _lineMat, _velocityColor);
                _newRenderer.DrawLine(arrow1.x, arrow1.y, dest.x, dest.y, _lineMat, _velocityColor);
                _newRenderer.DrawLine(arrow2.x, arrow2.y, dest.x, dest.y, _lineMat, _velocityColor);
            }
        }
    }
}