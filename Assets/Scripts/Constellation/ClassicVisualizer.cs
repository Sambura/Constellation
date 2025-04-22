using UnityEngine;
using Core;
using SimpleGraphics;

public class ClassicVisualizer : VisualizerBase
{
    [Header("Rendering")]
    [SerializeField] private ImmediateBatchRenderer _renderer;
    [SerializeField] private Viewport _viewport;
    [SerializeField] private Material _particleMaterial;
    [SerializeField] private int _renderQueueIndex = 0;
    // [SerializeField] private GameObject _rendererObject;

    // precomputed values for faster rendering
    private float _connectionDistanceSquared;
    private float _lineIntensityDenominator;
    private float _triangleColorOffset;
    private float _triangleColorCoefficient;
    private float _halfParticleSize;

    // temp storage
    private FastList<Particle>[] _neighbours = new FastList<Particle>[4];

    private SimpleDrawBatch _backgroundBatch;
    private SimpleDrawBatch _mainBatch;
    private SimpleDrawBatch _particleBatch;

    public override void SetClearColorBufferEnabled(bool enabled) {
        _renderer.ClearColor = enabled;
    }

    private void RecalculateIntermediates() {
        _connectionDistanceSquared = _connectionDistance * _connectionDistance;
        _lineIntensityDenominator = _connectionDistance - _strongDistance;
        _triangleColorCoefficient = _triangleFillOpacity / _lineIntensityDenominator;
        _triangleColorOffset = 1 - _triangleFillOpacity;
    }

    protected override void SetConnectionDistance(float value) {
        base.SetConnectionDistance(value);
        RecalculateIntermediates();
    }

    protected override void SetStrongDistance(float value) {
        base.SetStrongDistance(value);
        RecalculateIntermediates();
    }

    protected override void SetTriangleFillOpacity(float value) {
        base.SetTriangleFillOpacity(value);
        RecalculateIntermediates();
    }

    protected override void SetParticleSize(float value)
    {
        base.SetParticleSize(value);
        _halfParticleSize = value / 2;
    }

    protected override void SetParticleSprite(Texture2D value)
    {
        base.SetParticleSprite(value);

        _particleMaterial.mainTexture = value;
        // _renderer.SetParticleSprite(value);
    }

    protected override void SetClearColor(Color value) {
        base.SetClearColor(value);

        _backgroundBatch.quads[0].color = value;
    }

    private void UpdateBackgroundQuad() {
        float extents = 0.1f;
        float width = _viewport.MaxX + extents;
        float height = _viewport.MaxY + extents;
        _backgroundBatch.quads[0].SetCoords(-width, -height, -width, height, width, height, width, -height);
    }

    protected override void Awake()
    {
        _backgroundBatch = new SimpleDrawBatch
        {
            quads = new FastList<QuadEntry>()
        };
        _mainBatch = new SimpleDrawBatch
        {
            lines = new FastList<LineEntry>(),
            meshLines = new FastList<MeshLineEntry>(),
            triangles = new FastList<TriangleEntry>()
        };
        _particleBatch = new SimpleDrawBatch
        {
            quads = new FastList<QuadEntry>(),
            material = _particleMaterial
        };
        _backgroundBatch.quads.Add(new QuadEntry());

        //_renderer = _rendererObject.GetComponent<ImmediateBatchDirectRenderer>();
        //_renderer = new ImmediateBatchDirectRenderer();
        base.Awake();

        _renderer.AddBatch(_renderQueueIndex, _backgroundBatch);
        _renderer.AddBatch(_renderQueueIndex + 1, _mainBatch);
        _renderer.AddBatch(_renderQueueIndex + 2, _particleBatch);

        //_renderer.Initialize(MainVisualizer);
    }

    protected override void OnEnable() {
        base.OnEnable();

        _viewport.CameraDimensionsChanged += UpdateBackgroundQuad;
        _renderer.enabled = true;
        UpdateBackgroundQuad();
    }

    protected override void OnDisable() {
        base.OnDisable();

        _viewport.CameraDimensionsChanged -= UpdateBackgroundQuad;
        if (_renderer != null) _renderer.enabled = false;
    }

