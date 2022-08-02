using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class MonoDraggable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Vector2 _dragOffset;
    private bool _isDragging;
    private bool _restrictMovement;
    protected RectTransform _transform;
    protected RectTransform _parent;

    public event Action<MonoDraggable, PointerEventData> PointerDown;

    public bool RestrictMovement
	{
        get => _restrictMovement;
        set
		{
            if (_restrictMovement == value) return;
            _restrictMovement = value;
            if (_restrictMovement) SetPositionWithoutNotify(Position);
		}
	}

	public Vector3 Position
	{
		get => transform.position;
		set { if (transform.position != value) { SetPositionWithoutNotify(value); PositionChanged?.Invoke(value); } }
	}

	public event Action<Vector3> PositionChanged;

	public virtual void SetPositionWithoutNotify(Vector3 position)
	{
        if (RestrictMovement)
        {
            position = _parent.worldToLocalMatrix.MultiplyPoint3x4(position);

            float minX = _parent.rect.xMin + _transform.rect.width * _transform.pivot.x;
            float maxX = _parent.rect.xMax - _transform.rect.width * (1 - _transform.pivot.x);
            float minY = _parent.rect.yMin + _transform.rect.height * _transform.pivot.y;
            float maxY = _parent.rect.yMax - _transform.rect.height * (1 - _transform.pivot.y);

            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);

            position = _parent.localToWorldMatrix.MultiplyPoint3x4(position);
        }

        transform.position = position;
	}

	public virtual void OnPointerDown(PointerEventData eventData)
    {
        _dragOffset = Position - (Vector3)eventData.position;
        _isDragging = true;
        PointerDown?.Invoke(this, eventData);
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

	protected virtual void Awake()
	{
        _transform = GetComponent<RectTransform>();
        _parent = _transform.parent as RectTransform;
    }
}
