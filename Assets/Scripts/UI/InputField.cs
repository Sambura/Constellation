using UnityEngine;
using System;
using TMPro;

namespace ConstellationUI
{
    public class InputField : LabeledUIElement
    {
        [Header("Objects")]
        [SerializeField] protected TMP_InputField _inputField;

        public string Text
        {
            get => _inputField.text;
            set {
                if (value == _inputField.text) return;
                _inputField.text = value;
                TextChanged?.Invoke(value);
            }
        }

        public event Action<string> TextChanged;

        protected virtual void OnInputFieldTextChanged(string text)
        {
            TextChanged?.Invoke(_inputField.text);
        }

        protected virtual void Start()
        {
            _inputField.onValueChanged.AddListener(OnInputFieldTextChanged);
        }

        protected virtual void OnDestroy()
        {
            _inputField?.onValueChanged.RemoveListener(OnInputFieldTextChanged);
        }
    }
}
