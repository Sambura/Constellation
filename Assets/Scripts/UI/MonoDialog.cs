using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MonoDialog : MonoDraggable
{
    [SerializeField] protected Button _okButton;
	[SerializeField] protected Button _cancelButton;
	[SerializeField] protected TextMeshProUGUI _titleLabel;

    protected string _defaultDialogTitle;
    protected RectTransform _transform;
    protected RectTransform _parent;

    public System.Action<MonoDialog> OnOkClicked;
    public System.Action<MonoDialog> OnCancelClicked;

    public event System.Action<MonoDialog> DialogOpened;
    public event System.Action<MonoDialog> DialogClosed;

    protected virtual void Awake()
    {
        _transform = GetComponent<RectTransform>();
        _parent = _transform.parent as RectTransform;
        _okButton?.onClick.AddListener(OkButtonClick);
        _cancelButton?.onClick.AddListener(CancelButtonClick);
        _defaultDialogTitle = _titleLabel?.text;
    }

    protected virtual void OnDestroy()
    {
        _okButton?.onClick.RemoveListener(OkButtonClick);
        _cancelButton?.onClick.RemoveListener(CancelButtonClick);
    }

    public override void SetPositionWithoutNotify(Vector3 position)
    {
        position = _parent.worldToLocalMatrix.MultiplyPoint3x4(position);

        float minX = _parent.rect.xMin + _transform.rect.width * _transform.pivot.x;
        float maxX = _parent.rect.xMax - _transform.rect.width * (1 - _transform.pivot.x);
        float minY = _parent.rect.yMin + _transform.rect.height * _transform.pivot.y;
        float maxY = _parent.rect.yMax - _transform.rect.height * (1 - _transform.pivot.y);

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);
        
        position = _parent.localToWorldMatrix.MultiplyPoint3x4(position);

        base.SetPositionWithoutNotify(position);
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
