using System;
using UnityEngine;

public class MonoEvents : MonoBehaviour
{
    public event Action OnObjectEnable;
    public event Action OnObjectDisable;
    public event Action OnRectTransformChange;

    public void InvokeRectTransformChange() => OnRectTransformChange?.Invoke();
    public void InvokeObjectEnable() => OnObjectEnable?.Invoke();
    public void InvokeObjectDisable() => OnObjectDisable?.Invoke();

    private void OnEnable() => InvokeObjectEnable();
    private void OnDisable() => InvokeObjectDisable();
    private void OnRectTransformDimensionsChange() => InvokeRectTransformChange();
}
