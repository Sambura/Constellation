using UnityEngine;
using System.Collections.Generic;

class WindowsManager : MonoBehaviour
{
	private List<MonoDialog> _windows = new List<MonoDialog>();

	private List<MonoDialog> _windowStack = new List<MonoDialog>();

	private void Awake()
	{
		gameObject.GetComponentsInChildren(true, _windows);

		foreach (MonoDialog dialog in _windows)
		{
			dialog.DialogOpened += OnDialogOpened;
			dialog.DialogClosed += OnDialogClosed;
			dialog.PointerDown += (_, __) => BringToTop(dialog);
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

	private void OnDialogClosed(MonoDialog window)
	{
		if (_windowStack.Contains(window))
		{
			_windowStack.Remove(window);
		}
	}
}
