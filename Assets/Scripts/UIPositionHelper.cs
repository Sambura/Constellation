using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class UIPositionHelper : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public Vector2 PointerPosition
    {
        get => _pointerPosition;
        private set
		{
            _pointerPosition = value;
            PointerPositionChanged?.Invoke(value);
        }
    }
    public event Action<Vector2> PointerPositionChanged;
    public Vector2 PointerPositionNormalized { get; private set; }

    private Vector2 _pointerPosition;
    private RectTransform _rectTransform;

	public void OnDrag(PointerEventData eventData)
	{
        Vector3 clampedPosition = eventData.position;

        Vector2 preNormalized = _rectTransform.worldToLocalMatrix.MultiplyPoint3x4(clampedPosition)
            / _rectTransform.rect.size + _rectTransform.pivot;

        preNormalized.x = Mathf.Clamp01(preNormalized.x);
        preNormalized.y = Mathf.Clamp01(preNormalized.y);

        PointerPositionNormalized = preNormalized;

        PointerPosition = NormalizedToWorldPosition(preNormalized);
    }

    public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

    public Vector2 NormalizedToWorldPosition(Vector2 normalized)
	{
        return _rectTransform.localToWorldMatrix.MultiplyPoint3x4((normalized - _rectTransform.pivot) * _rectTransform.rect.size);
    }

	private void Awake()
	{
        _rectTransform = GetComponent<RectTransform>();
    }
}
