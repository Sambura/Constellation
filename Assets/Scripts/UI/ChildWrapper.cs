using UnityEngine;
using UnityCore;

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
			MonoEvents events = _referenceChild.gameObject.GetOrAddComponent<MonoEvents>();
			events.OnRectTransformChange += UpdateLayout;
			UpdateLayout();
		}

		public void UpdateLayout()
		{
			// This is kindof (?) a hack for it to work nicely with VerticalUILayout
			float y = (_transform.offsetMin.y + _transform.offsetMax.y) / 2;

			_transform.offsetMin = new Vector2(-_referenceChild.rect.width / 2 - _paddingLeft, y - _referenceChild.rect.height / 2 - _paddingBottom);
			_transform.offsetMax = new Vector2(_referenceChild.rect.width / 2 + _paddingRight, y + _referenceChild.rect.height / 2 + _paddingTop);
		}

		private void OnDestroy()
		{
			MonoEvents events = _referenceChild.gameObject.GetComponent<MonoEvents>();
			if (events != null) events.OnRectTransformChange -= UpdateLayout;
		}
	}
}
