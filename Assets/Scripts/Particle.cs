using UnityEngine;

public class Particle : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public Vector3 Velocity => _velocity;
    public float XBound { get; set; }
    public float YBound { get; set; }
    public System.Func<Particle, float> VelocityDelegate { get; set; }
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

    private Transform _transform;
    private Vector3 _position;
    private Vector3 _velocity;

    private float RandomDirection()
	{
        return 2 * Random.value - 1;
    }

    private void Start()
    {
        _transform = transform;
        _position = _transform.localPosition;
        _velocity = new Vector3(RandomDirection(), RandomDirection()) * VelocityDelegate(this);
    }

    private void Update()
    {
        _position += _velocity * Time.deltaTime;

        _transform.localPosition = _position;

        bool xHit = Mathf.Abs(_position.x) >= XBound && _position.x * _velocity.x > 0;
        bool yHit = Mathf.Abs(_position.y) >= YBound && _position.y * _velocity.y > 0;
        if (xHit || yHit)
		{
            float magnitude = VelocityDelegate(this);
            _velocity = magnitude * new Vector3(xHit ? (-Random.value * Mathf.Sign(_position.x)) : RandomDirection(), 
                                               yHit ? (-Random.value * Mathf.Sign(_position.y)) : RandomDirection());
		}
    }
}
