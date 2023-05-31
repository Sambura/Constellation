using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace ConstellationUI
{
	public class DropdownList : MonoBehaviour
	{
		[Header("Objects")]
		[SerializeField] private CustomDropdown _dropdown;
		[SerializeField] private TextMeshProUGUI _label;

		public List<CustomDropdown.OptionData> Options
		{
			get => _dropdown.options;
			set => _dropdown.options = value;
		}

		public string TextLabel
		{
			get => _label == null ? null : _label.text;
			set { if (_label != null) _label.text = value; }
		}

		public int SelectedValue
		{
			get => _dropdown.value;
			set => _dropdown.value = value;
		}

		public event System.Action<int> SelectedValueChanged;

		public void SetOptions(List<string> options)
		{
			_dropdown.ClearOptions();
			_dropdown.AddOptions(options);
		}

		private void OnDropdownValueChanged(int value) => SelectedValueChanged?.Invoke(value);

		private void Start()
		{
			_dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
		}

		private void OnDestroy()
		{
			_dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
		}
	}
}