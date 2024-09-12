using UnityEngine;
using System;

/// <summary>
/// Note: I wrote this comment ages after I added this script, and most of this description is me guessing the functionality
/// 
/// Helper component that attaches to the game object with camera, to provide other objects in the scene
/// information about size of the viewport (in world units) (viewport is the rectangle that gets rendered on screen).
/// 
/// This may be useful to detect when the objects leaves/enters the camera's field of view for example.
/// 
/// Most probably this script relies on the fact that the camera is fixed at the origin (x/y axes)
/// Most probably is not useful for 3D applications
/// </summary>
[RequireComponent(typeof(Camera))]
public class Viewport : MonoBehaviour
{
	private Camera _camera;
	private float _aspect;

	/// <summary>
	/// The height (in world units) of the viewport. This value is precomputed
	/// </summary>
	public float Height { get; private set; }
	/// <summary>
	/// The width (in world units) of the viewport. This value is precomputed
	/// </summary>
	public float Width { get; private set; }
	/// <summary>
	/// Half of viewport's height. This value is precomputed
	/// </summary>
	public float MaxY { get; private set; }
	/// <summary>
	/// Half of viewport's width. This value is precomputed
	/// </summary>
	public float MaxX { get; private set; }
	/// <summary>
	/// Radius of circle that can inscribe the whole viewport
	/// </summary>
	public float Radius => Mathf.Sqrt(MaxX * MaxX + MaxY * MaxY);

	/// <summary>
	/// Camera that is being monitored by this viewport script
	/// </summary>
	public Camera Camera => _camera;

	public event Action CameraDimensionsChanged;

	private void Awake()
	{
		_camera = GetComponent<Camera>();
		UpdateTrackedVariables();
	}

	private void Update() => UpdateTrackedVariables();

	public void UpdateTrackedVariables()
	{
		float newAspect = _camera.aspect;
		if (_aspect != newAspect)
		{
			_aspect = newAspect;
			OnTrackedVariableChanged();
		}
	}

	private void OnTrackedVariableChanged()
	{
		MaxY = _camera.orthographicSize;
		MaxX = _aspect * _camera.orthographicSize;
		Height = MaxY * 2;
		Width = MaxX * 2;
		CameraDimensionsChanged?.Invoke();
	}
}
