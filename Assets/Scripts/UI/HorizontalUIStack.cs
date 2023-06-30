using UnityEngine;
using UnityCore;

/// <summary>
/// This works even worse than its vertical counterpart... GOOD
/// </summary>
public class HorizontalUIStack : MonoBehaviour
{
	[SerializeField] private float _spacing = 8;
	[SerializeField] private float _leftMargin = 3;
	[SerializeField] private bool _controlContainerHeight = true;

	public float Spacing { get => _spacing; set { _spacing = value; RebuildLayout(); } }
	public float LeftMargin { get => _leftMargin; set { _leftMargin = value; RebuildLayout(); } }
	public bool ControlContainerHeight { get => _controlContainerHeight; set { _controlContainerHeight = value; RebuildLayout(); } }

	private bool _layoutLock = false;
	private RectTransform _transform;
	protected RectTransform RectTransform => _transform ?? (_transform = GetComponent<RectTransform>());

	private void Start() => RegisterNewChildren();

	private void OnTransformChildrenChanged() => RebuildLayout();

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
		float x = _leftMargin;
		float containerHeight = 0;
;
		foreach (RectTransform child in RectTransform)
		{
			if (child.gameObject.activeInHierarchy == false) continue;
			if (child.anchorMax.x != child.anchorMin.x) continue;

			CalculateExtents(child, out float left, out float right);
			child.anchorMin = new Vector2(0, 1);
			child.anchorMax = new Vector2(0, 1);

			x -= left;
			child.anchoredPosition = new Vector2(x, child.anchoredPosition.y);
			x += right + _spacing;

			containerHeight = Mathf.Max(containerHeight, child.sizeDelta.y);

			_layoutLock = true;
			child.gameObject.GetComponent<ConstellationUI.ChildWrapper>()?.UpdateLayout();
			//child.gameObject.GetComponent<MonoEvents>()?.InvokeRectTransformChange();
			_layoutLock = false;
		}

		if (_controlContainerHeight)
			_transform.sizeDelta = new Vector2(_transform.sizeDelta.x, containerHeight);
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

	// Depth parameter is not implemented properly, and only works for values of 0 or 1. 
	private void CalculateExtents(RectTransform obj, out float leftExtent, out float rightExtent, int depth = 0)
	{
		leftExtent = -obj.rect.width * obj.pivot.x;
		rightExtent = obj.rect.width * (1 - obj.pivot.x);

		if (depth == 0) return;

		foreach (RectTransform child in obj)
		{
			if (child.gameObject.activeInHierarchy == false) continue;

			float localX = child.localPosition.x;
			float localLeft = localX - child.rect.width * child.pivot.x;
			float localRight = localX + child.rect.width * (1 - child.pivot.x);

			leftExtent = Mathf.Min(leftExtent, localLeft);
			rightExtent = Mathf.Max(rightExtent, localRight);
		}
	}
}
