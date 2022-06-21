using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// MonoBehaviour that sends events from IPointerDownHandler and IPointerUpHandler
/// </summary>
public class UIEvents : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	public event Action<PointerEventData> PointerDown;
	public event Action<PointerEventData> PointerUp;

	public void OnPointerDown(PointerEventData eventData) { PointerDown?.Invoke(eventData); }
	public void OnPointerUp(PointerEventData eventData) { PointerUp?.Invoke(eventData); }
}
