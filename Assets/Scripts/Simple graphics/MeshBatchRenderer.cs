using System.Collections.Generic;
using UnityEngine;

namespace SimpleGraphics
{
    public class MeshBatchRenderer : MonoBehaviour
    {
        [SerializeField] private Material _material;
        [SerializeField] private Camera _camera;
        [SerializeField] private bool _enabled = true;

        private Transform _cameraTransform;
        private SortedList<int, SimpleDrawBatch> _batches = new SortedList<int, SimpleDrawBatch>();
        private IList<SimpleDrawBatch> _batchList;
        private int _batchesCount;

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

        private void Update()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[] { new Vector3(0, 0), new Vector3(1, 1), new Vector3(1, 0) };
            mesh.triangles = new int[] { 0, 1, 2 };
            Graphics.DrawMesh(mesh, Matrix4x4.identity, _material, 0);


            if (_enabled == false) return;

            for (int batchIndex = 0; batchIndex < _batchesCount; batchIndex++)
            {
                SimpleDrawBatch batch = _batchList[batchIndex];

                if (batch.lines != null)
                {
                    Mesh linesMesh = new Mesh();
                    
                    LineEntry[] buffer = batch.lines._buffer;
                    int count = batch.lines._count;

                    Vector3[] verts = new Vector3[2 * count];
                    int[] indices = new int[2 * count];

                    for (int i = 0; i < count; i++)
                    {
                        LineEntry line = buffer[i];

                        verts[2 * i] = new Vector3(line.x1, line.y1);
                        verts[2 * i + 1] = new Vector3(line.x1, line.y1);

                        indices[2 * i] = 2 * i;
                        indices[2 * i + 1] = 2 * i + 1;
                    }

                    linesMesh.SetVertices(verts);
                    linesMesh.SetIndices(indices, MeshTopology.Lines, 0);

                    Graphics.DrawMesh(linesMesh, Matrix4x4.identity, _material, 0);
                }
            }
        }

        /// <summary>
        /// Adds a batch in render queue with the specified index
        /// </summary>
        /// <param name="renderQueueIndex">Rendering index. Greater values are rendered later</param>
        /// <param name="batch">A batch to render</param>
        public void AddBatch(int renderQueueIndex, SimpleDrawBatch batch)
        {
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