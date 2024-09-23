using UnityEngine.EventSystems;
using System;

public class MonoSelectable : MonoDraggable, IPointerDownHandler
{
    private bool _isSelected;

    public bool Selected
    {
        get => _isSelected;
        set { if (_isSelected != value) { SetSelectedWithoutNotify(value); SelectedChanged?.Invoke(this, value); } }
    }

    public event Action<MonoSelectable, bool> SelectedChanged;

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        Selected = true;
    }

    public virtual void SetSelectedWithoutNotify(bool value)
    {
        _isSelected = value;
    }
}
