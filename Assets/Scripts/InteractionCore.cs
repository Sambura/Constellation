using UnityEngine;
using System.Collections.Generic;
using ConfigSerialization;
using UnityRawInput;

public class InteractionCore : MonoBehaviour
{
	[Header("Objects")]
	[SerializeField] private ParticleController _particles;
	[SerializeField] private Camera _camera;

	[Header("Parameters")]
	[SerializeField] private float _attractionOrder = 2;
	[SerializeField] private float _attractionStrength = 1;
	[SerializeField] private float _attractionAssertion = 0.01f;
	//[SerializeField] private float _driftStrength = 1;
	//[SerializeField] private float _decelerationStrength = 0.01f;
	//[SerializeField] private float _decelerationOrder = 0.5f;

	[SliderProperty(-10, 6, inputFormatting: "0.00")] public float AttractionOrder
	{
		get => _attractionOrder;
		set { if (_attractionOrder != value) { SetAttractionOrder(value); AttractionOrderChanged?.Invoke(value); } }
	}
	[SliderProperty(-2, 2)] public float AttractionStrength
	{
		get => _attractionStrength;
		set { if (_attractionStrength != value) { SetAttractionStrength(value); AttractionStrengthChanged?.Invoke(value); } }
	}
	[SliderProperty(-2, 2)] public float AttractionAssertion
	{
		get => _attractionAssertion;
		set { if (_attractionAssertion != value) { SetAttractionAssertion(value); AttractionAssertionChanged?.Invoke(value); } }
	}

	public event System.Action<float> AttractionOrderChanged;
	public event System.Action<float> AttractionStrengthChanged;
	public event System.Action<float> AttractionAssertionChanged;

	private void SetAttractionOrder(float value) { _attractionOrder = value; }
	private void SetAttractionStrength(float value) { _attractionStrength = value; }
	private void SetAttractionAssertion(float value) { _attractionAssertion = value; }

	public void Update()
	{
		Vector3 mousePosition = _camera.ScreenToWorldPoint(Input.mousePosition);
		mousePosition.z = 0;
		List<Particle> particles = _particles.Particles;

		if (Input.GetKey(KeyCode.A) || RawInput.IsKeyDown(RawKey.A)) // attract
		{
			foreach (Particle p in particles)
			{
				Vector3 direction = mousePosition - p.Position;
				float magnitude = direction.magnitude;
				direction /= magnitude;

				float acceleration = _attractionStrength * Mathf.Pow(magnitude, _attractionOrder);

				p.Velocity *= 1 - _attractionAssertion * Time.deltaTime;
				p.Velocity += Time.deltaTime * acceleration * direction;
			}
		}

		/*if (Input.GetMouseButton(0))
		{
			foreach (Particle p in particles)
			{
				Vector3 direction = mousePosition - p.Position;
				float magnitude = direction.magnitude;
				direction /= magnitude;
				Vector3 normal = new Vector3(-direction.y, direction.x);

				// float acceleration = Time.deltaTime / Mathf.Pow(magnitude, _attractionOrder);
				//p.Velocity += acceleration * _attractionStrength * direction + acceleration * _driftStrength * normal;

				float acceleration = Time.deltaTime * Mathf.Pow(Vector2.Dot(normal, p.Velocity), 2) / magnitude;
				// p.Velocity += acceleration * _attractionStrength * direction / (magnitude * magnitude);
				p.Velocity += Time.deltaTime * _attractionStrength * direction * Mathf.Pow(magnitude, _attractionOrder);
				p.Velocity -= p.Velocity * _decelerationStrength * Mathf.Clamp01(Mathf.Pow(magnitude, _decelerationOrder)) * Time.deltaTime;
			}
		}

		if (Input.GetMouseButton(1))
		{
			foreach (Particle p in particles)
			{
				Vector3 direction = mousePosition - p.Position;
				float magnitude = direction.magnitude;
				direction /= magnitude;
				Vector3 normal = new Vector3(-direction.y, direction.x);

				p.Velocity += Time.deltaTime * normal * _driftStrength + Time.deltaTime * direction * _driftStrength * 5;
			}
		}

		if (Input.GetMouseButton(2))
		{
			foreach (Particle p in particles)
			{
				Vector3 direction = mousePosition - p.Position;
				float magnitude = direction.magnitude;
				direction /= magnitude;
				Vector3 normal = new Vector3(-direction.y, direction.x);

				float value = Vector2.Dot(direction, p.Velocity);

				p.Velocity -= Time.deltaTime * direction * value * _decelerationStrength;
			}
		}*/
	}
}
