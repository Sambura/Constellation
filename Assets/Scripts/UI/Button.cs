using UnityEngine;

namespace ConstellationUI
{
	public class Button : LabeledUIElement
	{
		[Header("Objects")]
		[SerializeField] private UnityEngine.UI.Button _button;

		public event System.Action Click;

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