using UnityEngine;
using UnityEngine.UIElements;

namespace ConstellationUI
{
    public class Button : LabeledUIElement
    {
        [Header("Objects")]
        [SerializeField] private UnityEngine.UI.Button _button;
        [SerializeField] private UnityEngine.UI.Image _icon;

        public event System.Action Click;
        
        public Sprite ButtonIcon
        {
            get => _icon?.sprite;
            set { if (_icon is { }) _icon.sprite = value; }
        }

        private void OnButtonClick() => Click?.Invoke();

        private void Start()
        {
            _button?.onClick.AddListener(OnButtonClick);
        }

        private void OnDestroy()
        {
            _button?.onClick.RemoveListener(OnButtonClick);
        }
    }
}