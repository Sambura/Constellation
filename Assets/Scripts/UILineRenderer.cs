using UnityEngine;
using UnityEngine.UI;
using static System.Math;
using Core;

[RequireComponent (typeof(CanvasRenderer))]
public class UILineRenderer : MaskableGraphic
{
    [SerializeField] private float _lineWidth = 1;

    private Vector2[] _points;
    private readonly UIVertex[] _vertices = new UIVertex[4] { UIVertex.simpleVert, UIVertex.simpleVert, UIVertex.simpleVert, UIVertex.simpleVert };

    public Vector2[] LocalPoints
	{
        get => _points;
        set
		{
            _points = value;
            UpdateGeometry();
		}
	}

    public void SetNormalizedPoints(Vector2[] normalizedPoints)
	{
        var points = new Vector2[normalizedPoints.Length];
        for (int i = 0; i < normalizedPoints.Length; i++)
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

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (_points == null) return;

        for (int i = 0; i < _points.Length - 1; i++)
            AddLineSegment(vh, _points[i], _points[i + 1]);
    }

	private void AddLineSegment(VertexHelper vh, Vector2 p1, Vector2 p2)
	{
        if (p1 == p2) return;

        float dirX = p1.x - p2.x, dirY = p1.y - p2.y;
        float dirNormal = (float)Sqrt(dirX * dirX + dirY * dirY) / _lineWidth;
        float normalX = dirY / dirNormal, normalY = -dirX / dirNormal;

		_vertices[0].position = new Vector2(p1.x + normalX, (p1.y + normalY));
        _vertices[0].color = color;

        _vertices[1].position = new Vector2(p2.x + normalX, (p2.y + normalY));
        _vertices[1].color = color;                                         

        _vertices[2].position = new Vector2(p2.x - normalX, (p2.y - normalY));
        _vertices[2].color = color;

        _vertices[3].position = new Vector2(p1.x - normalX, (p1.y - normalY));
        _vertices[3].color = color;

        vh.AddUIVertexQuad(_vertices);
    }

	protected override void Awake()
	{
        base.Awake();
        useLegacyMeshGeneration = false;
	}
}