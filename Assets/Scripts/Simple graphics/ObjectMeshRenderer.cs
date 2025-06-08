using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using static UnityCore.Utility;
using static Core.MathUtility;
using UnityCore;

public class ObjectMeshRenderer : MonoBehaviour
{
    [Header("Rendering")]
    [SerializeField] private Material _defaultMat;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct ColoredVertex
    {
        public float x;
        public float y;
        public Color color;
    }

    struct RenderUnit
    {
        public Mesh Mesh;
        public GameObject Parent;
        public ColoredVertex[] VertexBuffer;
    }

    class RenderUnitEntry
    {
        public List<RenderUnit> Units;
        public int VertexIndex;
        public int UnitIndex;
        public int LastVertexIndex;
        public int LastUnitIndex;
        public float RunningUnitCount;
    }

    private List<Dictionary<Material, RenderUnitEntry>> _renderUnits = new List<Dictionary<Material, RenderUnitEntry>>();
    private Dictionary<Material, RenderUnitEntry> _quadRenderUnits = new Dictionary<Material, RenderUnitEntry>();
    private Dictionary<Material, RenderUnitEntry> _lineRenderUnits = new Dictionary<Material, RenderUnitEntry>();
    private int _drawcalls;

    // a bit less than the actual max (it seems it's discouraged to go to the actual max)
    private static readonly int MaxVertexCount = 65200;
    private static readonly int QuadsPerUnit = MaxVertexCount / 4;
    private static readonly int TrianglesPerUnit = MaxVertexCount / 3;
    private static readonly int MeshLinesPerUnit = TrianglesPerUnit;
    private static readonly int LinesPerUnit = MaxVertexCount / 2;
    private static readonly MeshUpdateFlags UpdateFlags = MeshUpdateFlags.DontValidateIndices |
        MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontNotifyMeshUsers;

    public static ObjectMeshRenderer Instance { get; private set; }

    private void Awake() {
        _renderUnits.Add(_quadRenderUnits);
        _renderUnits.Add(_lineRenderUnits);
        Instance ??= this;
    }

    private RenderUnit MakeLineRenderUnit(Material mat, int lineCount = -1)
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

