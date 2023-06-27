using UnityEngine;

public class UnityActionHelper : MonoBehaviour
{
	[SerializeField] private UnityEngine.Events.UnityEvent _onTrue;
	[SerializeField] private UnityEngine.Events.UnityEvent _onFalse;

	public void BoolAction(bool value)
	{
		(value ? _onTrue : _onFalse).Invoke();
	}
}
