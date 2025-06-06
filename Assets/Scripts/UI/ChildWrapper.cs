﻿using UnityEngine;
using UnityCore;

namespace ConstellationUI
{
    /// <summary>
    /// Scales this component so that it wraps the referenced child object with specified padding
    /// E.g. put this on a button and set reference child to button text to scale button to text size
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ChildWrapper : MonoBehaviour
    {
        [SerializeField] private RectTransform _referenceChild;
        [SerializeField] private float _paddingTop;
        [SerializeField] private float _paddingLeft;
        [SerializeField] private float _paddingRight;
        [SerializeField] private float _paddingBottom;

        private RectTransform _transform;
        protected RectTransform RectTransform => _transform ?? (_transform = GetComponent<RectTransform>());

        private void Awake()
        {
            MonoEvents events = _referenceChild.gameObject.GetOrAddComponent<MonoEvents>();
            events.OnRectTransformChange += UpdateLayout;
        }

        private void OnEnable() => UpdateLayout();

        public void UpdateLayout()
        {
            if (!enabled) return;

            // This is kindof (?) a hack for it to work nicely with VerticalUILayout
            float y = (RectTransform.offsetMin.y + RectTransform.offsetMax.y) / 2;
            // ??????
            float x = (RectTransform.offsetMin.x + RectTransform.offsetMax.x) / 2;

            RectTransform.offsetMin = new Vector2(x - _referenceChild.rect.width / 2 - _paddingLeft, y - _referenceChild.rect.height / 2 - _paddingBottom);
            RectTransform.offsetMax = new Vector2(x + _referenceChild.rect.width / 2 + _paddingRight, y + _referenceChild.rect.height / 2 + _paddingTop);

            // ?????????????????????????
            GetComponent<MonoEvents>()?.InvokeRectTransformChange();

            // they have played us for absolute fools
        }

        private void OnDestroy()
        {
            MonoEvents events = _referenceChild.gameObject.GetComponent<MonoEvents>();
            if (events != null) events.OnRectTransformChange -= UpdateLayout;
        }
    }
}
