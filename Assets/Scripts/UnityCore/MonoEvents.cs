using System;
using UnityEngine;

public class MonoEvents : MonoBehaviour
{
    public event Action OnObjectEnable;
    public event Action OnObjectDisable;
    public event Action OnRectTransformChange;

    public void InvokeRectTransformChange() { if (!enabled) return; OnRectTransformChange?.Invoke(); }
    public void InvokeObjectEnable() { if (!enabled) return; OnObjectEnable?.Invoke(); }
    public void InvokeObjectDisable() { if (!enabled) return; OnObjectDisable?.Invoke(); }

    private void OnEnable() => InvokeObjectEnable();
    private void OnDisable() => InvokeObjectDisable();
    private void OnRectTransformDimensionsChange() => InvokeRectTransformChange();
}
