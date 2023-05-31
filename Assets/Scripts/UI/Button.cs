using UnityEngine;
using TMPro;

namespace ConstellationUI
{
	public class Button : MonoBehaviour
	{
		[Header("Objects")]
		[SerializeField] private UnityEngine.UI.Button _button;
		[SerializeField] private TextMeshProUGUI _label;

		public event System.Action Click;

		public string TextLabel
		{
			get => _label == null ? null : _label.text;
			set { if (_label != null) _label.text = value; }
		}

		private void OnButtonClick() => Click?.Invoke();

		private void Start()
		{
			_button?.onClick.AddListener(OnButtonClick);
		}

		private void OnDestroy()
		{
			_button?.onClick.RemoveListener(OnButtonClick);
		}
	}
}