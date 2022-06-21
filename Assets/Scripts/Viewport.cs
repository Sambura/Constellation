using UnityEngine;
using System;

[RequireComponent(typeof(Camera))]
public class Viewport : MonoBehaviour
{
	private Camera _camera;
	private float _aspect;

	public float Height { get; private set; }
	public float Width { get; private set; }
	public float MaxY { get; private set; }
	public float MaxX { get; private set; }

	public event Action CameraDimensionsChanged;

	private void Awake()
	{
		_camera = GetComponent<Camera>();
		UpdateTrackedVariables();
	}

	private void Update() => UpdateTrackedVariables();

	private void UpdateTrackedVariables()
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
