using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class ColorPickerButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _colorImage;

    public Color Color
	{
		get => _colorImage.color;
		set => _colorImage.color = value;
	}
	public event Action ButtonClick;

	public void OnPointerClick(PointerEventData eventData)
	{
		ButtonClick?.Invoke();
	}
}
