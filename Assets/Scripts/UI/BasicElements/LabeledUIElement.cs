using UnityEngine;
using TMPro;

namespace ConstellationUI
{
    public class LabeledUIElement : MonoBehaviour
    {
        [Header("Label objects")]
        [SerializeField] private TextMeshProUGUI _label;

        private RectTransform _rectTransform;

        public virtual string LabelText
        {
            get => WrappedLabel?.text;
            set { if (WrappedLabel is { }) WrappedLabel.text = value; }
        }
        public RectTransform RectTransform => _rectTransform ?? (_rectTransform = GetComponent<RectTransform>());

        public virtual TextMeshProUGUI WrappedLabel => _label;
    }
}
