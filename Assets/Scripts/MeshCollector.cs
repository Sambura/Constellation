using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCollector : MonoBehaviour
{
	[SerializeField] private ParticleController _particles;
	[SerializeField] private Material _mat;
	[SerializeField] private MeshFilter _meshFilter;
	[SerializeField] private Viewport _viewport;
	[SerializeField] private GameObject _prefab;
	// [SerializeField] private Shader _shader;

	private List<(Particle, Material)> _things = new List<(Particle, Material)>();

	public bool halt = false;

	protected static int _the_id = Shader.PropertyToID("_ParticlePosition");

	private void Start()
	{
		UpdateBackgroundQuad();
		_viewport.CameraDimensionsChanged += UpdateBackgroundQuad;

		if (halt) return;

		Material source = _prefab.GetComponent<MeshRenderer>().material;

		List<Particle> particles = _particles.Particles;

		for (int i = 0; i < particles.Count; i++)
		{
			GameObject go = Instantiate(_prefab);
			go.GetComponent<MeshCollector>().halt = true;
			MeshRenderer mr = go.GetComponent<MeshRenderer>();
			Material material = new Material(source);
			mr.material = material;
			_things.Add((particles[i], material));
		}

		_prefab.GetComponent<MeshRenderer>().enabled = false;
	}

	private void Update()
	{
		for (int i = 0; i < _things.Count; i++)
		{
			var thing = _things[i];
			thing.Item2.SetVector(_the_id, thing.Item1.Position);
		}
	}

	private void UpdateBackgroundQuad()
	{
		float extents = 0.1f;
		float width = _viewport.MaxX + extents;
		float height = _viewport.MaxY + extents;

		Mesh quad = new Mesh();
		quad.vertices = new Vector3[]
		{
			new Vector3(-width, -height),
			new Vector3(-width, height),
			new Vector3(width, height),
			new Vector3(width, -height)
		};

		quad.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
		quad.uv = new Vector2[] { 
			new Vector2(-width, -height),
			new Vector2(-width, height),
			new Vector2(width, height),
			new Vector2(width, -height)
		};

		_meshFilter.mesh = quad;
	}
}
