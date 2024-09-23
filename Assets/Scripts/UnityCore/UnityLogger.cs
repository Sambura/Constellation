using UnityEngine;
using System;
using System.Text;

/// <summary>
/// This class can listen to Unity messages and gather the logs - the same messages that appear in Unity Editor's console.
/// </summary>
public static class UnityLogger
{
    private static bool _loggingEnabled = false;
    private static int _logLimit = 5_000_000; // around 10 MB (?)
    private static int _stackTraceFontSize;
    private static string _stackTraceSizeTag; // this is updated by StackTraceFontSize setter
    private static StringBuilder _log;

    static UnityLogger()
    {
        _log = new StringBuilder(_logLimit, _logLimit);
        StackTraceFontSize = 12;
    }

    /// <summary>
    /// Whether the logger is currently gathering logs
    /// When this value is changed, LoggingToggled event is invoked
    /// </summary>
    public static bool LoggingEnabled
    {
        get => _loggingEnabled;
        set {
            if (_loggingEnabled == value) return;
            _loggingEnabled = value;

            if (value)
            {
                Application.logMessageReceived += OnMessageReceived;
            } else
            {
                Application.logMessageReceived -= OnMessageReceived;
            }
            
            LoggingToggled?.Invoke(value);
        }
    }

    /// <summary>
    /// Invoked when the logging is getting enabled / disabled
    /// </summary>
    public static event Action<bool> LoggingToggled;

    /// <summary>
    /// Invoked every time the log is updated
    /// </summary>
    public static event Action LogChanged;

