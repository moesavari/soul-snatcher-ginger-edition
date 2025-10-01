using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized logging with context prefixes, duplicate protection,
/// and a small in-memory ring buffer of recent logs.
/// </summary>
public static class DebugManager
{
    public static bool useSoftResetOnLose = false;

    // ---------- Public API ----------
    public enum LogLevel { Info, Warning, Error }

    public static IReadOnlyList<LogEntry> recent => _buffer;

    public struct LogEntry
    {
        public LogLevel level;
        public string message;
        public string contextName;
        public int contextId;
        public int frame;
        public DateTime time;
        public string stack;
    }

    public static void Log(string message, UnityEngine.Object context = null)
    {
        if (string.IsNullOrEmpty(message)) return;
        if (IsDuplicate(message, context)) return;

        _isOurLog = true;
        Debug.Log(Prefix(context) + " " + message, context);
        _isOurLog = false;

        AddToBuffer(LogLevel.Info, message, context, null);
    }

    public static void LogWarning(string message, UnityEngine.Object context = null)
    {
        if (string.IsNullOrEmpty(message)) return;
        if (IsDuplicate(message, context)) return;

        _isOurLog = true;
        Debug.LogWarning(Prefix(context) + " " + message, context);
        _isOurLog = false;

        AddToBuffer(LogLevel.Warning, message, context, null);
    }

    public static void LogError(string message, UnityEngine.Object context = null)
    {
        if (string.IsNullOrEmpty(message)) return;
        if (IsDuplicate(message, context)) return;

        _isOurLog = true;
        Debug.LogError(Prefix(context) + " " + message, context);
        _isOurLog = false;

        AddToBuffer(LogLevel.Error, message, context, null);
    }

    public static void LogLevelled(LogLevel level, string message, UnityEngine.Object context = null)
        => (level switch
        {
            LogLevel.Info => (Action<string, UnityEngine.Object>)Log,
            LogLevel.Warning => LogWarning,
            LogLevel.Error => LogError,
            _ => Log
        }).Invoke(message, context);

    public static void Clear() { _buffer.Clear(); }

    // ---------- Internals ----------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Init()
    {
        if (_subscribed) return;
        Application.logMessageReceived += OnUnityLog;
        _subscribed = true;
        _lastFrame = -1; _lastHash = 0;
    }

    private static string Prefix(UnityEngine.Object context)
        => context ? $"[{context.GetType().Name}]" : "[DebugManager]";

    private static void OnUnityLog(string condition, string stackTrace, LogType type)
    {
        if (_isOurLog) return;

        var level = type switch
        {
            LogType.Log => LogLevel.Info,
            LogType.Warning => LogLevel.Warning,
            LogType.Assert => LogLevel.Warning,
            LogType.Error => LogLevel.Error,
            LogType.Exception => LogLevel.Error,
            _ => LogLevel.Warning
        };

        AddToBuffer(level, condition, null, stackTrace);
    }
    public static void TriggerSoftReset()
    {
        // TODO: implement your reset-to-day/start logic here
        Debug.Log("TriggerSoftReset called");
    }

    // ---- Ring buffer (lightweight) ----
    private static void AddToBuffer(LogLevel level, string message, UnityEngine.Object context, string stack)
    {
        var entry = new LogEntry
        {
            level = level,
            message = message,
            contextName = context ? context.GetType().Name : string.Empty,
            contextId = context ? context.GetInstanceID() : 0,
            frame = Time.frameCount,
            time = DateTime.Now,
            stack = stack
        };

        if (_buffer.Count >= _maxEntries) _buffer.RemoveAt(0);
        _buffer.Add(entry);
    }

    // ---- One-frame duplicate guard (cheap & cheerful) ----
    private static bool IsDuplicate(string msg, UnityEngine.Object ctx)
    {
        int h = (msg?.GetHashCode() ?? 0) ^ (ctx ? ctx.GetInstanceID() : 0);
        if (Time.frameCount == _lastFrame && h == _lastHash) return true;
        _lastFrame = Time.frameCount;
        _lastHash = h;
        return false;
    }

    // ---------- State ----------
    private static bool _subscribed;
    private static readonly List<LogEntry> _buffer = new List<LogEntry>(128);
    private const int _maxEntries = 256;

    [ThreadStatic] private static bool _isOurLog;
    private static int _lastFrame;
    private static int _lastHash;
}
