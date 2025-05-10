using UnityEngine;
using System.Collections.Generic;

public class UIArranger : MonoBehaviour
{
    [SerializeField] private List<UIConfiguration> _configurations = new List<UIConfiguration>();
    [SerializeField] private int _selectedConfiguration;
    [SerializeField] private bool _allowSelfTransform = false;

    private RectTransform _transform;
    protected RectTransform RectTransform => _transform ?? (_transform = GetComponent<RectTransform>());

    public int SelectedConfigurationIndex {
        get => _selectedConfiguration;
        set => ApplyConfiguration(value);
    }

    public string SelectedConfigurationName
    {
        get => _configurations[_selectedConfiguration].Name;
        set => ApplyConfiguration(value);
    }	

    private void ApplyConfiguration(string name)
    {
        int index = _configurations.FindIndex(x => x.Name == name);
        if (index < 0) { Debug.LogError($"Cannot set configuration `{name}`: no such configuration"); return; }
        ApplyConfiguration(index);
    }

    private void ApplyConfiguration(int index)
    {
        _selectedConfiguration = index;
        UIConfiguration config = _configurations[index];

        foreach (RectTransformConfiguration element in config.UIElements)
        {
            RectTransform rt = element.Object;

            // be careful with changing offsets/anchors of the parent (i mean self). Let's only change the size
            if (rt != RectTransform || _allowSelfTransform) {
                rt.pivot = element.Pivot;
                rt.anchorMin = element.AnchorMin;
                rt.anchorMax = element.AnchorMax;
                rt.offsetMin = element.OffsetMin;
                rt.offsetMax = element.OffsetMax;
            } else {
                rt.sizeDelta = element.OffsetMax - element.OffsetMin;
            }

            rt.localRotation = element.LocalRotation;
        }
    }

    private void Start()
    {
        ApplyConfiguration(_selectedConfiguration);
    }

    [System.Serializable]
    public class UIConfiguration
    {
        public string Name;
        public List<RectTransformConfiguration> UIElements = new List<RectTransformConfiguration>();

        public void SyncAllElements()
        {
            foreach (var element in UIElements) element.SyncWithObject();
        }
    }

    [System.Serializable]
    public class RectTransformConfiguration
    {
        public RectTransform Object;
        public Vector2 Pivot;
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 OffsetMin;
        public Vector2 OffsetMax;
        public Quaternion LocalRotation = Quaternion.identity;

        public void RewriteFromObject(RectTransform obj)
        {
            Object = obj;
            Pivot = obj.pivot;
            AnchorMin = obj.anchorMin;
            AnchorMax = obj.anchorMax;
            OffsetMin = obj.offsetMin;
            OffsetMax = obj.offsetMax;
            LocalRotation = obj.localRotation;
        }

        public void SyncWithObject() =>	RewriteFromObject(Object);

        public static implicit operator RectTransformConfiguration(RectTransform obj) {
            var newConfig = new RectTransformConfiguration();
            newConfig.RewriteFromObject(obj);
            return newConfig;
        }
    }
}
