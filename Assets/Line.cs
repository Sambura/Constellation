using UnityEngine;

public class Line : MonoBehaviour
{
    [SerializeField] private LineRenderer _lineRenderer;

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
            _gradient.SetKeys(new GradientColorKey[] { new GradientColorKey(value, 0) },
                              new GradientAlphaKey[] { new GradientAlphaKey(value.a, 0) });
            _lineRenderer.colorGradient = _gradient;
        }
    }

	public Vector3 this[int index]
	{
		get => _lineRenderer.GetPosition(index);
        set => _lineRenderer.SetPosition(index, value);
    }

    public bool Enabled
	{
        get => gameObject.activeSelf;
        set => gameObject.SetActive(value);
	}

    private Gradient _gradient;

    private void Awake()
    {
        _gradient = new Gradient();
        _gradient.SetKeys(new GradientColorKey[] { new GradientColorKey() }, new GradientAlphaKey[] { new GradientAlphaKey() });
        _lineRenderer.colorGradient = _gradient;
    }
}
