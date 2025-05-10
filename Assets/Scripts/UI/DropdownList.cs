using UnityEngine;
using System.Collections.Generic;

namespace ConstellationUI
{
    public class DropdownList : LabeledUIElement
    {
        [Header("Objects")]
        [SerializeField] private CustomDropdown _dropdown;

        public List<CustomDropdown.OptionData> Options
        {
            get => _dropdown.options;
            set => _dropdown.options = value;
        }

        public int SelectedValue
        {
            get => _dropdown.value;
            set => _dropdown.value = value;
        }

        public event System.Action<int> SelectedValueChanged;

        public CustomDropdown WrappedDropdown => _dropdown;

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