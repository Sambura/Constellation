using UnityEngine;
using System;

public class ApplicationController : MonoBehaviour
{
	public int TargetFrameRate
	{
		get => Application.targetFrameRate;
		set { if (Application.targetFrameRate != value) { Application.targetFrameRate = value; TargetFrameRateChanged?.Invoke(value); } }
	}

	private FullScreenMode _pendingMode;
	public FullScreenMode FullScreen
	{
		get => Screen.fullScreenMode;
		set
		{
			if (_pendingMode != value) {
				_pendingMode = value;
				if (value == FullScreenMode.ExclusiveFullScreen || value == FullScreenMode.FullScreenWindow)
				{
					Resolution max = Screen.resolutions[Screen.resolutions.Length - 1];
					Screen.SetResolution(max.width, max.height, value);
				}
				else Screen.fullScreenMode = value;
				FullScreenModeChanged?.Invoke(value);
			}
		}
	}

	public event Action<int> TargetFrameRateChanged;
	public event Action<FullScreenMode> FullScreenModeChanged;

	private void Awake()
	{
		_pendingMode = Screen.fullScreenMode;
	}

	public void Quit()
	{
#if DEBUG
		Debug.Break();
#else
		Application.Quit();
#endif
	}
}
