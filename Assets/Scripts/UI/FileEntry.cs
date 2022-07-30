using UnityEngine;
using UnityEngine.UI;

public class FileEntry : MonoBehaviour
{
    [SerializeField] private Image _highlight;

    public bool Highlighted
	{
		get => _highlight.enabled;
		set => _highlight.enabled = value;
	}

	public object Data;
}
