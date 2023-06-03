using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Class that represents a message box dialog
/// Message box has a title, icon, text message and buttons:
/// `Ok` and (optionally) `Cancel`
/// </summary>
public class MessageBox : MonoDialog
{
    [SerializeField] private TextMeshProUGUI _textField;
    [SerializeField] private Image _icon;

    /// <summary>
    /// Text message that is displayed in the message box
    /// </summary>
    public string Text { get => _textField.text; set => _textField.text = value; }
    /// <summary>
    /// Icon that is displayed in the message box
    /// Default icons are supposed to be defined in an instance of WindowsManager class
    /// </summary>
    public Sprite Icon { get => _icon.sprite; set => _icon.sprite = value; }
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
    public void ShowDialog(string title, Func<MonoDialog, bool, bool> onClose, string text, Sprite icon, bool cancelButton = false)
	{
        Text = text;
        Icon = icon;
        ShowCancelButton = cancelButton;
        ShowDialog(title, onClose);
	}
}