    /// <summary>
    /// Maximum number of character to store in the logs
    /// Note, that if the logs exceed this number, they are automatically cleared
    /// </summary>
    public static int LogLimit
    {
        get => _logLimit;
        set
        {
            if (_logLimit == value) return;
            _logLimit = value;
            StringBuilder oldLog = _log; 
            _log = new StringBuilder(value, value);

            if (_log.Length <= value)
                _log.Append(oldLog);
            else
            {
                AppendLogSafe(LogsClearedMessage);
                LogChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// This message gets appended to the logs every time they were automatically cleared
    /// </summary>
    public static string LogsClearedMessage { get; set; } = "! Previous logs were cleared !\n";

    public static Color MessageColor { get; set; } = Color.white;
    public static Color WarningColor { get; set; } = Color.yellow;
    public static Color ErrorColor { get; set; } = new Color(0.878f, 0.1529f, 0.1529f);
    public static Color ExceptionColor { get; set; } = new Color(0.878f, 0.1529f, 0.1529f);
    public static Color AssertColor { get; set; } = new Color(0.878f, 0.1529f, 0.1529f);

    /// <summary>
    /// Whether logger should insert rich text tags in the log such as <color> or <size>
    /// </summary>
    public static bool GenerateRichText { get; set; } = true;

    /// <summary>
    /// Should the stack trace be included in the log when there is one
    /// </summary>
    public static bool IncludeStackTrace { get; set; } = true;

    /// <summary>
    /// Only applies if GenerateRichText == true. This is the size of font that is applied to stack trace text
    /// </summary>
    public static int StackTraceFontSize
    {
        get => _stackTraceFontSize;
        set
        {
            if (value == _stackTraceFontSize) return;
            _stackTraceFontSize = value;
            _stackTraceSizeTag = $"<size={value}>";
        }
    }

    public static string TimestampFormat { get; set; } = "[HH:mm:ss] ";

    /// <summary>
    /// Whether logger should mark all messages with timestamps or not
    /// </summary>
    public static bool AddTimestamps { get; set; } = true;

    /// <summary>
    /// Length of the full log currently stored
    /// </summary>
    public static int LogLength => _log.Length;

    /// <summary>
    /// Last log entry received by the logger
    /// </summary>
    public static UnityLogMessage LastMessage { get; private set; }

    /// <summary>
    /// Get the log gathered so far. The operation causes allocation of a large string.
    /// </summary>
    public static string GetLog(int startIndex = 0) => _log.ToString(startIndex, LogLength);
    /// <summary>
    /// Get the log gathered so far, starting at specified index and of specified length.
    /// </summary>
    public static string GetLog(int startIndex, int length) => _log.ToString(startIndex, length);
    /// <summary>
    /// Get the last `length` characters from the log. Incorrect length values are handled automatically.
    /// </summary>
    public static string GetLatestLog(int length) => _log.ToString(Mathf.Max(0, LogLength - length), Mathf.Min(length, LogLength));

    /// <summary>
    /// Clears all the collected logs
    /// </summary>
    public static void ClearLog()
    {
        _log.Clear();
        LogChanged?.Invoke();
    }

    /// <summary>
    /// Appends a given string to the _log StringBuilder. If there is not enough space left in
    /// the buffer, the given string is trimmed to fit. If the buffer is full, the given string
    /// is just discarded.
    /// Does not raise LogChanged event
    /// </summary>
    private static void AppendLogSafe(string str)
    {
        int spaceRemainig = _log.MaxCapacity - _log.Length;

        if (str.Length > spaceRemainig)
            _log.Append(str, str.Length - spaceRemainig, spaceRemainig);
        else 
            _log.Append(str);
    }

    /// <summary>
    /// Get the color associated with the given log type
    /// </summary>
    public static Color GetLogTypeColor(LogType type)
    {
        switch (type)
        {
            case LogType.Error: return ErrorColor;
            case LogType.Assert: return AssertColor;
            case LogType.Warning: return WarningColor;
            case LogType.Log: return MessageColor;
            case LogType.Exception: return ExceptionColor;
        }

        throw new NotImplementedException("Unknown LogType provided");
    }

    private static void OnMessageReceived(string logString, string stackTrace, LogType type)
    {
        LastMessage = new UnityLogMessage(logString, stackTrace, type);

        bool includeStackTrace = IncludeStackTrace && !string.IsNullOrWhiteSpace(stackTrace);

        //
        // Calculating new log entry length
        //

        int newEntryLength = logString.Length + "\n".Length;
        string timestamp = null;
        if (AddTimestamps)
        {
            timestamp = LastMessage.Timestamp.ToString(TimestampFormat);
            newEntryLength += timestamp.Length;
        }
        if (includeStackTrace) newEntryLength += stackTrace.Length + "\n".Length;
        string colorCode = null;
        if (GenerateRichText)
        {
            colorCode = GetColorCode(GetLogTypeColor(type));
            newEntryLength += "<color=#></color>".Length + colorCode.Length;
            if (includeStackTrace) newEntryLength += "</size>".Length + _stackTraceSizeTag.Length;
        }

        if (_log.Length + newEntryLength > _log.MaxCapacity)
        {
            _log.Clear();
            AppendLogSafe(LogsClearedMessage);
        }

        //
        // Actual log message
        // 

        if (GenerateRichText)
        {
            AppendLogSafe("<color=#");
            AppendLogSafe(colorCode);
            AppendLogSafe(">");
        }
        if (AddTimestamps) AppendLogSafe(timestamp);
        AppendLogSafe(logString);
        AppendLogSafe("\n");
        if (includeStackTrace)
        {
            if (GenerateRichText) AppendLogSafe(_stackTraceSizeTag);
            AppendLogSafe(stackTrace);
            if (GenerateRichText) AppendLogSafe("</size>");
            AppendLogSafe("\n");
        }
        if (GenerateRichText) AppendLogSafe("</color>");

        LogChanged?.Invoke();
        
        //

        static string GetColorCode(Color color)
        {
            if (color.a == 1) return ColorUtility.ToHtmlStringRGB(color);
            return ColorUtility.ToHtmlStringRGBA(color);
        }
    }

    public struct UnityLogMessage
    {
        public string Message;
        public string StackTrace;
        public LogType LogType;
        public DateTime Timestamp;

        public UnityLogMessage(string message, string stackTrack, LogType type)
        {
            Message = message;
            StackTrace = stackTrack;
            LogType = type;
            Timestamp = DateTime.Now;
        }
    }
}
