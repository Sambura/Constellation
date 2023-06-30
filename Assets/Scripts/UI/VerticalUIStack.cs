using UnityEngine;
using UnityCore;

public class VerticalUIStack : MonoBehaviour
{
	[SerializeField] private float _spacing = 3;
	[SerializeField] private float _topMargin = 2;
	[SerializeField] private float _bottomMargin = 2;

	public float Spacing { get => _spacing; set { _spacing = value; RebuildLayout(); } }
	public float TopMargin { get => _topMargin; set { _topMargin = value; RebuildLayout(); } }
	public float BottomMargin { get => _bottomMargin; set { _bottomMargin = value; RebuildLayout(); } }

	private RectTransform _transform;
	private bool _layoutLock = false;
	protected RectTransform RectTransform => _transform ?? (_transform = GetComponent<RectTransform>());

	private void Start() => RegisterNewChildren();

	private void OnTransformChildrenChanged()
	{
		RebuildLayout();
	}

	public void RegisterNewChildren()
	{
		foreach (RectTransform child in RectTransform)
		{
			MonoEvents events = child.gameObject.GetOrAddComponent<MonoEvents>();

			events.OnObjectDisable -= RebuildLayout;
			events.OnObjectEnable -= RebuildLayout;
			events.OnRectTransformChange -= RebuildLayout;

			events.OnObjectDisable += RebuildLayout;
			events.OnObjectEnable += RebuildLayout;
			events.OnRectTransformChange += RebuildLayout;
		}

		RebuildLayout();
	}

	public void RebuildLayout()
	{
		if (_layoutLock) return;
		float y = -_topMargin;

		foreach (RectTransform child in RectTransform)
		{
			if (child.gameObject.activeInHierarchy == false) continue;
			if (child.anchorMax.y != child.anchorMin.y) continue;

			CalculateExtents(child, out float top, out float bottom);

			y -= top;
			child.anchoredPosition = new Vector2(child.anchoredPosition.x, y);
			y += bottom - _spacing;

			// if the element is not stretched horizontally, let's move it to the left
			if (child.anchorMax.x == child.anchorMin.x && child.anchorMin.x == 0)
			{
				float leftExtent = -child.rect.width * child.pivot.x;
				float rightExtent = child.rect.width * (1 - child.pivot.x);

				child.anchoredPosition = new Vector2(-leftExtent, child.anchoredPosition.y);
			}

			_layoutLock = true;
			child.gameObject.GetComponent<ConstellationUI.ChildWrapper>()?.UpdateLayout();
			//child.gameObject.GetComponent<MonoEvents>()?.InvokeRectTransformChange();
			_layoutLock = false;
		}

		y -= _bottomMargin;

		RectTransform.sizeDelta = new Vector2(RectTransform.sizeDelta.x, -y);
	}

	private void OnDestroy()
	{
		foreach (Transform child in transform)
		{
			MonoEvents events = child.gameObject.GetComponent<MonoEvents>();
			if (events == null) continue;
			Destroy(events);
		}
	}

	private void CalculateExtents(RectTransform obj, out float topExtent, out float bottomExtent)
	{
		topExtent = obj.rect.height * (1 - obj.pivot.y);
		bottomExtent = -obj.rect.height * obj.pivot.y;

		foreach (RectTransform child in obj)
		{
			if (child.gameObject.activeInHierarchy == false) continue;

			float localY = child.localPosition.y;
			float localTop = localY + child.rect.height * (1 - child.pivot.y);
			float localBottom = localY - child.rect.height * child.pivot.y;

			topExtent = Mathf.Max(topExtent, localTop);
			bottomExtent = Mathf.Min(bottomExtent, localBottom);
		}
	}
}
