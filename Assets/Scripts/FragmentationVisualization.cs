using UnityEngine;
using System;
using Core;
using SimpleGraphics;
using ConfigSerialization;
using ConfigSerialization.Structuring;

public class FragmentationVisualization : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private ImmediateBatchRenderer _renderer;
    [SerializeField] private ObjectMeshRenderer _newRenderer;
    [SerializeField] private ParticleController _fragmentator;
    [SerializeField] private Material _lineMat;
    [SerializeField] private Material _cellMat;
    [SerializeField] private int _renderQueueIndex = 1000;

    [Header("Debug")]
    [SerializeField] private bool _showCellBorders = false;
    [SerializeField] private Color _cellBorderColor = Color.red;
    [SerializeField] private bool _showCells = false;
    [SerializeField] private Color _cellColor = Color.yellow;
    [SerializeField] private bool _showBounds = false;
    [SerializeField] private Color _boundsColor = Color.blue;

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

    public event Action<bool> ShowCellBordersChanged;
    public event Action<Color> CellBorderColorChanged;
    public event Action<bool> ShowCellsChanged;
    public event Action<Color> CellColorChanged;
    public event Action<bool> ShowBoundsChanged;
    public event Action<Color> BoundsColorChanged;

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

    private void OnFragmentSizeChanged(float fragmentSize) {
        int count = _showBounds ? 4 : 0;
        if (_showCellBorders) {
            count += Mathf.CeilToInt((float)_fragmentator.Viewport.Width / _fragmentator.FragmentSize) + 2;
            count += Mathf.CeilToInt((float)_fragmentator.Viewport.Height / _fragmentator.FragmentSize) + 2;
        }
        _newRenderer.ReserveLineCount(_lineMat, count);
        _newRenderer.ReserveQuadCount(_cellMat, _showCells ? _fragmentator.CellCount : 0);
    }

    private void UpdateRenderBatch() => this.enabled = _showCellBorders || _showCells || _showBounds;

    // private void OnEnable() =>_renderer.AddBatch(_renderQueueIndex, _renderBatch);
    // private void OnDisable() => _renderer.RemoveBatch(_renderQueueIndex, _renderBatch);

    private void OnEnable() => _newRenderer.enabled = true;
    private void OnDisable() => _newRenderer.enabled = false;

    private void Awake()
    {
        _renderBatch = new SimpleDrawBatch();
        _renderBatch.lines = new FastList<LineEntry>();
        _renderBatch.quads = new FastList<QuadEntry>();

        UpdateRenderBatch();
        _fragmentator.FragmentSizeChanged += OnFragmentSizeChanged;
        OnFragmentSizeChanged(_fragmentator.FragmentSize);
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

        if (_showBounds)
        {
            (float left, float right, float bottom, float top) = (_fragmentator.BoundLeft, _fragmentator.BoundRight, _fragmentator.BoundBottom, _fragmentator.BoundTop);
            // _renderBatch.lines.Add(new LineEntry(left, bottom, left, top, _boundsColor));
            // _renderBatch.lines.Add(new LineEntry(right, bottom, right, top, _boundsColor));
            // _renderBatch.lines.Add(new LineEntry(left, bottom, right, bottom, _boundsColor));
            // _renderBatch.lines.Add(new LineEntry(left, top, right, top, _boundsColor));
            _newRenderer.DrawLine(left, bottom, left, top, _lineMat, _boundsColor);
            _newRenderer.DrawLine(right, bottom, right, top, _lineMat, _boundsColor);
            _newRenderer.DrawLine(left, bottom, right, bottom, _lineMat, _boundsColor);
            _newRenderer.DrawLine(left, top, right, top, _lineMat, _boundsColor);
        }
    }
}