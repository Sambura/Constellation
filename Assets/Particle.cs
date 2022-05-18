using UnityEngine;

public class Particle : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public Vector3 Velocity { get; private set; }
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

    private float RandomDirection()
	{
        return 2 * Random.value - 1;
    }

    private void Start()
    {
        Velocity = new Vector3(RandomDirection(), RandomDirection()) * VelocityDelegate(this);
    }

    private void Update()
    {
        transform.position = transform.position + Velocity * Time.deltaTime;

        bool xHit = Mathf.Abs(transform.position.x) >= XBound;
        bool yHit = Mathf.Abs(transform.position.y) >= YBound;
        if (xHit || yHit)
		{
            float magnitude = VelocityDelegate(this);
            Velocity = magnitude * new Vector3(xHit ? (-Random.value * Mathf.Sign(Velocity.x)) : RandomDirection(), 
                                               yHit ? (-Random.value * Mathf.Sign(Velocity.y)) : RandomDirection());
		}
    }
}
