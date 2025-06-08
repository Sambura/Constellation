using System.Runtime.CompilerServices;
using UnityCore;
using UnityEngine;
using UnityEngine.EventSystems;
using static Core.MathUtility;
using System;

public class GraphicControls : MonoBehaviour
{
    [SerializeField] private float _interactThreshold = 0.05f;
    [SerializeField] private Camera _camera;
    [SerializeField] private EventSystem _eventSystem;

    public static GraphicControls Instance { get; private set; }

    private static ObjectMeshRenderer Renderer;
    private static Camera MainCamera;
    private static EventSystem EventSystem;

    /// <summary>
    /// Max distance from control to mouse pointer to count as mouse hover
    /// </summary>
    private static float InteractThreshold;
    /// <summary>
    /// Reset to false every frame. Set to true as soon as a control is found that is hovered over
    /// </summary>
    private static bool FocusCaptured = false;
    /// <summary>
    /// Data of the caller on whose control user pressed previously. Reset upon mouse button up
    /// </summary>
    private static (string path, int line, object data)? LastPressed = null;
    /// <summary>
    /// Only updated when LastPressed is assigned or when interaction requiring mouse position takes place
    /// </summary>
    private static Vector2 MousePos;
    private static Vector2 LastMousePos;
    private static Vector2 MouseDelta;
    private static bool IsPointerObscured;
    private static bool IsMousePressed;

    private void Awake()
    {
        if (Instance is not null) {
            Debug.LogError("Multiple GraphicControls objects detected");
            return;
        }

        Instance = this;
        InteractThreshold = _interactThreshold;
        MainCamera = _camera;
        EventSystem = _eventSystem;
    }

    private void Start() { Renderer ??= ObjectMeshRenderer.Instance; }

    private void Update() { 
        FocusCaptured = false;
        MousePos = MainCamera.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButtonUp(0))
            LastPressed = null;

