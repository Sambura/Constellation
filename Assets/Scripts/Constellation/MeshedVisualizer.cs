using UnityEngine;
using Core;
using System.Collections.Generic;
using UnityEngine.Rendering;
using static UnityCore.Utility;

public class MeshedVisualizer : VisualizerBase
{
    [Header("Rendering")]
    [SerializeField] private Camera _camera;
    [SerializeField] private Viewport _viewport;
    [SerializeField] private Material _particleMat;
    [SerializeField] private Material _lineMat;
    [SerializeField] private Material _meshLineMat;
    [SerializeField] private Material _triangleMat;
    [SerializeField] private Material _backgroundMat;

    // precomputed values for faster rendering
    private float _connectionDistanceSquared;
    private float _lineIntensityDenominator;
    private float _triangleColorOffset;
    private float _triangleColorCoefficient;
    private float _halfParticleSize;
    private float _actualLineWidth;

    // temp storage
    private FastList<Particle>[] _neighbours = new FastList<Particle>[4];

    struct RenderUnit
    {
        public Mesh Mesh;
        public GameObject Parent;
        public ColoredVertex[] VertexBuffer;
    }

    // background rendering
    private MeshFilter _background;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct ColoredVertex
    {
        public float x;
        public float y;
        public Color color;
    }

    // particle rendering
    private int _vertexIndex;
    private int _unitIndex;
    private List<RenderUnit> _renderUnits = new List<RenderUnit>();

    // line rendering
    private int _linIndex;
    private int _linUnitIndex;
    private int _lastLinIndex;
    private int _lastLinUnitIndex;
    private List<RenderUnit> _linRenderUnits = new List<RenderUnit>();

    // triangle rendering
    private int _triIndex;
    private int _triUnitIndex;
    private int _lastTriIndex;
    private int _lastTriUnitIndex;
    private List<RenderUnit> _triRenderUnits = new List<RenderUnit>();

    // a bit less than the actual max (it seems it's discouraged to go to the actual max)
    private static readonly int MaxVertexCount = 65200;
    private static readonly int ParticlesPerUnit = MaxVertexCount / 4;
    private static readonly int TrianglesPerUnit = MaxVertexCount / 3;
    private static readonly int MeshLinesPerUnit = TrianglesPerUnit;
    private static readonly int LinesPerUnit = MaxVertexCount / 2;
    private static readonly MeshUpdateFlags UpdateFlags = MeshUpdateFlags.DontValidateIndices |
        MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontNotifyMeshUsers;

    public override void SetClearColorBufferEnabled(bool enabled) {
        _camera.clearFlags = enabled ? CameraClearFlags.SolidColor : CameraClearFlags.Depth;
    }

    protected override void SetParticleCount(int particleCount) {
        base.SetParticleCount(particleCount);

        if (_showParticles) MakeRenderUnits(particleCount);
        if (_showTriangles) SetTriRenderUnitsEnabled(particleCount > 0);
        if (_showLines) SetLinRenderUnitsEnabled(particleCount > 0);
    }

