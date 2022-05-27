using UnityEngine;
using System;

public class ApplicationController : MonoBehaviour
{
	public int TargetFrameRate
	{
		get => Application.targetFrameRate;
		set { if (Application.targetFrameRate != value) { Application.targetFrameRate = value; TargetFrameRateChanged?.Invoke(value); } }
	}

	public event Action<int> TargetFrameRateChanged;

    public void Quit()
	{
#if DEBUG
		Debug.Break();
#else
		Application.Quit();
#endif
	}
}