        GameObject meshObject = MakeFlatMesh(mesh, mat, transform, "LRU");
        return new RenderUnit() { Mesh = mesh, Parent = meshObject, VertexBuffer = vertexData };
    }

    private RenderUnit MakeQuadRenderUnit(Material mat, int quadCount = -1)
    {
        if (quadCount < 0) quadCount = QuadsPerUnit;

        ColoredVertex[] vertexData = new ColoredVertex[quadCount * 4];
        Vector2[] uvs = new Vector2[quadCount * 4];
        int[] triangles = new int[quadCount * 6];

        for (int i = 0; i < quadCount; i++)
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

        GameObject meshObject = MakeFlatMesh(mesh, mat, transform, "QRU");
        return new RenderUnit() { Mesh = mesh, Parent = meshObject, VertexBuffer = vertexData };
    }

    private void LateUpdate() {
        if (_drawcalls == 0) return;

        SubmitFrame();
        _drawcalls = _drawcalls > 1 ? 1 : 0;

        foreach (var units in _renderUnits) {
            foreach (var entry in units.Values) {
                entry.LastVertexIndex = entry.VertexIndex;
                entry.LastUnitIndex = entry.UnitIndex;
                entry.VertexIndex = 0;
                entry.UnitIndex = 0;

                entry.RunningUnitCount = (1 - Time.deltaTime) * entry.RunningUnitCount + Time.deltaTime * (entry.LastUnitIndex + 0.5f);
                float maxUnits = System.MathF.Max(entry.LastUnitIndex + 1, entry.RunningUnitCount);
                while (maxUnits < entry.Units.Count) {
                    Destroy(entry.Units[0].Parent);
                    entry.Units.RemoveAt(0);
                }
            }
        }
    }

    private void OnDisable() => LateUpdate();

    private void SubmitFrame()
    {
        foreach (var units in _renderUnits) {
            foreach (var entry in units.Values) {
                SubmitDynamicRenderUnits(entry.Units, entry.VertexIndex, entry.UnitIndex, entry.LastVertexIndex, entry.LastUnitIndex);
            }
        }

        static void SubmitDynamicRenderUnits(List<RenderUnit> renderUnits, int vIndex, int unitIndex, int lastVIndex, int lastUnitIndex)
        {
            if (renderUnits.Count == 0) return;

            // Activate / deactivate render units
            for (int i = 0; i < renderUnits.Count; i++)
            {
                bool enabled = i <= unitIndex && (unitIndex > 0 || vIndex > 0);
                if (renderUnits[i].Parent.activeSelf == enabled) continue;
                renderUnits[i].Parent.SetActive(enabled);
                if (enabled) continue;

                // If we disable render unit, we must clean it to be ready for further use
                var vertBuffer = renderUnits[i].VertexBuffer;
                int end = i == lastUnitIndex ? lastVIndex : vertBuffer.Length;
                for (int j = 0; j < end; j++)
                {
                    vertBuffer[j].x = 0;
                    vertBuffer[j].y = 0;
                }
            }

            // Clear vertices that are no longer displayed on the last used render unit
            if (unitIndex <= lastUnitIndex)
            {
                var vertBuffer = renderUnits[unitIndex].VertexBuffer;
                int end = unitIndex == lastUnitIndex ? lastVIndex : vertBuffer.Length;

                for (int i = vIndex; i < end; i++)
                {
                    vertBuffer[i].x = 0;
                    vertBuffer[i].y = 0;
                }
            }

            // submit mesh data
            for (int i = 0; i <= unitIndex; i++)
            {
                RenderUnit unit = renderUnits[i];
                unit.Mesh.SetVertexBufferData(unit.VertexBuffer, 0, 0, unit.VertexBuffer.Length, 0, UpdateFlags);
            }
        }
    }

    public void DrawQuad(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4, Material mat, Color color)
    {
        _drawcalls++;
        mat ??= _defaultMat;
        if (!_quadRenderUnits.TryGetValue(mat, out RenderUnitEntry entry)) {
            entry = new RenderUnitEntry() { Units = new List<RenderUnit>() { MakeQuadRenderUnit(mat) } };
            _quadRenderUnits.Add(mat, entry);
        }

        ColoredVertex[] vertBuffer = entry.Units[entry.UnitIndex].VertexBuffer;
        if (entry.VertexIndex >= vertBuffer.Length)
        {
            entry.VertexIndex = 0;
            entry.UnitIndex++;
            if (entry.UnitIndex >= entry.Units.Count)
                entry.Units.Add(MakeQuadRenderUnit(mat));

            vertBuffer = entry.Units[entry.UnitIndex].VertexBuffer;
        }

        vertBuffer[entry.VertexIndex].x = x1;
        vertBuffer[entry.VertexIndex].y = y1;
        vertBuffer[entry.VertexIndex].color = color;
        entry.VertexIndex++;
        vertBuffer[entry.VertexIndex].x = x2;
        vertBuffer[entry.VertexIndex].y = y2;
        vertBuffer[entry.VertexIndex].color = color;
        entry.VertexIndex++;
        vertBuffer[entry.VertexIndex].x = x3;
        vertBuffer[entry.VertexIndex].y = y3;
        vertBuffer[entry.VertexIndex].color = color;
        entry.VertexIndex++;
        vertBuffer[entry.VertexIndex].x = x4;
        vertBuffer[entry.VertexIndex].y = y4;
        vertBuffer[entry.VertexIndex].color = color;
        entry.VertexIndex++;
    }

    public void DrawLine(float x1, float y1, float x2, float y2, Material mat, Color color)
    {
        _drawcalls++;
        mat ??= _defaultMat;
        if (!_lineRenderUnits.TryGetValue(mat, out RenderUnitEntry entry)) {
            entry = new RenderUnitEntry() { Units = new List<RenderUnit>() { MakeLineRenderUnit(mat) } };
            _lineRenderUnits.Add(mat, entry);
        }

        ColoredVertex[] vertBuffer = entry.Units[entry.UnitIndex].VertexBuffer;
        if (entry.VertexIndex >= vertBuffer.Length)
        {
            entry.VertexIndex = 0;
            entry.UnitIndex++;
            if (entry.UnitIndex >= entry.Units.Count)
                entry.Units.Add(MakeLineRenderUnit(mat));

            vertBuffer = entry.Units[entry.UnitIndex].VertexBuffer;
        }

        vertBuffer[entry.VertexIndex].x = x1;
        vertBuffer[entry.VertexIndex].y = y1;
        vertBuffer[entry.VertexIndex].color = color;
        entry.VertexIndex++;
        vertBuffer[entry.VertexIndex].x = x2;
        vertBuffer[entry.VertexIndex].y = y2;
        vertBuffer[entry.VertexIndex].color = color;
        entry.VertexIndex++;
    }

    public void DrawArrow(Vector2 src, Vector2 direction, Material mat, Color color, float sideFactor = 17f / 40f, float sharpness = 10f / 17f)
    {
        Vector2 dest = src + direction;
        Vector2 sideDirection = -direction * sideFactor;
        Vector2 normal = sideDirection.Rotate90CCW();
        Vector2 arrow1 = dest + normal * (1 - sharpness) + sideDirection * sharpness;
        Vector2 arrow2 = dest - normal * (1 - sharpness) + sideDirection * sharpness;

        DrawLine(src.x, src.y, dest.x, dest.y, mat, color);
        DrawLine(arrow1.x, arrow1.y, dest.x, dest.y, mat, color);
        DrawLine(arrow2.x, arrow2.y, dest.x, dest.y, mat, color);
    }

    public void DrawEllipse(Vector2 center, float radiusA, float radiusB, bool dashed, Material mat, Color color, float pointCountFactor = 80)
    {
        int pointCount = Mathf.CeilToInt((radiusA + radiusB) * pointCountFactor);
        Vector2 prev = center + new Vector2(radiusA, 0);
        for (int i = 1; i <= pointCount; i++)
        {
            Vector2 pos = center + GetEllipsePoint(radiusA, radiusB, Mathf.PI * 2 * i / pointCount).ToVector2();
            if (!dashed || i % 2 == 1)
                DrawLine(prev.x, prev.y, pos.x, pos.y, mat, color);
            prev = pos;
        }
    }

    public void DrawCircle(Vector2 center, float radius, bool dashed, Material mat, Color color, float pointCountFactor = 80)
    {
        int pointCount = Mathf.CeilToInt(2 * radius * pointCountFactor);
        Vector2 prev = center + new Vector2(radius, 0);
        for (int i = 1; i <= pointCount; i++)
        {
            Vector2 pos = center + GetCirclePoint(radius, Mathf.PI * 2 * i / pointCount).ToVector2();
            if (!dashed || i % 2 == 1)
                DrawLine(prev.x, prev.y, pos.x, pos.y, mat, color);
            prev = pos;
        }
    }
}
