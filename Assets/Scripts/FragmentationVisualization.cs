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
    [SerializeField] private ParticleController _fragmentator;
    [SerializeField] private int _renderQueueIndex = 1000;

    [Header("Debug")]
    [SerializeField] private bool _showCellBorders = false;
    [SerializeField] private Color _cellBorderColor = Color.red;
    [SerializeField] private bool _showCells = false;
    [SerializeField] private Color _cellColor = Color.yellow;
    [SerializeField] private bool _showBounds = false;
    [SerializeField] private Color _boundsColor = Color.blue;

    private SimpleDrawBatch _renderBatch;
    // Is _renderBatch added to the renderer?
    private bool _isRendered = false;

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
    }

    private void SetShowCells(bool value)
    {
        _showCells = value;
        UpdateRenderBatch();
    }

    private void SetShowBounds(bool value)
    {
        _showBounds = value;
        UpdateRenderBatch();
    }

    private void UpdateRenderBatch()
    {
        bool needsRendering = _showCellBorders || _showCells || _showBounds;

        if (needsRendering && !_isRendered)
        {
            _isRendered = true;
            _renderer.AddBatch(_renderQueueIndex, _renderBatch);
        }

        if (!needsRendering && _isRendered)
        {
            _isRendered = false;
            _renderer.RemoveBatch(_renderQueueIndex, _renderBatch);
        }
    }

    private void Awake()
    {
        _renderBatch = new SimpleDrawBatch();
        _renderBatch.lines = new FastList<LineEntry>();
        _renderBatch.quads = new FastList<QuadEntry>();

        UpdateRenderBatch();
    }

    private void Update()
    {
        if (!_isRendered) return;

        _renderBatch.lines.PseudoClear();
        _renderBatch.quads.PseudoClear();

        float connectionDistance = _fragmentator.ConnectionDistance;

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

                    if (_showCells)
                    {
                        float x = (rx - xSquareOffset) * connectionDistance;
                        float y = (ry - ySquareOffset) * connectionDistance;
                        Color currentColor = _cellColor;
                        currentColor.a *= currentCount / averagePerCell;
                        _renderBatch.quads.Add(new QuadEntry(
                            x, y, x + connectionDistance, y, x + connectionDistance, y + connectionDistance, x, y + connectionDistance, currentColor
                            ));
                    }
                }
            }
        }

        if (_showCellBorders)
        {
            Viewport viewport = _fragmentator.Viewport;
            float maxX = viewport.MaxX, maxY = viewport.MaxY;

            for (float x = -Mathf.Floor(maxX / connectionDistance) * connectionDistance; x < maxX; x += connectionDistance)
            {
                _renderBatch.lines.Add(new LineEntry(x, -maxY, x, maxY, _cellBorderColor));
            }

            for (float y = -Mathf.Floor(maxY / connectionDistance) * connectionDistance; y < maxY; y += connectionDistance)
            {
                _renderBatch.lines.Add(new LineEntry(-maxX, y, maxX, y, _cellBorderColor));
            }
        }

        if (_showBounds)
        {
            (float left, float right, float bottom, float top) = (_fragmentator.BoundLeft, _fragmentator.BoundRight, _fragmentator.BoundBottom, _fragmentator.BoundTop);
            _renderBatch.lines.Add(new LineEntry(left, bottom, left, top, _boundsColor));
            _renderBatch.lines.Add(new LineEntry(right, bottom, right, top, _boundsColor));
            _renderBatch.lines.Add(new LineEntry(left, bottom, right, bottom, _boundsColor));
            _renderBatch.lines.Add(new LineEntry(left, top, right, top, _boundsColor));
        }
    }
}