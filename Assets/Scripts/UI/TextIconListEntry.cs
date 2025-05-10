using UnityEngine;
using UnityEngine.UI;

namespace ConstellationUI
{
    public class TextIconListEntry : ListEntryBase
    {
        [Header("Text/Icon List entry objects")]
        [SerializeField] private Image _icon;
        [SerializeField] private Image _highlight;

        public bool Highlighted
        {
            get => _highlight.enabled;
            set => _highlight.enabled = value;
        }

        public Image Icon => _icon;
    }
}