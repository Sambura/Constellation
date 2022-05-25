using UnityEngine;

public class TabView : MonoBehaviour
{
    [SerializeField] private GameObject[] _tabActiveObjects;
    [SerializeField] private GameObject[] _tabContents;
    [SerializeField] private int _selectedIndex;

    public void SelectTab(int index)
	{
        if (index == _selectedIndex) return;
        SetTabActive(_selectedIndex, false);
        _selectedIndex = index;
        SetTabActive(_selectedIndex, true);
	}

    private void SetTabActive(int index, bool isActive)
	{
        _tabActiveObjects[index].SetActive(isActive);
        _tabContents[index].SetActive(isActive);
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
