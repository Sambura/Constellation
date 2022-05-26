using UnityEngine;
using UnityEngine.UI;

public class TabView : MonoBehaviour
{
    [SerializeField] private GameObject[] _tabActiveObjects;
    [SerializeField] private RectTransform[] _tabContents;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private bool _resetScrollRectPosition;
    [SerializeField] private bool _resizeScrollRectContent;
    [SerializeField] private int _selectedIndex;

    public void SelectTab(int index)
	{
        if (index == _selectedIndex) return;
        SetTabActive(_selectedIndex, false);
        _selectedIndex = index;
        SetTabActive(_selectedIndex, true);

        if (_resetScrollRectPosition)
		{
            _scrollRect.verticalNormalizedPosition = 1;
            _scrollRect.horizontalNormalizedPosition = 0;
		}

        if (_resizeScrollRectContent)
		{
            RectTransform content = _scrollRect.content;
            Rect targetRect = _tabContents[index].rect;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetRect.width);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetRect.height);
		}
	}

    private void SetTabActive(int index, bool isActive)
	{
        _tabActiveObjects[index].SetActive(isActive);
        _tabContents[index].gameObject.SetActive(isActive);
	}

	private void Start()
	{
		for (int i = 0; i < _tabContents.Length; i++)
		{
            SetTabActive(i, false);
		}

        SetTabActive(_selectedIndex, true);
	}
}
