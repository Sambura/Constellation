using UnityEngine;
using System;
using ConfigSerialization;
using ConfigSerialization.Structuring;
using System.Collections;
using Core;

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

    private long _startingTotalMemory;
    private long _currentTotalMemory;

    [ConfigGroupMember("Other", 2, GroupId = "Misc+other", SetDisplayIndex = -1)]
    [LabelProperty(typeof(BytesToStringConverter), DisplayPropertyName = true, AllowPolling = true)]
    public long MemoryDelta => _currentTotalMemory - _startingTotalMemory;

    private void Awake()
    {
        _pendingMode = Screen.fullScreenMode;
#if !UNITY_EDITOR
        // Look into this later, right now doesn't seem like good idea :(
        // GarbageCollector.GCMode = GarbageCollector.Mode.Manual;
#endif
        _startingTotalMemory = GC.GetTotalMemory(true);
        StartCoroutine(MemoryMonitor());
    }

    IEnumerator MemoryMonitor() {
        while (true) {
            _currentTotalMemory = GC.GetTotalMemory(false);
            yield return new WaitForSeconds(1f);
        }
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

public class BytesToStringConverter : IObjectConverter<string>
{
    public string Convert(object input)
    {
        double bytes = (long)input;

        if (bytes < 768) return $"{(int)bytes} B"; bytes /= 1024;
        if (bytes < 768) return $"{bytes:0.0} KB"; bytes /= 1024;
        if (bytes < 768) return $"{bytes:0.00} MB"; bytes /= 1024;
        if (bytes < 768) return $"{bytes:0.00} GB"; bytes /= 1024;
        
        return $"{bytes:0.000} TB";;
    }
}