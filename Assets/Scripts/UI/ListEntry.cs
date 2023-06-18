using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ConstellationUI
{
	public class ListEntry : MonoBehaviour
	{
		[SerializeField] private Image _icon;
		[SerializeField] private TextMeshProUGUI _label;
		[SerializeField] private Image _highlight;

		public bool Highlighted
		{
			get => _highlight.enabled;
			set => _highlight.enabled = value;
		}

		public Image Icon => _icon;

		public TextMeshProUGUI Label => _label;

		public object Data { get; set; }
	}
}