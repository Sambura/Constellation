using UnityEngine;
using Core;

namespace SimpleGraphics
{
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

        public void SetCoords(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
        {
            this.x1 = x1;
            this.x2 = x2;
            this.y1 = y1;
            this.y2 = y2;
            this.x3 = x3;
            this.y3 = y3;
            this.x4 = x4;
            this.y4 = y4;
        }

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
