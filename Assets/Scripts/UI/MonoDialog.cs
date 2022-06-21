using UnityEngine;
using UnityEngine.UI;

public class MonoDialog : MonoDraggable
{
    [SerializeField] protected Button _okButton;

    protected virtual void Awake()
    {
        _okButton?.onClick.AddListener(OnOkButtonPressed);
    }

    protected virtual void OnDestroy()
    {
        _okButton?.onClick.RemoveListener(OnOkButtonPressed);
    }

    protected virtual void OnOkButtonPressed()
    {
        CloseDialog();
    }

    public virtual void CloseDialog()
	{
        gameObject.SetActive(false);
	}
}
