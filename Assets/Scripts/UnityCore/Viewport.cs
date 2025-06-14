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

    /// <summary>
    /// The height (in world units) of the viewport. This value is precomputed
    /// </summary>
    public float Height { get; private set; }
    /// <summary>
    /// The width (in world units) of the viewport. This value is precomputed
    /// </summary>
    public float Width { get; private set; }
    /// <summary>
    /// Viewport area. Computed as Width x Height
    /// </summary>
    public float Area => Height * Width;
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
    /// Shorthand for Vector2(Width, Height). Full viewport size
    /// </summary>
    public Vector2 ViewportSize => new Vector2(Width, Height);
    /// <summary>
    /// How many pixels are there in one world unit?
    /// </summary>
    // note: presumably it is the same for height and width, but let's average just in case
    public float PixelsPerUnit => (PixelHeight / Height + PixelWidth / Width) / 2;
    /// <summary>
    /// How many world units are there in one pixel? This is usually less than 1
    /// </summary>
    public float UnitsPerPixel => 1 / PixelsPerUnit;
    /// <summary>
    /// Viewport aspect ratio (width / height)
    /// </summary>
    public float Aspect { get; private set; }
    public int PixelWidth { get; private set; }
    public int PixelHeight { get; private set; }

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
        if (Aspect != newAspect || MaxY != _camera.orthographicSize || PixelHeight != _camera.pixelHeight) {
            Aspect = newAspect;
            OnTrackedVariableChanged();
        }
    }

    private void OnTrackedVariableChanged()
    {
        MaxY = _camera.orthographicSize;
        MaxX = Aspect * _camera.orthographicSize;
        Height = MaxY * 2;
        Width = MaxX * 2;
        PixelWidth = _camera.pixelWidth;
        PixelHeight = _camera.pixelHeight;
        CameraDimensionsChanged?.Invoke();
    }

    /// <summary>
    /// Same as <see cref="Radius"/> but with custom specified center
    /// </summary>
    public float GetRadius(Vector2 worldPosition)
    {
        float radius = Radius;
        Vector2 diagonal = new Vector2(MaxX, MaxY);
        worldPosition = new Vector2(Mathf.Abs(worldPosition.x), Mathf.Abs(worldPosition.y));
        float projection = Vector2.Dot(diagonal, worldPosition) / radius;
        return projection + radius;
    }
}