    private void SetTriRenderUnitsEnabled(bool enabled)
    {
        if (true)
        {
            // I have no idea what that is or how it works but its kinda accurate (tuned to over-estimate for ~10-30%)
            int pc = ParticleController.ParticleCount;
            double maxCount = pc * (pc - 1.0) * (pc - 2.0);
            double regularFactor = Mathf.PI * (Mathf.PI - 0.75 * Mathf.Sqrt(3));
            double areaFactor = Mathf.Sqrt(_viewport.Area);
            double sizeFactor = System.Math.Pow(ParticleController.FragmentSize / areaFactor, 4);
            int estimated = Mathf.RoundToInt((float)(maxCount * regularFactor * sizeFactor / 5));
        }

        // We always keep 4 render units with custom triangle counts (as listed at the end of the function)
        // Destroy all others (just because we need to do it somewhere :)
        while (_triRenderUnits.Count > (enabled ? 4 : 0))
        {
            int last = _triRenderUnits.Count - 1;
            Destroy(_triRenderUnits[last].Parent);
            _triRenderUnits.RemoveAt(last);

            if (_lastTriUnitIndex == last)
            {
                _lastTriIndex = 0;
                _lastTriUnitIndex = 0;
            }
        }

        if (!enabled || _triRenderUnits.Count != 0) return;

        // We have these to allow faster rendering for simulations with low triangle count since
        // these have smaller meshes that require less time to be transferred to GPU for rendering.
        // Since the unused units are disabled, we don't lose (much) performance even if we only
        // need 1 unit for displaying all triangles
        AddTriRenderUnit(TrianglesPerUnit / 10);
        AddTriRenderUnit(TrianglesPerUnit / 4);
        AddTriRenderUnit(TrianglesPerUnit / 3);
        AddTriRenderUnit(TrianglesPerUnit / 2);
    }

