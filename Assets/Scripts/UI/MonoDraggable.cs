using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class MonoDraggable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Vector2 _dragOffset;
    private bool _isDragging;

	public Vector3 Position
	{
		get => transform.position;
		set { if (transform.position != value) { SetPositionWithoutNotify(value); PositionChanged?.Invoke(value); } }
	}

	public event Action<Vector3> PositionChanged;

	public virtual void SetPositionWithoutNotify(Vector3 position)
	{
        transform.position = position;
	}

	public virtual void OnPointerDown(PointerEventData eventData)
    {
        _dragOffset = Position - (Vector3)eventData.position;
        _isDragging = true;
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        _isDragging = false;
        Position = (Vector2)Input.mousePosition + _dragOffset;
    }

    protected virtual void Update()
    {
        if (_isDragging == false) return;

        Position = (Vector2)Input.mousePosition + _dragOffset;
    }
}
