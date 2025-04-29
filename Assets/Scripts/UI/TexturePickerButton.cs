using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using UnityCore;

namespace ConstellationUI
{
    public class TexturePickerButton : LabeledUIElement, IPointerClickHandler
    {
        [Header("Objects")]
        [SerializeField] private Image _spriteFrame;
        [SerializeField] private TexturePicker _texturePicker;

        [Header("Parameters")]
        [SerializeField] private Vector2 _windowOffset;
        [SerializeField] private Vector2 _windowPivot;
        [SerializeField] private string _dialogTitle = "Select texture";
        [SerializeField] private bool _findTexturePicker = true;

        public Texture2D Texture
        {
            get => _spriteFrame.sprite.texture;
            set { if (_spriteFrame.sprite.texture != value) { _spriteFrame.sprite = TextureManager.ToSprite(value); TextureChanged?.Invoke(value); } }
        }
        public event Action ButtonClick;
        public event Action<Texture2D> TextureChanged;

        public bool Interactable { get; set; } = true;

        private RectTransform _transform;

        public TexturePicker TexturePicker
        {
            get => _texturePicker != null ? _texturePicker : (_findTexturePicker ? _texturePicker = FindFirstObjectByType<TexturePicker>(FindObjectsInactive.Include) : null);
            /* Set can be added as needed, but proper support for dynamic property set may be a pain to implement */
        }

        public string DialogTitle
        {
            get => _dialogTitle;
            set
            {
                if (_dialogTitle == value) return;
                _dialogTitle = value;
                /* TODO add dynamic title change for color picker */
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Interactable == false) return;

            ButtonClick?.Invoke();
            OpenTexturePicker();
        }

        public void CloseTexturePicker()
        {
            if (TexturePicker.OnTextureChanged != OnTexturePickerTextureChange) return;

            TexturePicker.CloseDialog(false);
        }

        private void OnTexturePickerTextureChange(Texture2D texture) => Texture = texture;

        private void OpenTexturePicker()
        {
            Vector2 zeroPosition = (Vector2)_transform.position + _transform.rect.position;
            Vector2 pivotPosition = zeroPosition + _transform.rect.size * _windowPivot;
            TexturePicker.ShowDialog(_dialogTitle);
            TexturePicker.Position = _transform.position + (Vector3)_windowOffset;
            TexturePicker.OnTextureChanged = OnTexturePickerTextureChange;
            TexturePicker.Texture = Texture;
        }

        private void Awake()
        {
            _transform = GetComponent<RectTransform>();
        }
    }
}