    private void AddTriRenderUnit(int triangleCount = -1)
    {
        if (triangleCount < 0) triangleCount = TrianglesPerUnit;

        ColoredVertex[] vertexData = new ColoredVertex[3 * triangleCount];
        int[] triangles = new int[3 * triangleCount];

        // Warning: peak procedural mesh generation
        for (int i = 0; i < triangles.Length; i++) triangles[i] = i;

        var mesh = new Mesh();
        mesh.MarkDynamic();
        mesh.SetVertexBufferParams(vertexData.Length, new VertexAttributeDescriptor[] {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension: 2, stream: 0),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, dimension: 4, stream: 0)
        });
        mesh.SetVertexBufferData(vertexData, 0, 0, vertexData.Length, stream: 0, MeshUpdateFlags.DontRecalculateBounds);
        mesh.SetTriangles(triangles, 0, calculateBounds: false);

        GameObject meshObject = MakeFlatMesh(mesh, _triangleMat, transform, "TriRenderUnit");
        _triRenderUnits.Add(new RenderUnit() { Mesh = mesh, Parent = meshObject, VertexBuffer = vertexData });
    }

    private void SetLinRenderUnitsEnabled(bool enabled)
    {
        // See SetTriRenderUnitsEnabled about 4 default render units
        while (_linRenderUnits.Count > (enabled ? 4 : 0))
        {
            int last = _linRenderUnits.Count - 1;
            Destroy(_linRenderUnits[last].Parent);
            _linRenderUnits.RemoveAt(last);

            if (_lastLinUnitIndex == last)
            {
                _lastLinIndex = 0;
                _lastLinUnitIndex = 0;
            }
        }

        if (!enabled || _linRenderUnits.Count != 0) return;

        if (_meshLines) {
            AddMeshLinRenderUnit(MeshLinesPerUnit / 10);
            AddMeshLinRenderUnit(MeshLinesPerUnit / 4);
            AddMeshLinRenderUnit(MeshLinesPerUnit / 3);
            AddMeshLinRenderUnit(MeshLinesPerUnit / 2);
        } else {
            AddLinRenderUnit(LinesPerUnit / 12);
            AddLinRenderUnit(LinesPerUnit / 5);
            AddLinRenderUnit(LinesPerUnit / 4);
            AddLinRenderUnit(LinesPerUnit / 2);
        }
    }

    // About mesh line rendering: this visualizer uses 1 triangle to draw each mesh line. One
    // point is located in triangle's base, while the other is in the middle of the height
    // from the tip vertex to the triangle's base. The triangle's height is 2x the length of
    // the line it renders. The triangle's base is 2x the line's width. This setup ensures
    // maximum triangle efficiency (50% of triangle area is occupied by line pixels).
    // ---
    // Note: there IS a chance that quad setup would be faster, since I didn't test it,
    // but presumably it will only improve GPU time, whereas we already are CPU-bound
    // (and by a large margin)
    private void AddMeshLinRenderUnit(int triangleCount = -1)
    {
        if (triangleCount < 0) triangleCount = MeshLinesPerUnit;

        ColoredVertex[] vertexData = new ColoredVertex[3 * triangleCount];
        int[] triangles = new int[3 * triangleCount];
        Vector2[] uvs = new Vector2[3 * triangleCount];

        for (int i = 0; i < triangleCount; i++)
        {
            triangles[i * 3 + 0] = i * 3 + 0; // Tip vertex
            triangles[i * 3 + 1] = i * 3 + 1; // Base vertex 1
            triangles[i * 3 + 2] = i * 3 + 2; // Base vertex 2

            // The UV values for tip vertex a calculated as u = v = (f + 1) / 2
            // where `f` is the ratio of height of the triangle to the length of line it renders
            // e.g. if we draw a line of length 2, we make a triangle of height 2.2, meaning f = 1.1
            // To get the formula yourself, manually interpolate UV values in starting point of a line (pos1)
            // Also, I did the math, and the optimal f to maximize line area vs. triangle area seems to be f = 2
            uvs[i * 3 + 0] = new Vector2(1.5f, 1.5f);
            uvs[i * 3 + 1] = new Vector2(0, 1);
            uvs[i * 3 + 2] = new Vector2(1, 0);
        }

        var mesh = new Mesh();
        mesh.MarkDynamic();
        mesh.SetVertexBufferParams(vertexData.Length, new VertexAttributeDescriptor[] {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension: 2, stream: 0),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, dimension: 4, stream: 0),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, dimension: 2, stream: 1),
        });
        mesh.SetVertexBufferData(vertexData, 0, 0, vertexData.Length, stream: 0, MeshUpdateFlags.DontRecalculateBounds);
        mesh.SetVertexBufferData(uvs, 0, 0, uvs.Length, stream: 1, MeshUpdateFlags.DontRecalculateBounds);
        mesh.SetTriangles(triangles, 0, calculateBounds: false);

        GameObject meshObject = MakeFlatMesh(mesh, _meshLineMat, transform, "MeshLinRenderUnit");
        _linRenderUnits.Add(new RenderUnit() { Mesh = mesh, Parent = meshObject, VertexBuffer = vertexData });
    }

    private void AddLinRenderUnit(int lineCount = -1)
    {
        if (lineCount < 0) lineCount = LinesPerUnit;

        ColoredVertex[] vertexData = new ColoredVertex[2 * lineCount];
        int[] indices = new int[2 * lineCount];

        for (int i = 0; i < indices.Length; i++) indices[i] = i;

        var mesh = new Mesh();
        mesh.MarkDynamic();
        mesh.SetVertexBufferParams(vertexData.Length, new VertexAttributeDescriptor[] {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension: 2, stream: 0),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, dimension: 4, stream: 0)
        });
        mesh.SetVertexBufferData(vertexData, 0, 0, vertexData.Length, stream: 0, MeshUpdateFlags.DontRecalculateBounds);
        mesh.SetIndices(indices, MeshTopology.Lines, 0, calculateBounds: false);

        GameObject meshObject = MakeFlatMesh(mesh, _lineMat, transform, "LinRenderUnit");
        _linRenderUnits.Add(new RenderUnit() { Mesh = mesh, Parent = meshObject, VertexBuffer = vertexData });
    }

    private void MakeRenderUnits(int particleCount)
    {
        foreach (var unit in _renderUnits)
            Destroy(unit.Parent);

        int renderUnitCount = Mathf.CeilToInt(particleCount / (float)ParticlesPerUnit);
        int currentCount = particleCount;
        _renderUnits.Clear();

        for (int j = 0; j < renderUnitCount; j++)
        {
            int count = Mathf.Min(currentCount, ParticlesPerUnit);
            currentCount -= count;

            ColoredVertex[] vertexData = new ColoredVertex[count * 4];
            Vector2[] uvs = new Vector2[count * 4];
            int[] triangles = new int[count * 6];

            for (int i = 0; i < count; i++)
            {
                uvs[i * 4 + 0] = new Vector2(0, 0);
                uvs[i * 4 + 1] = new Vector2(1, 0);
                uvs[i * 4 + 2] = new Vector2(1, 1);
                uvs[i * 4 + 3] = new Vector2(0, 1);

                triangles[i * 6 + 0] = i * 4 + 2;
                triangles[i * 6 + 1] = i * 4 + 1;
                triangles[i * 6 + 2] = i * 4 + 0;
                triangles[i * 6 + 3] = i * 4 + 3;
                triangles[i * 6 + 4] = i * 4 + 2;
                triangles[i * 6 + 5] = i * 4 + 0;
            }

            var mesh = new Mesh();
            mesh.MarkDynamic();
            mesh.SetVertexBufferParams(vertexData.Length, new VertexAttributeDescriptor[] {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension: 2, stream: 0),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, dimension: 4, stream: 0),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, dimension: 2, stream: 1),
            });
            mesh.SetVertexBufferData(vertexData, 0, 0, vertexData.Length, stream: 0, MeshUpdateFlags.DontRecalculateBounds);
            mesh.SetVertexBufferData(uvs, 0, 0, uvs.Length, stream: 1, MeshUpdateFlags.DontRecalculateBounds);
            mesh.SetTriangles(triangles, 0, calculateBounds: false);

            GameObject meshObject = MakeFlatMesh(mesh, _particleMat, transform, "RenderUnit");
            _renderUnits.Add(new RenderUnit() { Mesh = mesh, Parent = meshObject, VertexBuffer = vertexData });
        }
    }

    private void RecalculateIntermediates()
    {
        _connectionDistanceSquared = _connectionDistance * _connectionDistance;
        _lineIntensityDenominator = _connectionDistance - _strongDistance;
        _triangleColorCoefficient = _triangleFillOpacity / _lineIntensityDenominator;
        _triangleColorOffset = 1 - _triangleFillOpacity;
    }

    protected override void SetConnectionDistance(float value)
    {
        base.SetConnectionDistance(value);
        RecalculateIntermediates();
    }

    protected override void SetStrongDistance(float value)
    {
        base.SetStrongDistance(value);
        RecalculateIntermediates();
    }

    protected override void SetTriangleFillOpacity(float value)
    {
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

        _particleMat.mainTexture = value;
    }

    protected override void SetClearColor(Color value)
    {
        base.SetClearColor(value);

        _backgroundMat.color = value;
    }

    protected override void SetShowTriangles(bool value)
    {
        base.SetShowTriangles(value);

        SetTriRenderUnitsEnabled(value);
    }

    protected override void SetShowLines(bool value)
    {
        base.SetShowLines(value);

        SetLinRenderUnitsEnabled(value);
    }

    protected override void SetShowParticles(bool value)
    {
        base.SetShowParticles(value);

        MakeRenderUnits(value ? ParticleController.ParticleCount : 0);
    }

    protected override void SetLineWidth(float value)
    {
        base.SetLineWidth(value);

        // We draw lines 2x thicker in ClassicVisualizer, so have to do the same here
        _actualLineWidth = value * 2;
    }

    protected override void SetMeshLines(bool value)
    {
        base.SetMeshLines(value);

        // disable to destroy old render units for different rendering mode
        SetLinRenderUnitsEnabled(false);
        SetLinRenderUnitsEnabled(_showLines);
    }

    private void UpdateBackground()
    {
        if (_background is null)
        {
            var newMesh = new Mesh {
                vertices = new Vector3[4],
                triangles = new int[6] { 2, 1, 0, 3, 2, 0 }
            };

            _background = MakeFlatMesh(newMesh, _backgroundMat, transform, "Background", makeStatic: true).GetComponent<MeshFilter>();
        }

        float extents = 0.1f;
        float width = _viewport.MaxX + extents;
        float height = _viewport.MaxY + extents;
        _background.mesh.vertices = new Vector3[4] {
            new Vector3(-width, -height, 0),
            new Vector3(width, -height, 0),
            new Vector3(width, height, 0),
            new Vector3(-width, height, 0)
        };
        _background.mesh.RecalculateBounds();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        _viewport.CameraDimensionsChanged += UpdateBackground;
        UpdateBackground();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        _viewport.CameraDimensionsChanged -= UpdateBackground;
        SetParticleCount(0); // destroys all meshes
        Destroy(_background.gameObject);
        _background = null;
    }

    private void Update() {
        _lastTriIndex = _triIndex;
        _lastTriUnitIndex = _triUnitIndex;
        _lastLinIndex = _linIndex;
        _lastLinUnitIndex = _linUnitIndex;

        _vertexIndex = 0;
        _unitIndex = 0;
        _linIndex = 0;
        _linUnitIndex = 0;
        _triIndex = 0;
        _triUnitIndex = 0;

        // Part of the reason we don't draw particles in that huge loop down below, is that
        // Drawing each cell separately actually creates visible edges when a lot of 
        // transparent particles are drawn on screen (probably related to limited color precision)
        if (_showParticles) {
            List<Particle> particles = ParticleController.Particles;
            for (int i = 0; i < particles.Count; i++)
                DrawParticle(particles[i].Position);
        }

        if (!_showLines && !_showTriangles) {
            SubmitFrame();
            return;
        }

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

        SubmitFrame();
    }

    private void SubmitFrame()
    {
        // submit particle data
        foreach (var unit in _renderUnits)
            unit.Mesh.SetVertexBufferData(unit.VertexBuffer, 0, 0, unit.VertexBuffer.Length, 0, UpdateFlags);

        // submit lines data
        SubmitDynamicRenderUnits(_linRenderUnits, _linIndex, _linUnitIndex, _lastLinIndex, _lastLinUnitIndex);
        // submit triangle data
        SubmitDynamicRenderUnits(_triRenderUnits, _triIndex, _triUnitIndex, _lastTriIndex, _lastTriUnitIndex);

        static void SubmitDynamicRenderUnits(List<RenderUnit> renderUnits, int vIndex, int unitIndex, int lastVIndex, int lastUnitIndex)
        {
            if (renderUnits.Count == 0) return;

            // Activate / deactivate render units
            for (int i = 0; i < renderUnits.Count; i++) {
                bool enabled = i <= unitIndex;
                if (renderUnits[i].Parent.activeSelf == enabled) continue;
                renderUnits[i].Parent.SetActive(enabled);
                if (enabled) continue;

                // If we disable render unit, we must clean it to be ready for further use
                var vertBuffer = renderUnits[i].VertexBuffer;
                int end = i == lastUnitIndex ? lastVIndex : vertBuffer.Length;
                for (int j = 0; j < end; j++) {
                    vertBuffer[j].x = 0;
                    vertBuffer[j].y = 0;
                }
            }

            // Clear vertices that are no longer displayed on the last used render unit
            if (unitIndex <= lastUnitIndex) {
                var vertBuffer = renderUnits[unitIndex].VertexBuffer;
                int end = unitIndex == lastUnitIndex ? lastVIndex : vertBuffer.Length;

                for (int i = vIndex; i < end; i++) {
                    vertBuffer[i].x = 0;
                    vertBuffer[i].y = 0;
                }
            }

            // submit mesh data
            for (int i = 0; i <= unitIndex; i++) {
                RenderUnit unit = renderUnits[i];
                unit.Mesh.SetVertexBufferData(unit.VertexBuffer, 0, 0, unit.VertexBuffer.Length, 0, UpdateFlags);
            }
        }
    }

    private void DrawParticle(Vector2 position)
    {
        float hs = _halfParticleSize;
        Color color = _particlesColor;
        float x = position.x, y = position.y;

        if (_vertexIndex >= MaxVertexCount)
        {
            _vertexIndex = 0;
            _unitIndex++;
        }

        ColoredVertex[] vertBuffer = _renderUnits[_unitIndex].VertexBuffer;
        float x_min = x - hs;
        float x_max = x + hs;
        float y_min = y - hs;
        float y_max = y + hs;

        vertBuffer[_vertexIndex].x = x_min;
        vertBuffer[_vertexIndex].y = y_min;
        vertBuffer[_vertexIndex].color = color;
        _vertexIndex++;
        vertBuffer[_vertexIndex].x = x_max;
        vertBuffer[_vertexIndex].y = y_min;
        vertBuffer[_vertexIndex].color = color;
        _vertexIndex++;
        vertBuffer[_vertexIndex].x = x_max;
        vertBuffer[_vertexIndex].y = y_max;
        vertBuffer[_vertexIndex].color = color;
        _vertexIndex++;
        vertBuffer[_vertexIndex].x = x_min;
        vertBuffer[_vertexIndex].y = y_max;
        vertBuffer[_vertexIndex].color = color;
        _vertexIndex++;
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

        // Add to render unit
        ColoredVertex[] vertBuffer = _linRenderUnits[_linUnitIndex].VertexBuffer;
        if (_linIndex >= vertBuffer.Length)
        {
            _linIndex = 0;
            _linUnitIndex++;
        }

        if (_linUnitIndex >= _linRenderUnits.Count)
            if (_meshLines) AddMeshLinRenderUnit(); else AddLinRenderUnit();

        vertBuffer = _linRenderUnits[_linUnitIndex].VertexBuffer;

        if (_meshLines)
        {
            // line width == half of triangle width
            float dirNormal = distance / _actualLineWidth;
            float normalX = diffY / dirNormal, normalY = -diffX / dirNormal;

            // Tip vertex (make triangle longer than line, f = 2.0, see AddLinRenderUnit function)
            vertBuffer[_linIndex].x = pos1.x + diffX;
            vertBuffer[_linIndex].y = pos1.y + diffY;
            vertBuffer[_linIndex].color = color;
            _linIndex++;
            // Base vertices
            vertBuffer[_linIndex].x = pos2.x + normalX;
            vertBuffer[_linIndex].y = pos2.y + normalY;
            vertBuffer[_linIndex].color = color;
            _linIndex++;
            vertBuffer[_linIndex].x = pos2.x - normalX;
            vertBuffer[_linIndex].y = pos2.y - normalY;
            vertBuffer[_linIndex].color = color;
            _linIndex++;
        }
        else
        {
            vertBuffer[_linIndex].x = pos1.x;
            vertBuffer[_linIndex].y = pos1.y;
            vertBuffer[_linIndex].color = color;
            _linIndex++;
            vertBuffer[_linIndex].x = pos2.x;
            vertBuffer[_linIndex].y = pos2.y;
            vertBuffer[_linIndex].color = color;
            _linIndex++;
        }
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

        // Add to render unit
        ColoredVertex[] vertBuffer = _triRenderUnits[_triUnitIndex].VertexBuffer;

        if (_triIndex >= vertBuffer.Length) {
            _triIndex = 0;
            _triUnitIndex++;
        }

        if (_triUnitIndex >= _triRenderUnits.Count)
            AddTriRenderUnit();

        vertBuffer = _triRenderUnits[_triUnitIndex].VertexBuffer;

        vertBuffer[_triIndex].x = pos1.x;
        vertBuffer[_triIndex].y = pos1.y;
        vertBuffer[_triIndex].color = color;
        _triIndex++;
        vertBuffer[_triIndex].x = pos2.x;
        vertBuffer[_triIndex].y = pos2.y;
        vertBuffer[_triIndex].color = color;
        _triIndex++;
        vertBuffer[_triIndex].x = pos3.x;
        vertBuffer[_triIndex].y = pos3.y;
        vertBuffer[_triIndex].color = color;
        _triIndex++;
    }
}
