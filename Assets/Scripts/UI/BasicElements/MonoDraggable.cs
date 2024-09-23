﻿using UnityEngine;
using UnityEngine.EventSystems;
using System;

/// <summary>
/// The class that represents a UI element that can be dragged using mouse
/// Default implementation ignores all buttons except for left mouse button
/// </summary>
public class MonoDraggable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    /// <summary>
    /// Offset between mouse pointer and MonoDraggable's positions
    /// </summary>
    private Vector2 _dragOffset;
    /// <summary>
    /// Is MonoDraggable being dragged at the moment?
    /// </summary>
    private bool _isDragging;
    /// <summary>
    /// RectTransform that is attached to this gameObject
    /// </summary>
    protected RectTransform _transform;
    /// <summary>
    /// RectTransform that is attached to the parent gameObject
    /// </summary>
    protected RectTransform _parent;

    /// <summary>
    /// Event that is raised any time IPointerDownHandler emits its event
    /// That is, it is raised each time a mouse click is registered (not 
    /// necessarily left mouse buton)
    /// </summary>
    public event Action<MonoDraggable, PointerEventData> PointerDown;

    private bool _restrictMovement;
    /// <summary>
    /// Whether movement of MonoDraggable should be restricted by a parent container
    /// When true, the RectTransform attached to this gameObject will try to not go
    /// outside of the parent RectTransform (As long as position is changed via
    /// `Position` property of MonoDraggable
    /// </summary>
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

    /// <summary>
    /// The current world position of MonoDraggable
    /// </summary>
    public Vector3 Position
    {
        get => transform.position;
        set { if (transform.position != value) { SetPositionWithoutNotify(value); PositionChanged?.Invoke(value); } }
    }

    /// <summary>
    /// Raised any time `Position` property is set a new value
    /// </summary>
    public event Action<Vector3> PositionChanged;

    /// <summary>
    /// A function that sets value to `Position` property
    /// Does not raise PositionChanged event
    /// </summary>
    /// <param name="position">New position value</param>
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

    /// <summary>
    /// See IPointerDownHandler
    /// Raises PointerDown event
    /// </summary>
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        PointerDown?.Invoke(this, eventData);
        if (eventData.button != PointerEventData.InputButton.Left) return;

        _dragOffset = Position - (Vector3)eventData.position;
        _isDragging = true;
    }

    /// <summary>
    /// See IPointerUpHandler
    /// </summary>
    public virtual void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

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
