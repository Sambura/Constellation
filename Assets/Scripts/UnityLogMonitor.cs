using ConfigSerialization.Structuring;
using ConfigSerialization;
using UnityEngine;
using ConstellationUI;

public class UnityLogMonitor : MonoBehaviour
{
    [SerializeField] private LogViewerDialog _logDialog;
    [SerializeField] private int _maxCharactersOutput = 100000;

    [ConfigGroupMember("Logging")] [ConfigProperty("Enable logging")]
    public bool LoggingEnabled
    {
        get => UnityLogger.LoggingEnabled;
        set => UnityLogger.LoggingEnabled = value;
    }

    public event System.Action<bool> LoggingEnabledChanged;

    private Color _bubbleColor = new Color(0, 0, 0, 0);

    [ConfigGroupMember(1, 0, Layout = ConfigGroupLayout.Horizontal, SetIndent = false)] [ConfigMemberOrder(1)] [ConfigProperty("")]
    public Color BubbleColor => _bubbleColor;

    public event System.Action<Color> BubbleColorChanged;

    private void Awake()
    {
        UnityLogger.LoggingEnabled = true;
        UnityLogger.GenerateRichText = true;
        UnityLogger.IncludeStackTrace = true;
        UnityLogger.AddTimestamps = true;
        UnityLogger.LogChanged += UpdateDialogText;
        UnityLogger.LoggingToggled += LoggingEnabledChanged;
    }

    private void UpdateDialogText()
    {
        Color initialColor = BubbleColor;

        _bubbleColor = UnityLogger.LogLength > 0 ? UnityLogger.GetLogTypeColor(UnityLogger.LastMessage.LogType) : new Color(0, 0, 0, 0);
        if (!_logDialog.DialogActive) { InvokeBubbleColorChanged(); return; }
        
        _bubbleColor = new Color(0, 0, 0, 0);
        _logDialog.Text = UnityLogger.GetLatestLog(_maxCharactersOutput);
        InvokeBubbleColorChanged();

        void InvokeBubbleColorChanged()
        {
            if (initialColor != _bubbleColor) BubbleColorChanged?.Invoke(BubbleColor);
        }
    }

    [ConfigGroupMember(1)] [InvokableMethod]
    public void ShowLogs()
    {
        if (_logDialog.DialogActive) return;

        _logDialog.ShowDialog();
        UpdateDialogText();
    }

    [ConfigGroupMember(1)] [InvokableMethod]
    [SetComponentProperty(typeof(UnityEngine.UI.Image), "color", typeof(Color), new object[] { 1, 0.6f, 0.6f }, "Border")]
    [SetComponentProperty(typeof(TMPro.TextMeshProUGUI), "color", typeof(Color), new object[] { 1, 0.2f, 0.2f })]
    public void ClearLogs() => UnityLogger.ClearLog();
}
