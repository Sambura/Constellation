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

    private Viewport _viewport;
    private Transform _transform;
    private Vector3 _position;
    private Vector3 _velocity;
    private float _left;
    private float _right;
    private float _top;
    private float _bottom;

    private void OnViewportChanged()
	{
        _left = -_viewport.MaxX;
        _right = _viewport.MaxX;
        _bottom = -_viewport.MaxY;
        _top = _viewport.MaxY;
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

	private void Update()
    {
        _position += _velocity * Time.deltaTime;
        _transform.localPosition = _position;

        bool leftHit = _position.x < _left;
        bool rightHit = _position.x > _right;
        bool bottomHit = _position.y < _bottom;
        bool topHit = _position.y > _top;

        if ((leftHit || rightHit) && _position.x * _velocity.x > 0)
		{
            SetRandomVelocity(leftHit ? -Angle90 : Angle90, leftHit ? Angle90 : Angle270);
		} 
        if ((bottomHit || topHit) && _position.y * _velocity.y > 0)
        {
            SetRandomVelocity(bottomHit ? Angle0 : Angle180, bottomHit ? Angle180 : Angle360);
        }
    }
}