    private void Update()
    {
        _mainBatch.lines.PseudoClear();
        _mainBatch.triangles.PseudoClear();
        _mainBatch.meshLines.PseudoClear();
        _particleBatch.quads.PseudoClear();

        // if (_showParticles)
        // {
        //     List<Particle> particles = ParticleController.Particles;
        //     for (int i = 0; i < particles.Count; i++)
        //     {
        //         Vector2 pos = particles[i].Position;
        //         // _newRenderer.DrawParticle(pos.x, pos.y);
        //         // _newRenderer.DrawParticle(pos.x, pos.y, _particlesSize, Color.white);
        //     }
        // }
        // 
        // if (!_showLines && !_showTriangles)
        // {
        //     // _newRenderer.SubmitDrawCall();
        //     return;
        // }

        var regionMap = ParticleController.RegionMap;
        int maxSquareX = ParticleController.MaxSquareX;
        int maxSquareY = ParticleController.MaxSquareY;

        for (int ry = 0; ry <= maxSquareY; ry++)
        {
            for (int rx = 0; rx <= maxSquareX; rx++)
            {
                FastList<Particle> current = regionMap[rx, ry];
                if (current._count == 0) continue;

                int xFrom = rx == 0 ? 0 : rx - 1, xTo = rx == maxSquareX ? rx : rx + 1;
                int yFrom = ry, yTo = ry == maxSquareY ? ry : ry + 1;

                // Draw lines between a point in the current cell and the point in one of the 4 neighbour cells
                // . - skipped, C - current cell, # - *neighbour* cell
                //
                // ......
                // ..CC##
                // ######
                //
                int neighbourCount = 0;
                for (int x = xFrom; x <= xTo; x++)
                {
                    for (int y = yFrom; y <= yTo; y++)
                    {
                        if (x <= rx && y == ry) continue;
                        _neighbours[neighbourCount] = regionMap[x, y];
                        neighbourCount++;
                    }
                }

                if (_showParticles)
                {
                    for (int i = 0; i < current._count; i++)
                    {
                        Vector2 pos = current[i].Position;
                        float hs = _halfParticleSize;
                        float x = pos.x, y = pos.y;
                        //_renderer.DrawParticleSimple(pos.x, pos.y);
                        _particleBatch.quads.Add(new QuadEntry(x - hs, y + hs, x - hs, y - hs, x + hs, y - hs, x + hs, y + hs, _particlesColor));
                    }
                }

                if (_showLines)
                {
                    // Draw lines between two points in the current cell
                    for (int i = 0; i < current._count; i++)
                        for (int j = i + 1; j < current._count; j++)
                            DrawLine(current[i], current[j]);

                    for (int neighbourIndex = 0; neighbourIndex < neighbourCount; neighbourIndex++)
                    {
                        FastList<Particle> neighbour = _neighbours[neighbourIndex];

                        for (int j = 0; j < neighbour._count; j++)
                            for (int i = 0; i < current._count; i++)
                                DrawLine(current[i], neighbour[j]);
                    }
                }

                if (_showTriangles)
                {
                    bool zerosNeighbour = neighbourCount == 4;

                    for (int i = 0; i < current._count; i++)
                    {
                        for (int j = i + 1; j < current._count; j++)
                        {
                            // Triangles inside the current cell
                            // no heuristics available
                            for (int k = j + 1; k < current._count; k++)
                                DrawTriangle(current[i], current[j], current[k]);
                            // Triangles with 2 points inside the current cell
                            // no heuristics available
                            for (int neighbourIndex = 0; neighbourIndex < neighbourCount; neighbourIndex++)
                            {
                                FastList<Particle> neighbour = _neighbours[neighbourIndex];

                                for (int k = 0; k < neighbour._count; k++)
                                    DrawTriangle(current[i], current[j], neighbour[k]);
                            }
                        }
                        // Triangles with 1 point inside the current cell

                        // Triangles with 2 points in the same neighbour cell
                        for (int nIndex1 = 0; nIndex1 < neighbourCount; nIndex1++)
                        {
                            FastList<Particle> neighbour1 = _neighbours[nIndex1];

                            for (int j = 0; j < neighbour1._count; j++)
                                for (int k = j + 1; k < neighbour1._count; k++)
                                    DrawTriangle(current[i], neighbour1[j], neighbour1[k]);
                        }

                        // Triangles with 2 points in 2 different neighbour cells
                        for (int nIndex1 = 0; nIndex1 < neighbourCount; nIndex1++)
                        {
                            FastList<Particle> neighbour1 = _neighbours[nIndex1];

                            for (int nIndex2 = nIndex1 + 1; nIndex2 < neighbourCount; nIndex2++)
                            {
                                // Cells that are 1 unit apart cannot connect
                                if (zerosNeighbour && nIndex2 > 1) continue;
                                FastList<Particle> neighbour2 = _neighbours[nIndex2];

                                for (int j = 0; j < neighbour1._count; j++)
                                    for (int k = 0; k < neighbour2._count; k++)
                                        DrawTriangle(current[i], neighbour1[j], neighbour2[k]);
                            }

                            zerosNeighbour = false;
                        }
                    }
                }
            }
        }

        // _renderer.SubmitFrame();
    }

