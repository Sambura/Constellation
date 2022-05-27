using UnityEngine;

public class MenuPresenter : MonoBehaviour
{
	[SerializeField] private GameObject _menuPanel;
	[SerializeField] private GameObject _menuOpenButton;

	public void OnMenuOpenButtonClick()
	{
		_menuPanel.SetActive(true);
		_menuOpenButton.SetActive(false);
	}

	public void OnMenuCloseButtonClick()
	{
		_menuPanel.SetActive(false);
		_menuOpenButton.SetActive(true);
	}

	private void Start()
	{
		if (_menuOpenButton.activeSelf) _menuPanel.SetActive(false);
	}
}
