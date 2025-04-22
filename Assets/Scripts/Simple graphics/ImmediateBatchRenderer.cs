using System.Collections.Generic;
using UnityEngine;

namespace SimpleGraphics
{
    public class ImmediateBatchRenderer : MonoBehaviour
    {
        [SerializeField] private Material _defaultMaterial;
        [SerializeField] private Camera _camera;

        private bool _enabled = true;
        private Transform _cameraTransform;
        private SortedList<int, SimpleDrawBatch> _batches = new SortedList<int, SimpleDrawBatch>();
        private IList<SimpleDrawBatch> _batchList;
        private int _batchesCount;

        public bool ClearColor = false;

        private void Awake()
        {
            _cameraTransform = _camera.transform;
            _batchList = _batches.Values;
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

        public void FromScreenToWorld(ref float x, ref float y)
        {
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(x, y));
            x = world.x;
            y = world.y;
        }

        public void FromScreenToWorld(ref LineEntry line)
        {
            FromScreenToWorld(ref line.x1, ref line.y1);
            FromScreenToWorld(ref line.x2, ref line.y2);
        }

        private void DrawBatch(SimpleDrawBatch batch)
        {
            batch.material.SetPass(0);

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
                    GL.TexCoord(new Vector3(0, 1));
                    GL.Vertex3(quad.x1, quad.y1, 0);
                    GL.TexCoord(new Vector3(0, 0));
                    GL.Vertex3(quad.x2, quad.y2, 0);
                    GL.TexCoord(new Vector3(1, 0));
                    GL.Vertex3(quad.x3, quad.y3, 0);
                    GL.TexCoord(new Vector3(1, 1));
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

        private void OnPreRender()
        {
            if (_enabled == false) return;

            GL.PushMatrix();

            GL.Clear(true, ClearColor, new Color(0, 0, 0, 0));
            GL.LoadProjectionMatrix(GetCameraProjectionMatrix());
            GL.modelview = Matrix4x4.identity;
            for (int batchIndex = 0; batchIndex < _batchesCount; batchIndex++)
                DrawBatch(_batchList[batchIndex]);

            GL.PopMatrix();
        }

        private void OnEnable() {
            _camera.clearFlags = CameraClearFlags.Nothing;
            _enabled = true;
        }

        private void OnDisable() { _enabled = false; }

        /// <summary>
        /// Adds a batch in render queue with the specified index
        /// </summary>
        /// <param name="renderQueueIndex">Rendering index. Greater values are rendered later</param>
        /// <param name="batch">A batch to render</param>
        public void AddBatch(int renderQueueIndex, SimpleDrawBatch batch)
        {
            if (batch.material == null) batch.material = _defaultMaterial;
            _batches.Add(renderQueueIndex, batch);
            _batchesCount = _batches.Count;
        }

        public void RemoveBatch(int renderQueueIndex, SimpleDrawBatch batch)
        {
            if (_batches.TryGetValue(renderQueueIndex, out SimpleDrawBatch existing))
            {
                if (existing == batch)
                {
                    _batches.Remove(renderQueueIndex);
                    _batchesCount = _batches.Count;
                }
            }
        }
    }
}