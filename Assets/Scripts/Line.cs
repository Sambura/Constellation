using UnityEngine;

public class Line : MonoBehaviour
{
    [SerializeField] private MeshFilter _meshFilter;
    [SerializeField] private MeshRenderer _meshRenderer;

    private GameObject _this;
    private bool _activeSelf;
    private MaterialPropertyBlock _properties;
    private Transform _transform;
    private Quaternion _rotation;
    private Vector3 _scale;

    private static Mesh LineMesh;
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

    public bool Enabled
	{
        get => _activeSelf;
        set
        {
            if (_activeSelf != value)
            {
                _activeSelf = value;
                _this.SetActive(value);
            }
        }
	}

    public void ChangeState(Vector3 p1, Vector3 p2, Color color, float width)
	{
        _properties.SetColor(ColorPropertyId, color);
        _meshRenderer.SetPropertyBlock(_properties);

        float diffX = p1.x - p2.x,  diffY = p1.y - p2.y;
        float angle = (float)System.Math.Atan2(diffY, diffX) / 2;
        _rotation.z = (float)System.Math.Sin(angle);
        _rotation.w = (float)System.Math.Cos(angle);
        _scale.x = (float)System.Math.Sqrt(diffX * diffX + diffY * diffY);
        _scale.y = width;
        _transform.SetPositionAndRotation(p2, _rotation);
        _transform.localScale = _scale;
    }

    private void Awake()
    {
        _this = gameObject;
        _activeSelf = true;
        _properties = new MaterialPropertyBlock();
        _transform = transform;
        _rotation = new Quaternion(0, 0, 0, 0);
        _scale = Vector3.one;

        if (LineMesh == null)
		{
            Vector3[] vertices = new Vector3[4];
            int[] triangles = new int[6];
            LineMesh = new Mesh();

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

            LineMesh.vertices = vertices;
            LineMesh.triangles = triangles;
        }

        _meshFilter.mesh = LineMesh;
    }
}
