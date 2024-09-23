using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using static System.Math;
using Core;

namespace ConstellationUI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UILineRenderer : MonoBehaviour
    {
        [SerializeField] private float _lineWidth = 1;
        [SerializeField] private Color _lineColor = Color.red;
        [SerializeField] private bool _maskable = true;

        // Unity's restriction per mesh, minus 4 (quad) (otherwise it somehow compalins about exactly 65k verts? idk, but 4 less vertices is probably fine)
        protected const int MaxVertexCount = 65000 - 4;

        private List<UILineChunk> _meshChunks = new List<UILineChunk>();
        private readonly UIVertex[] _vertices = new UIVertex[4] { UIVertex.simpleVert, UIVertex.simpleVert, UIVertex.simpleVert, UIVertex.simpleVert };

        private Vector2[] _points;
        private bool _chunckedMode;

        public Color Color {
            get => _lineColor;
            set
            {
                _lineColor = value;
                CreateMeshes();
            }
        }

        public Vector2[] LocalPoints
        {
            get => _points;
            set
            {
                _points = value;
                CreateMeshes();
            }
        }
        
        public bool ChunckedMode
        {
            get => _chunckedMode;
            set
            {
                if (_chunckedMode == value) return;
                _chunckedMode = value;
                CreateMeshes();
            }
        }

        protected RectTransform rectTransform => GetComponent<RectTransform>();

        public void SetNormalizedPoints(IList<Vector2> normalizedPoints)
        {
            var points = new Vector2[normalizedPoints.Count];
            for (int i = 0; i < normalizedPoints.Count; i++)
            {
                points[i] = UIPositionHelper.NormalizedToLocalPosition(rectTransform, normalizedPoints[i]);
            }

            LocalPoints = points;
        }

        public void SetNormalizedPoints(FastList<Vector2> normalizedPoints)
        {
            var points = new Vector2[normalizedPoints._count];
            for (int i = 0; i < normalizedPoints._count; i++)
            {
                points[i] = UIPositionHelper.NormalizedToLocalPosition(rectTransform, normalizedPoints[i]);
            }

            LocalPoints = points;
        }

        public void SetWorldPoints(Vector2[] worldPoints)
        {
            var points = new Vector2[worldPoints.Length];
            for (int i = 0; i < worldPoints.Length; i++)
            {
                points[i] = rectTransform.InverseTransformPoint(worldPoints[i]);
            }

            LocalPoints = points;
        }

        private void CreateMeshes()
        {
            if (_points == null)
            {
                foreach (var chunk in _meshChunks)
                    Destroy(chunk.gameObject);
                _meshChunks.Clear();
                
                return;
            }

            int verticesTotal = 4 * (_chunckedMode ? _points.Length / 2 : _points.Length - 1);
            int chunksRequiered = (verticesTotal + MaxVertexCount - 1) / MaxVertexCount;

            while (_meshChunks.Count > chunksRequiered)
            {
                Destroy(_meshChunks[0].gameObject);
                _meshChunks.RemoveAt(0);
            }

            while (_meshChunks.Count < chunksRequiered)
            {
                GameObject go = new GameObject() { name = "Line chunk" };
                go.transform.SetParent(transform, false);
                go.AddComponent<CanvasRenderer>();
                UILineChunk chunk = go.AddComponent<UILineChunk>();
                chunk.maskable = _maskable;
                _meshChunks.Add(chunk);
            }

            int loopStep = _chunckedMode ? 2 : 1;
            int nextToDraw = 0;
            int chunkIndex = 0;

            while (nextToDraw < _points.Length - 1)
            {
                UILineChunk chunk = _meshChunks[chunkIndex++];
                int startIndex = nextToDraw + 1, endIndex;

                if (_chunckedMode)
                {
                    int actualPoints = Mathf.Min(MaxVertexCount / 4 * 2, _points.Length - nextToDraw);
                    endIndex = startIndex + actualPoints;
                    nextToDraw += actualPoints;
                } else
                {
                    int actualPoints = Mathf.Min(MaxVertexCount / 4 + 1, _points.Length - nextToDraw);
                    endIndex = startIndex + actualPoints;
                    nextToDraw += actualPoints - 1;
                }

                chunk.OnPopulateMeshAction = vh => {
                    vh.Clear();
                    for (int i = startIndex; i < endIndex - 1; i += loopStep)
                        AddLineSegment(vh, _points[i - 1], _points[i]);
                };
            }
        }

        private void AddLineSegment(VertexHelper vh, Vector2 p1, Vector2 p2)
        {
            if (p1 == p2) return;

            float dirX = p1.x - p2.x, dirY = p1.y - p2.y;
            float dirNormal = (float)Sqrt(dirX * dirX + dirY * dirY) / _lineWidth;
            float normalX = dirY / dirNormal, normalY = -dirX / dirNormal;

            _vertices[0].position = new Vector2(p1.x + normalX, p1.y + normalY);
            _vertices[0].color = Color;

            _vertices[1].position = new Vector2(p2.x + normalX, p2.y + normalY);
            _vertices[1].color = Color;

            _vertices[2].position = new Vector2(p2.x - normalX, p2.y - normalY);
            _vertices[2].color = Color;

            _vertices[3].position = new Vector2(p1.x - normalX, p1.y - normalY);
            _vertices[3].color = Color;

            vh.AddUIVertexQuad(_vertices);
        }
    }
}