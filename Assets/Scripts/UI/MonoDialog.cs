using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MonoDialog : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] protected Button _okButton;

    private Vector2 _dragOffset;
    private bool _isDragging;

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
        gameObject.SetActive(false);
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        _dragOffset = transform.position - (Vector3)eventData.position;
        _isDragging = true;
    }

	public virtual void OnPointerUp(PointerEventData eventData)
	{
        _isDragging = false;
    }

    protected virtual void Update()
	{
        if (_isDragging == false) return;

        transform.position = (Vector2)Input.mousePosition + _dragOffset;
    }
}