    private void DrawLine(Particle p1, Particle p2)
    {
        Vector3 pos1 = p1.Position, pos2 = p2.Position;

        float diffX = pos1.x - pos2.x;
        float diffY = pos1.y - pos2.y;
        float distanceSquared = diffX * diffX + diffY * diffY;
        if (distanceSquared > _connectionDistanceSquared) return;
        float distance = (float)System.Math.Sqrt(distanceSquared);

        float intensity = (distance - _strongDistance) / _lineIntensityDenominator;
        Color color = _actualLineColor.Evaluate(intensity);
        color.a = _alphaCurve.Evaluate(intensity);

        if (_meshLines)
            // _renderer.DrawWideLine(pos1.x, pos1.y, pos2.x, pos2.y, color, _lineWidth);
            _mainBatch.meshLines.Add(new MeshLineEntry(pos1.x, pos1.y, pos2.x, pos2.y, _lineWidth, color));
        else
            // _renderer.DrawLine(pos1.x, pos1.y, pos2.x, pos2.y, color);
            _mainBatch.lines.Add(new LineEntry(pos1.x, pos1.y, pos2.x, pos2.y, color));
    }

    private void DrawTriangle(Particle p1, Particle p2, Particle p3)
    {
        Vector3 pos1 = p1.Position, pos2 = p2.Position, pos3 = p3.Position;

        float diffX1 = pos1.x - pos2.x, diffY1 = pos1.y - pos2.y;
        float side1Sqr = diffX1 * diffX1 + diffY1 * diffY1;
        if (side1Sqr > _connectionDistanceSquared) return;
        float diffX2 = pos1.x - pos3.x, diffY2 = pos1.y - pos3.y;
        float side2Sqr = diffX2 * diffX2 + diffY2 * diffY2;
        if (side2Sqr > _connectionDistanceSquared) return;
        float diffX3 = pos2.x - pos3.x, diffY3 = pos2.y - pos3.y;
        float side3Sqr = diffX3 * diffX3 + diffY3 * diffY3;
        if (side3Sqr > _connectionDistanceSquared) return;

        float maxSideSqr = System.Math.Max(System.Math.Max(side1Sqr, side2Sqr), side3Sqr);
        float maxSide = (float)System.Math.Sqrt(maxSideSqr);

        float intensity = _triangleColorOffset + _triangleColorCoefficient * (maxSide - _strongDistance);

        Color color = _actualLineColor.Evaluate(intensity);
        color.a = _alphaCurve.Evaluate(intensity);

        // _renderer.DrawTriangle(pos1.x, pos1.y, pos2.x, pos2.y, pos3.x, pos3.y, color);
        _mainBatch.triangles.Add(new TriangleEntry(
            pos1.x, pos1.y, pos2.x, pos2.y, pos3.x, pos3.y, color
        ));
    }
}
