using UnityEngine;

public class Line : MonoBehaviour
{
    [SerializeField] private LineRenderer _lineRenderer;

    private GameObject _this;
    private bool _activeSelf;

    public float LineWidth
	{
        get => _lineRenderer.widthMultiplier;
        set => _lineRenderer.widthMultiplier = value;
	}

    public Color Color
	{
        get => _gradient.Evaluate(0);
        set
        {
            _colorKeys[0].color = value;
            _alphaKeys[0].alpha = value.a;
            _gradient.SetKeys(_colorKeys, _alphaKeys);
            _lineRenderer.colorGradient = _gradient;
        }
    }

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

    private Gradient _gradient;
    private Vector3[] _positions;
    private GradientColorKey[] _colorKeys;
    private GradientAlphaKey[] _alphaKeys;

    public void SetPositions(Vector3 p1, Vector3 p2)
	{
        _positions[0] = p1;
        _positions[1] = p2;
        _lineRenderer.SetPositions(_positions);
	}

    private void Awake()
    {
        _this = gameObject;
        _activeSelf = true;
        _gradient = new Gradient();
        _positions = new Vector3[2];
        _colorKeys = new GradientColorKey[] { new GradientColorKey() };
        _alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey() } ;
        _gradient.SetKeys(_colorKeys, _alphaKeys);
        _lineRenderer.colorGradient = _gradient;
    }
}
