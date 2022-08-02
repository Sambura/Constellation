using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MonoDialog : MonoDraggable
{
    [SerializeField] protected Button _okButton;
	[SerializeField] protected Button _cancelButton;
	[SerializeField] protected TextMeshProUGUI _titleLabel;

    protected string _defaultDialogTitle;

    public System.Action<MonoDialog> OnOkClicked;
    public System.Action<MonoDialog> OnCancelClicked;

    public event System.Action<MonoDialog> DialogOpened;
    public event System.Action<MonoDialog> DialogClosed;

    protected override void Awake()
    {
        base.Awake();

        RestrictMovement = true;
        _okButton?.onClick.AddListener(OkButtonClick);
        _cancelButton?.onClick.AddListener(CancelButtonClick);
        _defaultDialogTitle = _titleLabel?.text;
    }

    protected virtual void OnDestroy()
    {
        _okButton?.onClick.RemoveListener(OkButtonClick);
        _cancelButton?.onClick.RemoveListener(CancelButtonClick);
    }

    public virtual void OkButtonClick()
	{
        OnOkClicked?.Invoke(this);
        OnOkClicked = null;
        CloseDialog(); 
    }

    public virtual void CancelButtonClick()
	{
        OnCancelClicked?.Invoke(this);
        OnCancelClicked = null;
        CloseDialog();
	}

    public virtual void ShowDialog(string title = null)
    {
        gameObject.SetActive(true);

        if (_titleLabel)
        {
            _titleLabel.text = title ?? _defaultDialogTitle;
        }

        DialogOpened?.Invoke(this);
    }

	public virtual void CloseDialog()
	{
        gameObject.SetActive(false);
        DialogClosed?.Invoke(this);
	}
}
