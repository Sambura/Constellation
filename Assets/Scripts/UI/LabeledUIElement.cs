using UnityEngine;
using TMPro;

namespace ConstellationUI
{
	public class LabeledUIElement : MonoBehaviour
	{
		[Header("Label objects")]
		[SerializeField] private TextMeshProUGUI _label;

		public string LabelText
		{
			get => _label == null ? null : _label.text;
			set { if (_label != null) _label.text = value; }
		}
	}
}