        IsPointerObscured = EventSystem.IsPointerOverGameObject();
        IsMousePressed = Input.GetMouseButton(0) && (!IsPointerObscured || LastPressed.HasValue);
    }
    
    private void LateUpdate() { if (!FocusCaptured) LastPressed = null; }

    private static bool SameCallData((string path, int line, object data) callData)
    {
        bool sameSource = LastPressed.HasValue &&
               callData.path == LastPressed.Value.path &&
               callData.line == LastPressed.Value.line;
        bool result = sameSource && CompareData(callData.data, LastPressed.Value.data);

        // if (!result && sameSource) Debug.Log($"Data mismatch for {callData.path} : {callData.line}");
        return result;

        bool CompareData(object a, object b) {
            Type type = a.GetType();
            if (type != b.GetType()) return false;
            if (type == typeof(Vector2)) {
                Vector2 A = (Vector2)a, B = (Vector2)b;
                const float tolerance = 1e-6f;
                return Vector2.Distance(A, B) <= tolerance;
            } else if (type == typeof(float)) {
                return MathF.Abs((float)a - (float)b) <= 1e-10;
            }

            return a.Equals(b);
        }
    }

    private static bool LogInteraction(bool hovered, (string, int, object) callData)
    {
        FocusCaptured |= hovered;

        if (IsMousePressed && hovered) {
            if (LastPressed is null && Input.GetMouseButtonDown(0)) {
                LastPressed = callData;
                LastMousePos = MousePos;
                MouseDelta = Vector2.zero;
            }
            else if (SameCallData(callData)) {
                MouseDelta = MousePos - LastMousePos;
                LastMousePos = MousePos;
                return true;
            }
        }

        return false;
    }

    public static Vector2 Arrow(Vector2 pos, Vector2 direction, Color color, bool interactable = true,
            [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
    {
        if (!interactable) { DrawVisual(color); return pos; }

        (string, int, object data) callData = (callerFilePath, callerLineNumber, pos);

        const float arrowHeadSize = 0.3f;
        const float arrowSharpness = 0.6f;

        bool hovered = IsMousePressed && SameCallData(callData);

        float length = direction.magnitude;
        if (!hovered && !FocusCaptured && !IsPointerObscured)
        {
            Vector2 posVector = MousePos - pos;
            float lineDistance = Mathf.Abs(Vector2.Dot(direction.Rotate90CCW(), posVector)) / length;
            float distance1 = Vector2.Distance(MousePos, pos);
            float distance2 = Vector2.Distance(MousePos, pos + direction);

            hovered = true;
            if (distance1 - InteractThreshold > length) hovered = false;
            else if (distance2 - InteractThreshold > length) hovered = false;
            else if (lineDistance > InteractThreshold && distance2 > arrowHeadSize * length * arrowSharpness) hovered = false;
        }

        color.a *= hovered ? (IsMousePressed ? 0.6f : 1) : 0.7f;
        DrawVisual(color);

        Vector2 newPos = pos;
        if (LogInteraction(hovered, callData)) {
            float projection = Vector2.Dot(MouseDelta, direction) / length;
            newPos += direction * projection / length;
            callData.data = newPos;
            LastPressed = callData;
        }

        return newPos;

        void DrawVisual(Color c) { Renderer.DrawArrow(pos, direction, null, c, arrowHeadSize, arrowSharpness); }
    }

    /// Note: for now angles not divisible by 90 are not rendered correctly
    public static Vector2 DragSquare(Vector2 pos, Color color, float size = 0.3f, float angleDegrees = 0, bool interactable = true,
            [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
    {
        float angle = angleDegrees * Mathf.Deg2Rad;
        Vector2 bottomLeft = pos, topRight = pos + Vector2.one.Rotate(angle) * size;
        if (!interactable) { DrawVisuals(color); return pos; }

        (string, int, object data) callData = (callerFilePath, callerLineNumber, pos);
        bool hovered = IsMousePressed && SameCallData(callData);

        if (!hovered && !FocusCaptured && !IsPointerObscured)
        {
            Vector2 center = (bottomLeft + topRight) / 2;
            Vector2 pointerVector = (MousePos - center).Rotate(-angle);
            hovered = new Rect(center.x - size / 2, center.y - size / 2, size, size).Contains(MousePos);
        }

        color.a *= hovered ? (IsMousePressed ? 0.3f : 0.7f) : 0.4f;
        DrawVisuals(color);

        Vector2 newPos = pos;
        if (LogInteraction(hovered, callData)) {
            newPos += MouseDelta;
            callData.data = newPos;
            LastPressed = callData;
        }

        return newPos;

        void DrawVisuals(Color color) {
            Renderer.DrawQuad(bottomLeft.x, bottomLeft.y, bottomLeft.x, topRight.y, topRight.x, topRight.y, topRight.x, bottomLeft.y, null, color);
            color.a = Mathf.Clamp01(color.a * 2.5f);
            Renderer.DrawLine(topRight.x, bottomLeft.y, topRight.x, topRight.y, null, color);
            Renderer.DrawLine(bottomLeft.x, topRight.y, topRight.x, topRight.y, null, color);
        }
    }

    public static Vector2 TranslatePosition(Vector2 pos)
    {
        pos = Arrow(pos, Vector2.right, Color.red);
        pos = Arrow(pos, Vector2.up, Color.green);
        pos = DragSquare(pos, Color.blue);
        return pos;
    }

    public static float CircleRadius(Vector2 center, float radius, Color color, bool dashed = false, bool interactable = true,
            [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
    {
        if (!interactable) {
            Renderer.DrawCircle(center, radius, dashed, null, color);
            return radius;
        }

        (string, int, object data) callData = (callerFilePath, callerLineNumber, radius);
        bool hovered = IsMousePressed && interactable && SameCallData(callData);

        Vector2 direction = MousePos - center;
        float distance = direction.magnitude;
        if (!hovered && !FocusCaptured && !IsPointerObscured)
            hovered = Mathf.Abs(distance - radius) <= InteractThreshold;

        color.a *= hovered ? (IsMousePressed ? 0.6f : 1) : 0.7f;
        Renderer.DrawCircle(center, radius, dashed, null, color);

        float newRadius = radius;
        if (LogInteraction(hovered, callData))
        {
            float lastDistance = (MousePos - MouseDelta - center).magnitude;
            float distanceDelta = distance - lastDistance;
            newRadius += distanceDelta;
            callData.data = newRadius;
            LastPressed = callData;
        }

        return newRadius;
    }

    public static Vector2 EllipseRadius(Vector2 center, Vector2 radius, Color color, bool dashed = false, bool interactable = true,
            [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
    {
        if (!interactable) {
            Renderer.DrawEllipse(center, radius.x ,radius.y, dashed, null, color);
            return radius;
        }

        (string, int, object data) callData = (callerFilePath, callerLineNumber, radius);
        bool hovered = IsMousePressed && SameCallData(callData);

        Vector2 direction = MousePos - center;
        float distance = direction.magnitude;
        double angle = Math.Atan2(direction.y, direction.x); // [-pi; pi]
        Vector2 point = GetEllipsePoint(radius.x, radius.y, (float)angle).ToVector2();
        if (!hovered && !FocusCaptured && !IsPointerObscured)
            hovered = Vector2.Distance(point, MousePos) <= InteractThreshold;

        color.a *= hovered ? (IsMousePressed ? 0.6f : 1) : 0.7f;
        Renderer.DrawEllipse(center, radius.x, radius.y, dashed, null, color);

        // This does not work perfectly but I would advise not to go here, but it does need to be fixed
        Vector2 newRadius = radius;
        if (LogInteraction(hovered, callData) && MouseDelta != Vector2.zero)
        {
            Vector2 lastMousePos = MousePos - MouseDelta;
            Vector2 offset = (lastMousePos - center).Abs() - point.Abs();
            Vector2 newPoint = (MousePos - center).Abs() - offset;

            Vector2 absDelta = MousePos.Abs() - lastMousePos.Abs();
            float x = MathF.Abs(newPoint.x), y = MathF.Abs(newPoint.y);
            float pa = MathF.Abs(radius.x + absDelta.x), pb = MathF.Abs(radius.y + absDelta.y);
            float c = (float)Math.Cos(angle), s = (float)Math.Sin(angle);
            float absAngle = MathF.Abs((float)angle);
            absAngle = MathF.Min(absAngle, MathF.PI - absAngle); // [0 deg ; 90 deg]
            bool xBase = absAngle < MathF.PI / 4;

            float coord = xBase ? x : y;
            float fr = xBase ? pb : pa;
            float fsr = xBase ? pa : pb;
            float frb = xBase ? radius.y : radius.x;
            float srb = xBase ? radius.x : radius.y;
            float scb = xBase ? absDelta.x : absDelta.y;
            float t1 = xBase ? c : s;
            float t2 = xBase ? s : c;

            float sr = fsr;
            const float changeTolerance = 10;
            for (int i = 0; i < 2; i++) {
                sr = GetSecondRadius(fr, coord, t1, t2);
                if (!float.IsNormal(sr)) { sr = fsr; break; }
                if (MathF.Abs(sr - srb) >= MathF.Abs(changeTolerance * scb))
                    fr = (frb + fr) / 2;
                else break;
            }

            newRadius = new Vector2(xBase ? sr : fr, xBase ? fr : sr);
            callData.data = newRadius;
            LastPressed = callData;
        }

        return newRadius;

        static float GetSecondRadius(double firstRadius, double coord, double trig1, double trig2) {
            double cs = coord * coord, rs = firstRadius * firstRadius, ts1 = trig1 * trig1, ts2 = trig2 * trig2;
            return (float)Math.Sqrt(Math.Abs(rs * ts1 * cs / (rs * ts1 - cs * ts2)));
        }
    }

    public static float Line(float x1, float y1, float x2, float y2, Color color, bool interactable = true,
        [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
    {
        if (!interactable) {
            Renderer.DrawLine(x1, y1, x2, y2, null, color);
            return 0;
        }

        Vector2 center = new Vector2(x1 + x2, y1 + y2) / 2;
        Vector2 normal = new Vector2(x2 - center.x, y2 - center.y).Rotate90CCW();

        Line(center, normal, color, out float delta, interactable, callerFilePath, callerLineNumber);
        return delta;
    }

    public static Vector2 Line(Vector2 pos, Vector2 normal, Color color, out float delta, bool interactable = true,
        [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
    {
        Vector2 direction = normal.Rotate90CCW();
        Vector2 point1 = pos + direction;
        Vector2 point2 = pos - direction;
        delta = 0;

        if (!interactable) {
            Renderer.DrawLine(point1.x, point1.y, point2.x, point2.y, null, color);
            return pos;
        }

        (string, int, object data) callData = (callerFilePath, callerLineNumber, pos);
        bool hovered = IsMousePressed && SameCallData(callData);
        
        float normalLength = normal.magnitude;
        float length = 2 * normalLength;
        if (!hovered && !FocusCaptured && !IsPointerObscured)
        {
            Vector2 posVector = MousePos - pos;
            float lineDistance = Mathf.Abs(Vector2.Dot(normal, posVector)) / normalLength;
            float distance1 = Vector2.Distance(MousePos, point1);
            float distance2 = Vector2.Distance(MousePos, point2);

            hovered = distance1 - InteractThreshold <= length && distance2 - InteractThreshold <= length && lineDistance <= InteractThreshold;
        }

        color.a *= hovered ? (IsMousePressed ? 0.6f : 1) : 0.7f;
        Renderer.DrawLine(point1.x, point1.y, point2.x, point2.y, null, color);

        Vector2 newPos = pos;
        if (LogInteraction(hovered, callData))
        {
            delta = Vector2.Dot(MouseDelta, normal) / normalLength;
            newPos += normal * delta / normalLength;
            callData.data = newPos;
            LastPressed = callData;
        }

        return newPos;
    }
}
