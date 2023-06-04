using UnityEngine;

namespace ConstellationUI
{
	[RequireComponent(typeof(RectTransform))]
	public class ChildWrapper : MonoBehaviour
	{
		[SerializeField] private RectTransform _referenceChild;
		[SerializeField] private float _paddingTop;
		[SerializeField] private float _paddingLeft;
		[SerializeField] private float _paddingRight;
		[SerializeField] private float _paddingBottom;

		private RectTransform _transform;

		private void Start()
		{
			_transform = GetComponent<RectTransform>();
			MonoEvents events = _referenceChild.gameObject.AddComponent<MonoEvents>();
			events.OnRectTransformChange += UpdateLayout;
			UpdateLayout();
		}

		public void UpdateLayout()
		{
			_transform.offsetMin = new Vector2(-_referenceChild.rect.width / 2 - _paddingLeft, -_referenceChild.rect.height / 2 - _paddingBottom);
			_transform.offsetMax = new Vector2(_referenceChild.rect.width / 2 + _paddingRight, _referenceChild.rect.height / 2 + _paddingTop);
		}
	}
}
