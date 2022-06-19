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

    public static Vector2 LocalToNormalizedPosition(RectTransform rectTransform, Vector2 local)
	{
        return local / rectTransform.rect.size + rectTransform.pivot;
    }

    public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

    public static Vector2 NormalizedToLocalPosition(RectTransform rectTransform, Vector2 normalized)
    {
        return (normalized - rectTransform.pivot) * rectTransform.rect.size;
    }

    public static Vector2 NormalizedToWorldPosition(RectTransform rectTransform, Vector2 normalized)
	{
        return rectTransform.localToWorldMatrix.MultiplyPoint3x4(NormalizedToLocalPosition(rectTransform, normalized));
    }

    public Vector2 NormalizedToWorldPosition(Vector2 normalized) => NormalizedToWorldPosition(_rectTransform, normalized);

    private void Awake()
	{
        _rectTransform = GetComponent<RectTransform>();
    }
}
