using UnityEngine;

public class VerticalUIStack : MonoBehaviour
{
	[SerializeField] private float _spacing = 3;
	[SerializeField] private float _topMargin = 2;
	[SerializeField] private float _bottomMargin = 2;

	public float Spacing { get => _spacing; set { _spacing = value; RebuildLayout(); } }
	public float TopMargin { get => _topMargin; set { _topMargin = value; RebuildLayout(); } }
	public float BottomMargin { get => _bottomMargin; set { _bottomMargin = value; RebuildLayout(); } }

	private RectTransform _transform;

	private void Awake()
	{
		_transform = GetComponent<RectTransform>();
	}

	private void Start()
	{
		foreach (RectTransform child in _transform)
		{
			MonoEvents events = child.gameObject.AddComponent<MonoEvents>();
			events.OnObjectDisable += RebuildLayout;
			events.OnObjectEnable += RebuildLayout;
			events.OnRectTransformChange += RebuildLayout;
		}

		RebuildLayout();
	}

	private void OnTransformChildrenChanged()
	{
		RebuildLayout();
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

	private void RebuildLayout()
	{
		float y = -_topMargin;

		foreach (RectTransform child in _transform)
		{
			if (child.gameObject.activeInHierarchy == false) continue;
			if (child.anchorMax.y != child.anchorMin.y) continue;

			CalculateExtents(child, out float top, out float bottom);

			y -= top;
			child.anchoredPosition = new Vector2(child.anchoredPosition.x, y);
			y += bottom - _spacing;
		}

		y -= _bottomMargin;

		_transform.sizeDelta = new Vector2(_transform.sizeDelta.x, -y);
	}

	private void CalculateExtents(RectTransform obj, out float topExtent, out float bottomExtent)
	{
		float top = obj.rect.height * (1 - obj.pivot.y);
		float bottom = -obj.rect.height * obj.pivot.y;

		foreach (RectTransform child in obj)
		{
			if (child.gameObject.activeInHierarchy == false) continue;

			float localY = child.localPosition.y;
			float localTop = localY + child.rect.height * (1 - child.pivot.y);
			float localBottom = localY - child.rect.height * child.pivot.y;

			top = Mathf.Max(top, localTop);
			bottom = Mathf.Min(bottom, localBottom);
		}

		topExtent = top;
		bottomExtent = bottom;
	}
}
