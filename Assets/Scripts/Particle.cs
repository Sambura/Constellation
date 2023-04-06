using UnityEngine;
using static System.Math;
using static Core.MathUtility;

public class Particle : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public Vector3 Velocity
	{
        get => _velocity;
        set => _velocity = value;
	}

    public System.Func<Particle, float> VelocityDelegate { get; set; }
    public Viewport Viewport
	{
        get => _viewport;
        set
		{
            if (_viewport == value) return;

            if (_viewport != null)
                _viewport.CameraDimensionsChanged -= OnViewportChanged;

            _viewport = value;
            _viewport.CameraDimensionsChanged += OnViewportChanged;
            OnViewportChanged();
		}
	}
    public bool Visible {
        get => _spriteRenderer.enabled;
        set => _spriteRenderer.enabled = value;
    }
    public Color Color
	{
        get => _spriteRenderer.color;
        set => _spriteRenderer.color = value;
	}
    public Vector3 Position
    {
        get => _position;
        set => _position = value;
    }
    public float Size
	{
        get => _transform.localScale.x;
        set => _transform.localScale = new Vector3(value, value, value);
	}
    public float BoundMargins
    {
        get => _boundMargins;
        set
        {
            _boundMargins = value;
            OnViewportChanged();
        }
    }
    public bool Warp { get; set; }

    private Viewport _viewport;
    public Transform _transform;
    public Vector3 _position;
    public Vector3 _velocity;
    public float _left;
    public float _right;
    public float _top;
    public float _bottom;
    public float _boundMargins;

    private void OnViewportChanged()
	{
        _left = -_viewport.MaxX + _boundMargins;
        _right = _viewport.MaxX - _boundMargins;
        _bottom = -_viewport.MaxY + _boundMargins;
        _top = _viewport.MaxY - _boundMargins;
	}

    public void SetRandomVelocity(float minAngle = Angle0, float maxAngle = Angle360)
	{
        float angle = Random.Range(minAngle, maxAngle);
        float magnitude = VelocityDelegate(this);
        _velocity.Set((float)Cos(angle) * magnitude, (float)Sin(angle) * magnitude, 0);
    }

    private void Awake()
    {
        _transform = transform;
        _position = _transform.localPosition;
    }

	private void Start()
	{
        _transform.localPosition = _position;
    }
}
