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

		private void Awake()
		{
			_transform = GetComponent<RectTransform>();
			MonoEvents events = _referenceChild.gameObject.GetOrAddComponent<MonoEvents>();
			events.OnRectTransformChange += UpdateLayout;
		}

		private void OnEnable()
		{
			UpdateLayout();
		}

		public void UpdateLayout()
		{
			if (!enabled) return;

			// This is kindof (?) a hack for it to work nicely with VerticalUILayout
			float y = (_transform.offsetMin.y + _transform.offsetMax.y) / 2;
			// ??????
			float x = (_transform.offsetMin.x + _transform.offsetMax.x) / 2;

			_transform.offsetMin = new Vector2(x - _referenceChild.rect.width / 2 - _paddingLeft, y - _referenceChild.rect.height / 2 - _paddingBottom);
			_transform.offsetMax = new Vector2(x + _referenceChild.rect.width / 2 + _paddingRight, y + _referenceChild.rect.height / 2 + _paddingTop);

			// ?????????????????????????
			GetComponent<MonoEvents>()?.InvokeRectTransformChange();

			// they have played us for absolute fools
		}

		private void OnDestroy()
		{
			MonoEvents events = _referenceChild.gameObject.GetComponent<MonoEvents>();
			if (events != null) events.OnRectTransformChange -= UpdateLayout;
		}
	}
}
