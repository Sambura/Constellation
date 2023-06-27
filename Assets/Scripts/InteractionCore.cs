using UnityEngine;
using System.Collections.Generic;
using ConfigSerialization;
using ConfigSerialization.Structuring;
using UnityRawInput;

public class InteractionCore : MonoBehaviour
{
	[Header("Objects")]
	[SerializeField] private ParticleController _particles;
	[SerializeField] private MainVisualizer _mainVisualizer;
	[SerializeField] private Viewport _viewport;

	[Header("Parameters")]
	[SerializeField] private float _attractionOrder = 2;
	[SerializeField] private float _attractionStrength = 1;
	[SerializeField] private float _attractionAssertion = 0.01f;

	[ConfigGroupMember("Attraction settings")]
	[SliderProperty(-10, 6, inputFormatting: "0.00")] public float AttractionOrder
	{
		get => _attractionOrder;
		set { if (_attractionOrder != value) { SetAttractionOrder(value); AttractionOrderChanged?.Invoke(value); } }
	}
	[ConfigGroupMember]
	[SliderProperty(-2, 2)] public float AttractionStrength
	{
		get => _attractionStrength;
		set { if (_attractionStrength != value) { SetAttractionStrength(value); AttractionStrengthChanged?.Invoke(value); } }
	}
	[ConfigGroupMember]
	[SliderProperty(-2, 2)] public float AttractionAssertion
	{
		get => _attractionAssertion;
		set { if (_attractionAssertion != value) { SetAttractionAssertion(value); AttractionAssertionChanged?.Invoke(value); } }
	}

	[ConfigGroupMember("Spiral attraction settings", 1)]
	[SliderProperty(-10, 10, name: "Acceleration", hasEvent: false)] public float AccelerationS { get; set; } = 6;
	[ConfigGroupMember(1)]
	[SliderProperty(0, 1, 0, name:"Consume radius", hasEvent: false)] public float ConsumeRadiusS { get; set; } = 0.025f;
	[ConfigGroupMember(1)]
	[SliderProperty(name: "Attraction ratio", hasEvent: false)] public float AttractionRatioS { get; set; } = 0.01f;
	[ConfigGroupMember(1)]
	[SliderProperty(0, 10, 0, name: "Velocity limit", hasEvent: false)] public float VelocityLimitS { get; set; } = 6f;
	[ConfigGroupMember(1)]
	[SliderProperty(0, 10, name: "Constant attraction", hasEvent: false)] public float ConstantAttractionS { get; set; } = 5f;

	public event System.Action<float> AttractionOrderChanged;
	public event System.Action<float> AttractionStrengthChanged;
	public event System.Action<float> AttractionAssertionChanged;

	private void SetAttractionOrder(float value) { _attractionOrder = value; }
	private void SetAttractionStrength(float value) { _attractionStrength = value; }
	private void SetAttractionAssertion(float value) { _attractionAssertion = value; }

	public void Update()
	{
		Vector3 mousePosition = _viewport.Camera.ScreenToWorldPoint(Input.mousePosition);
		mousePosition.z = 0;

		if (Input.GetKey(KeyCode.S) || RawInput.IsKeyDown(RawKey.S))
			InvokeSpiralAttractor(mousePosition);

		if (Input.GetKey(KeyCode.A) || RawInput.IsKeyDown(RawKey.A))
			InvokeAttractor(mousePosition);
	}

	public void InvokeSpiralAttractor(Vector2 point)
	{
		Vector3 mousePosition = point;
		List<Particle> particles = _particles.Particles;

		float escapeRadius = _viewport.Radius + _mainVisualizer.ParticleSize / 2 + 0.05f;

		foreach (Particle p in particles)
		{
			Vector3 direction = mousePosition - p.Position;
			float magnitude = direction.magnitude;

			if (AccelerationS > 0)
			{
				if (magnitude < ConsumeRadiusS)
				{
					float escapeDistance = escapeRadius + Random.Range(0, _viewport.Radius);

					Vector2 newPosition = Random.insideUnitCircle.normalized * escapeDistance;
					p.Position = newPosition;
					p.Velocity = Vector3.zero;
					continue;
				}
			}
			else
			{
				if (magnitude > escapeRadius && Random.value < Time.deltaTime)
				{
					p.Position = mousePosition + (Vector3)Random.insideUnitCircle * ConsumeRadiusS;
					p.Velocity = Random.insideUnitCircle.normalized * ConstantAttractionS;
					continue;
				}
			}

			direction /= magnitude;

			float normalVelocityLimit = magnitude * VelocityLimitS;

			// *Target* normal direction
			Vector3 normalDirection = new Vector3(direction.y, -direction.x);
			// Does particle's normal velocity align with target normal direction?
			float normalSign = Mathf.Sign(Vector2.Dot(p.Velocity, normalDirection));

			Vector3 normalComponent = normalDirection * Vector2.Dot(p.Velocity, normalDirection);
			normalComponent = Vector3.ClampMagnitude(normalComponent, normalVelocityLimit);
			Vector3 attractionComponent = direction * normalSign * (ConstantAttractionS + normalComponent.magnitude * normalComponent.magnitude * AttractionRatioS);

			p.Velocity = normalComponent + attractionComponent + Time.deltaTime * normalDirection * AccelerationS;
		}
	}

	public void InvokeAttractor(Vector2 point)
	{
		List<Particle> particles = _particles.Particles;

		foreach (Particle p in particles)
		{
			Vector2 direction = point - (Vector2)p.Position;
			float magnitude = direction.magnitude;
			direction /= magnitude;

			float acceleration = _attractionStrength * Mathf.Pow(magnitude, _attractionOrder);

			p.Velocity *= 1 - _attractionAssertion * Time.deltaTime;
			p.Velocity += (Vector3)(Time.deltaTime * acceleration * direction);
		}
	}
}
