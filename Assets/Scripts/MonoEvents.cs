using System;
using UnityEngine;

public class MonoEvents : MonoBehaviour
{
    public event Action OnObjectEnable;
    public event Action OnObjectDisable;
    public event Action OnRectTransformChange;

    private void OnEnable() => OnObjectEnable?.Invoke();
    private void OnDisable() => OnObjectDisable?.Invoke();
    private void OnRectTransformDimensionsChange() => OnRectTransformChange?.Invoke();
}
