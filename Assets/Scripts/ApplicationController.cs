using UnityEngine;
using System;
using ConfigSerialization;
using ConfigSerialization.Structuring;

public class ApplicationController : MonoBehaviour
{
    [ConfigGroupMember("Frames per second", GroupId = "AC+fps", SetDisplayIndex = 0)]
    [SliderProperty(30, 360, 0, name: "FPS limit")] public int TargetFrameRate
    {
        get => Application.targetFrameRate;
        set { if (Application.targetFrameRate != value) { Application.targetFrameRate = value; TargetFrameRateChanged?.Invoke(value); } }
    }

    private FullScreenMode _pendingMode;
    [ConfigGroupMember("Display", 1)]
    [DropdownListProperty(new object[] { FullScreenMode.ExclusiveFullScreen, FullScreenMode.FullScreenWindow, FullScreenMode.Windowed },
        new string[] { "Fullscreen", "Fullscreen window", "Windowed" }, "Fullscreen mode")] 
    public FullScreenMode FullScreenMode
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

    /// <summary>
    /// Close the application (or stop the play mode in editor)
    /// </summary>
    [ConfigGroupMember("Other", 2, GroupId = "Misc+other", SetDisplayIndex = -1)]
    [InvokableMethod("Exit")]
    public void Quit()
    {
#if DEBUG
        Debug.Break();
#else
        Application.Quit();
#endif
    }
}
