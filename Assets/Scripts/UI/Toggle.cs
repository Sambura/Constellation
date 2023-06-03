using UnityEngine;
using System;
using UnityEngine.UI;

namespace ConstellationUI
{
	public class Toggle : LabeledUIElement
	{
		[Header("Objects")]
		[SerializeField] private UnityEngine.UI.Toggle _toggle;

		[Header("Parameters")]
		[SerializeField] private bool _isChecked;

		public bool IsChecked
		{
			get => _isChecked;
			set
			{
				if (value == _isChecked) return;
				SetIsCheckedWithoutNotify(value);
				IsCheckedChanged?.Invoke(_isChecked);
			}
		}

		public ToggleGroup ToggleGroup
		{
			get => _toggle.group;
			set => _toggle.group = value;
		}

		public event Action<bool> IsCheckedChanged;

		public void SetIsCheckedWithoutNotify(bool value)
		{
			_isChecked = value;
			_toggle.SetIsOnWithoutNotify(value);
		}

		private void OnToggleValueChanged(bool value)
		{
			IsChecked = value;
		}

		private void Start()
		{
			SetIsCheckedWithoutNotify(_isChecked);

			_toggle.onValueChanged.AddListener(OnToggleValueChanged);
		}

		private void OnDestroy()
		{
			_toggle?.onValueChanged.RemoveListener(OnToggleValueChanged);
		}
	}
}