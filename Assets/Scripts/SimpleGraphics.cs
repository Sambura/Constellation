using System.Collections.Generic;
using UnityEngine;
using Core;

public class SimpleGraphics : MonoBehaviour
{
    [SerializeField] private Material _material;
    [SerializeField] private Camera _camera;

    private Transform _cameraTransform;
    private FastList<SimpleDrawBatch> _batches;

    private void Awake()
    {
        _cameraTransform = _camera.transform;
        _batches = new FastList<SimpleDrawBatch>();
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

    private void OnPreRender()
	{
        _material.SetPass(0);
        GL.PushMatrix();
        GL.LoadProjectionMatrix(GetCameraProjectionMatrix());

        for (int batchIndex = 0; batchIndex < _batches._count; batchIndex++) {
            SimpleDrawBatch batch = _batches[batchIndex];

            if (batch.triangles != null)
            {
                GL.Begin(GL.TRIANGLES);
                TriangleEntry[] buffer = batch.triangles._buffer;
                int count = batch.triangles._count;
                for (int i = 0; i < count; i++)
                {
                    TriangleEntry triangle = buffer[i];
                    GL.Color(triangle.color);
                    GL.Vertex3(triangle.x1, triangle.y1, 0);
                    GL.Vertex3(triangle.x2, triangle.y2, 0);
                    GL.Vertex3(triangle.x3, triangle.y3, 0);
                }
                GL.End();
            }

            GL.Begin(GL.QUADS);
            if (batch.quads != null)
            {
                QuadEntry[] buffer = batch.quads._buffer;
                int count = batch.quads._count;
                for (int i = 0; i < count; i++)
                {
                    QuadEntry quad = buffer[i];
                    GL.Color(quad.color);
                    GL.Vertex3(quad.x1, quad.y1, 0);
                    GL.Vertex3(quad.x2, quad.y2, 0);
                    GL.Vertex3(quad.x3, quad.y3, 0);
                    GL.Vertex3(quad.x4, quad.y4, 0);
                }
            }

            if (batch.meshLines != null)
            {
                MeshLineEntry[] buffer = batch.meshLines._buffer;
                int count = batch.meshLines._count;
                for (int i = 0; i < count; i++)
                {
                    MeshLineEntry line = buffer[i];
                    float dirX = line.x1 - line.x2, dirY = line.y1 - line.y2;
                    float dirNormal = (float)System.Math.Sqrt(dirX * dirX + dirY * dirY) / line.width;
                    float normalX = dirY / dirNormal, normalY = -dirX / dirNormal;

                    GL.Color(line.color);
                    GL.Vertex3(line.x1 + normalX, line.y1 + normalY, 0);
                    GL.Vertex3(line.x2 + normalX, line.y2 + normalY, 0);
                    GL.Vertex3(line.x2 - normalX, line.y2 - normalY, 0);
                    GL.Vertex3(line.x1 - normalX, line.y1 - normalY, 0);
                }
            }
            GL.End();

            if (batch.lines != null)
            {
                GL.Begin(GL.LINES);
                LineEntry[] buffer = batch.lines._buffer;
                int count = batch.lines._count;
                for (int i = 0; i < count; i++)
                {
                    LineEntry line = buffer[i];
                    GL.Color(line.color);
                    GL.Vertex3(line.x1, line.y1, 0);
                    GL.Vertex3(line.x2, line.y2, 0);
                }
                GL.End();
            }
        }

        GL.PopMatrix();
        _batches.PseudoClear();
    }

	public void AddBatch(SimpleDrawBatch batch)
	{
        _batches.Add(batch);
	}

    public struct LineEntry
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

    public struct MeshLineEntry
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

    public struct TriangleEntry
    {
        public float x1, x2, x3, y1, y2, y3;
        public Color color;

        public TriangleEntry(float x1, float y1, float x2, float y2, float x3, float y3, Color color)
        {
            this.x1 = x1;
            this.x2 = x2;
            this.y1 = y1;
            this.y2 = y2;
            this.x3 = x3;
            this.y3 = y3;
            this.color = color;
        }
    }

    public struct QuadEntry
    {
        public float x1, x2, x3, y1, y2, y3, x4, y4;
        public Color color;

        public QuadEntry(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4, Color color)
        {
            this.x1 = x1;
            this.x2 = x2;
            this.y1 = y1;
            this.y2 = y2;
            this.x3 = x3;
            this.y3 = y3;
            this.x4 = x4;
            this.y4 = y4;
            this.color = color;
        }
    }

    public class SimpleDrawBatch
	{
        public FastList<LineEntry> lines;
        public FastList<MeshLineEntry> meshLines;
        public FastList<TriangleEntry> triangles;
        public FastList<QuadEntry> quads;
	}
}
