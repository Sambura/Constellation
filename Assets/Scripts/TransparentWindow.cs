using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityRawInput;
using SimpleGraphics;

public class TransparentWindow : MonoBehaviour
{
    [SerializeField] private MainVisualizer _visualizer;
    [SerializeField] private bool _autoRun = true;
#if UNITY_EDITOR
    [SerializeField] private bool _debugMessages = true;
#endif
    [SerializeField] private int UILayer;

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    private const int GWL_EXSTYLE = -20;

    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    [DllImport("user32.dll")]
    private static extern int SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

    private const uint LWA_COLORKEY = 0x00000001;

    [DllImport("Dwmapi.dll")]
    private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    private IntPtr hWnd;

    public void MakeTransparent()
    {
#if UNITY_EDITOR
        if (_debugMessages) Debug.Log("MakeTransparent() call");
#else
        hWnd = GetActiveWindow();

        MARGINS margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);

        SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        //SetLayeredWindowAttributes(hWnd, 0, 0, LWA_COLORKEY);

        SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, 0);
#endif

        Application.runInBackground = true;

        _visualizer.ColorBufferClearEnabled = true;
        enabled = true;

        FindObjectOfType<MainVisualizer>().ClearColor = new Color(0, 0, 0, 0);

        RawInput.Start();
        RawInput.WorkInBackground = true;
    }

    private void OnDisable()
    {
        RawInput.Stop();
    }

    private void SetClickthrough(bool clickthrough)
    {
#if UNITY_EDITOR
        if (_debugMessages)
            Debug.Log($"Setting to {(clickthrough ? "transparent" : "clickable")}");
#else
        SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | (clickthrough ? WS_EX_TRANSPARENT : 0u));
#endif
    }

    // will only run if the script is enabled
    private void Update()
    {
        // This one does not work when out of focus for whatever reason (double checked)
        //SetClickthrough(!EventSystem.current.IsPointerOverGameObject());

        SetClickthrough(!IsPointerOverUIElement());
    }

    //Returns 'true' if we touched or hovering on Unity UI element.
    public bool IsPointerOverUIElement()
    {
        return IsPointerOverUIElement(GetEventSystemRaycastResults());
    }

    //Returns 'true' if we touched or hovering on Unity UI element.
    private bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults)
    {
        for (int index = 0; index < eventSystemRaysastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRaysastResults[index];
            if (curRaysastResult.gameObject.layer == UILayer)
                return true;
        }
        return false;
    }

    //Gets all event system raycast results of current mouse or touch position.
    static List<RaycastResult> GetEventSystemRaycastResults()
    {
        if (EventSystem.current == null) {
            Debug.LogWarning("Transparent window: no event system detected");
            return new List<RaycastResult>();
        }

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        return raysastResults;
    }

    private void Start()
    {
        enabled = false;

        if (_autoRun) MakeTransparent();
    }
}