using System.Collections.Generic;
using UnityEngine;

public class GraphicMeshDrawer : MonoBehaviour
{
    [SerializeField] private int _layer;
    [SerializeField] private Material _material;
    [SerializeField] private string _colorPropertyName;

    private int _colorPropertyId;
    private MaterialPropertyBlock _properties;
    private Mesh _mesh;
    private List<LineEntry> _toDraw;

    private void Awake()
    {
        _colorPropertyId = Shader.PropertyToID(_colorPropertyName);
        Vector3[] vertices = new Vector3[4];
        int[] triangles = new int[6];
        _properties = new MaterialPropertyBlock();
        _mesh = new Mesh();

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 1;
        triangles[4] = 2;
        triangles[5] = 3;

        vertices[0] = new Vector3(0, 0.5f);
        vertices[1] = new Vector3(0, -0.5f);
        vertices[2] = new Vector3(1, 0.5f);
        vertices[3] = new Vector3(1, -0.5f);

        _mesh.vertices = vertices;
        _mesh.triangles = triangles;

        _toDraw = new List<LineEntry>();
    }

	public void DrawLine(Vector3 p1, Vector3 p2, Color color, float width)
	{
        Vector3 direction = p1 - p2;
        float length = direction.magnitude;

        Matrix4x4 matrix = Matrix4x4.TRS(p2, Quaternion.FromToRotation(Vector3.right, direction), new Vector3(length, width, 1));
        _properties.SetColor(_colorPropertyId, color);

        Graphics.DrawMesh(_mesh, matrix, _material, _layer, null, 0, _properties);
    }

    private void OnRenderObject()
	{
        _material.SetPass(0);

        GL.PushMatrix();
        GL.Begin(GL.LINES);
        foreach (LineEntry line in _toDraw)
        {
            GL.Color(line.color);
            GL.Vertex3(line.x1, line.y1, 0);
            GL.Vertex3(line.x2, line.y2, 0);
        }
        GL.End();
        GL.PopMatrix();

        _toDraw.Clear();
    }

	public void DrawLineGL(Vector3 p1, Vector3 p2, Color color)
    {
        _toDraw.Add(new LineEntry(p1.x, p1.y, p2.x, p2.y, color));
    }

    private struct LineEntry
	{
        public float x1, x2, y1, y2;
        public Color color;

        public LineEntry(float x1, float y1, float x2, float y2, Color color)
		{
            this.x1 = x1;
            this.x2 = x2;
            this.y1 = y1;
            this.y2 = y2;
            this.color = color;
        }
	}
}
