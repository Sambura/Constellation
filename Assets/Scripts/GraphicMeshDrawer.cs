using System.Collections.Generic;
using UnityEngine;

public class GraphicMeshDrawer : MonoBehaviour
{
    [SerializeField] private int _layer;
    [SerializeField] private Material _material;
    [SerializeField] private string _colorPropertyName;
    [SerializeField] private Camera _camera;

    private int _colorPropertyId;
    private MaterialPropertyBlock _properties;
    private Mesh _mesh;
    private List<LineEntry> _linesToDraw;
    private List<MeshLineEntry> _meshLinesToDraw;
    private Vector3[] _bgVertices;
    private Color _bgColor;
    private bool _drawBg = false;
    private Transform _cameraTransform;

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

        _linesToDraw = new List<LineEntry>();
        _meshLinesToDraw = new List<MeshLineEntry>();
        _bgVertices = new Vector3[4];
        _cameraTransform = _camera.transform;
    }

    private Matrix4x4 GetCameraProjectionMatrix()
	{
        float height = _camera.orthographicSize;
        float width = height * _camera.aspect;
        Vector3 position = _cameraTransform.position;
        float left = position.x - width;
        float right = position.x + width;
        float top = position.y + height;
        float bottom = position.y - height;
        float zNear = position.z + _camera.nearClipPlane;
        float zFar = position.z + _camera.farClipPlane;
        return Matrix4x4.Ortho(left, right, bottom, top, zNear, zFar);
	}

    public void SetBgPositionAndSize(float x, float y, float width, float height)
	{
        _bgVertices[0].x = x - width / 2;
        _bgVertices[0].y = y - height / 2;
        _bgVertices[1].x = x - width / 2;
        _bgVertices[1].y = y + height / 2;
        _bgVertices[2].x = x + width / 2;
        _bgVertices[2].y = y + height / 2;
        _bgVertices[3].x = x + width / 2;
        _bgVertices[3].y = y - height / 2;
    }

	public void DrawLine(Vector3 p1, Vector3 p2, Color color, float width)
	{
        Vector3 direction = p1 - p2;
        float length = direction.magnitude;

        Matrix4x4 matrix = Matrix4x4.TRS(p2, Quaternion.FromToRotation(Vector3.right, direction), new Vector3(length, width, 1));
        _properties.SetColor(_colorPropertyId, color);

        Graphics.DrawMesh(_mesh, matrix, _material, _layer, null, 0, _properties);
    }

    private void OnPreRender()
	{
        _material.SetPass(0);
        GL.PushMatrix();

        GL.LoadProjectionMatrix(GetCameraProjectionMatrix());

        GL.Begin(GL.QUADS);

        if (_drawBg)
		{
            GL.Color(_bgColor);
            for (int i = 0; i < 4; i++) GL.Vertex(_bgVertices[i]);
            _drawBg = false;
        }

        foreach (MeshLineEntry line in _meshLinesToDraw)
        {
            float dirX = line.x1 - line.x2, dirY = line.y1 - line.y2;
            float dirNormal = (float)System.Math.Sqrt(dirX * dirX + dirY * dirY) / line.width;
            float normalX = dirY / dirNormal, normalY = -dirX / dirNormal;

            GL.Color(line.color);
            GL.Vertex3(line.x1 + normalX, line.y1 + normalY, 0);
            GL.Vertex3(line.x2 + normalX, line.y2 + normalY, 0);
            GL.Vertex3(line.x2 - normalX, line.y2 - normalY, 0);
            GL.Vertex3(line.x1 - normalX, line.y1 - normalY, 0);
        }
        GL.End();

        GL.Begin(GL.LINES);
        foreach (LineEntry line in _linesToDraw)
        {
            GL.Color(line.color);
            GL.Vertex3(line.x1, line.y1, 0);
            GL.Vertex3(line.x2, line.y2, 0);
        }
        GL.End();

        GL.PopMatrix();
        _linesToDraw.Clear();
        _meshLinesToDraw.Clear();
    }

	public void DrawLineGL(Vector3 p1, Vector3 p2, Color color)
    {
        _linesToDraw.Add(new LineEntry(p1.x, p1.y, p2.x, p2.y, color));
    }

    public void DrawMeshLineGL(Vector3 p1, Vector3 p2, Color color, float width)
    {
        _meshLinesToDraw.Add(new MeshLineEntry(p1.x, p1.y, p2.x, p2.y, width, color));
    }

    public void DrawBackgroundGL(Color color)
	{
        _drawBg = true;
        _bgColor = color;
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

    private struct MeshLineEntry
    {
        public float x1, x2, y1, y2, width;
        public Color color;

        public MeshLineEntry(float x1, float y1, float x2, float y2, float width, Color color)
        {
            this.x1 = x1;
            this.x2 = x2;
            this.y1 = y1;
            this.y2 = y2;
            this.width = width;
            this.color = color;
        }
    }
}
