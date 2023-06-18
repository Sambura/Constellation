using UnityEngine;
using TMPro;

namespace ConstellationUI
{
    /// <summary>
    /// Class that represents a message box dialog
    /// Message box has a title, icon, text message and buttons:
    /// `Ok` and (optionally) `Cancel`
    /// </summary>
    public class InputFieldDialog : MonoDialog
    {
        [SerializeField] private TMP_InputField _inputField;

        /// <summary>
        /// Text message that is displayed in the message box
        /// </summary>
        public string InputString { get => _inputField.text; set => _inputField.text = value; }
        /// <summary>
        /// Whether message box should have a `cancel` button. For this to work, _cancelButton
        /// (located in MonoDialog class) field should be assigned properly.
        /// </summary>
        public bool ShowCancelButton
        {
            get => _cancelButton != null ? _cancelButton.gameObject.activeSelf : false;
            set { if (_cancelButton) _cancelButton.gameObject.SetActive(value); }
        }

        /// <summary>
        /// Shows a dialog, assigning title, text message and icon to message box
        /// </summary>
        public void ShowDialog(string title, OnDialogClosingHandler onClose, string inputString, bool cancelButton = false)
        {
            InputString = inputString;
            ShowCancelButton = cancelButton;
            ShowDialog(title, onClose);
        }
    }
}