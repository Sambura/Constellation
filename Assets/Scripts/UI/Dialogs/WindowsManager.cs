using System;
using UnityEngine;
using System.Collections.Generic;

public class WindowsManager : MonoBehaviour
{
	[SerializeField] private List<NamedSprite> _messageBoxIcons;

	private List<MonoDialog> _windows = new List<MonoDialog>();

	private List<MonoDialog> _windowStack = new List<MonoDialog>();

	private MessageBox _messageBox;

	private void Awake()
	{
		gameObject.GetComponentsInChildren(true, _windows);

		foreach (MonoDialog dialog in _windows)
		{
			dialog.DialogOpened += OnDialogOpened;
			dialog.DialogClosed += OnDialogClosed;
			dialog.PointerDown += (_, __) => BringToTop(dialog);
			dialog.Manager = this;

			if (dialog is MessageBox messageBox)
				_messageBox = messageBox;
		}
	}

	public void BringToTop(MonoDialog window)
	{
		window.transform.SetAsLastSibling();
	}

	private void OnDialogOpened(MonoDialog window)
	{
		if (_windowStack.Contains(window))
		{
			_windowStack.Remove(window);
		}

		_windowStack.Add(window);
		BringToTop(window);
	}

	private void OnDialogClosed(MonoDialog window, bool result)
	{
		if (_windowStack.Contains(window))
		{
			_windowStack.Remove(window);
		}
	}

	public void ShowMessageBox(string title, string text, StandardMessageBoxIcons icon, MonoDialog parent = null)
	{
		ShowMessageBox(title, text, icon.ToString(), parent);
	}

	private Sprite GetMessageBoxIcon(string name)
	{
		for (int i = 0; i < _messageBoxIcons.Count; i++)
		{
			if (_messageBoxIcons[i].Name == name) return _messageBoxIcons[i].Sprite;
		}

		Debug.LogWarning($"Message box icon named {name} was not found!");
		return _messageBoxIcons[0].Sprite;
	}

	public void ShowMessageBox(string title, string text, string icon, MonoDialog parent = null)
	{
		if (_messageBox == null)
		{
			Debug.LogWarning("WindowsManager was asked to show message box, but no message boxes were found");
			return;
		}

		if (parent)
			_messageBox.Position = parent.transform.position;

		_messageBox.ShowDialog(title, null, text, GetMessageBoxIcon(icon), false);
	}

	public void ShowOkCancelMessageBox(string title, string text, StandardMessageBoxIcons icon, Func<bool, bool> callback, MonoDialog parent = null)
	{
		ShowOkCancelMessageBox(title, text, icon.ToString(), callback, parent);
	}

	public void ShowOkCancelMessageBox(string title, string text, string icon, Func<bool, bool> callback, MonoDialog parent = null)
	{
		if (_messageBox == null)
		{
			Debug.LogWarning("WindowsManager was asked to show message box, but no message boxes were found");
			return;
		}

		if (parent)
			_messageBox.Position = parent.transform.position;

		Func<MonoDialog, bool, bool> actualCallback = null;
		if (callback != null)
		{
			actualCallback = (x, y) => callback.Invoke(y);
		}
		_messageBox.ShowDialog(title, actualCallback, text, GetMessageBoxIcon(icon), true);
	}

	[Serializable]
	private struct NamedSprite
	{
		public string Name;
		public Sprite Sprite;
	}
}

public enum StandardMessageBoxIcons
{
	Info,
	Warning,
	Error,
	Question,
	Success,
}