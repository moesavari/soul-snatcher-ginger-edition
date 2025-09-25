using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class DebugManager : MonoSingleton<DebugManager>
{
    private enum LogLevel { Info, Warning, Error }

    [SerializeField] private int _maxEntries = 200;
    [SerializeField] private KeyCode _toggleKey = KeyCode.F1;
    [SerializeField] private KeyCode _clearKey = KeyCode.F2;
    [SerializeField] private KeyCode _copyKey = KeyCode.F3;
    [SerializeField] private bool _startHidden = true;
    [SerializeField] private bool _captureUnityDebug = true;

    [Header("Scene Reload settings")]
    [SerializeField] private bool _enableSoftResetHotkey = true;
    [SerializeField] private KeyCode _softResetKey = KeyCode.F9;
    [SerializeField] private float _softResetDayPause = 1.2f;
    [SerializeField] private bool _overrideLoseToSoftReset = false;

    private readonly List<(LogLevel level, string message)> _entries = new List<(LogLevel, string)>();
    private Vector2 _scroll;
    private bool _visible;
    private LogLevel _filter = LogLevel.Info;

    public bool useSoftResetOnLose => _overrideLoseToSoftReset;

    public static void Log(string msg) => Instance?.Add(LogLevel.Info, msg);
    public static void LogWarning(string msg) => Instance?.Add(LogLevel.Warning, msg);
    public static void LogError(string msg) => Instance?.Add(LogLevel.Error, msg);

    protected override void Awake()
    {
        base.Awake();
        _visible = !_startHidden;

        if (_captureUnityDebug)
        {
            Application.logMessageReceived -= OnUnityLog; // avoid double
            Application.logMessageReceived += OnUnityLog;
        }
    }

    private void OnDestroy()
    {
        if (_captureUnityDebug)
        {
            Application.logMessageReceived -= OnUnityLog;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey)) _visible = !_visible;
        if (Input.GetKeyDown(_clearKey)) _entries.Clear();
        if (Input.GetKeyDown(_copyKey)) GUIUtility.systemCopyBuffer = BuildAllText();

        if (_enableSoftResetHotkey && Input.GetKeyDown(_softResetKey))
        {
            TriggerSoftReset();
        }
    }

    private void OnGUI()
    {
        if (!_visible) return;

        const int margin = 10;
        var w = Mathf.Min(Screen.width - margin * 2, 720);
        var h = Mathf.Min(Screen.height - margin * 2, 380);
        var rect = new Rect(margin, margin, w, h);

        GUILayout.BeginArea(rect, GUI.skin.box);
        GUILayout.Label($"<b>Debug Manager</b>  [ {_entries.Count} entries ]  (Toggle:{_toggleKey}, Clear:{_clearKey}, Copy:{_copyKey})");

        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(_filter == LogLevel.Info, "Info", GUI.skin.button)) _filter = LogLevel.Info;
        if (GUILayout.Toggle(_filter == LogLevel.Warning, "Warning", GUI.skin.button)) _filter = LogLevel.Warning;
        if (GUILayout.Toggle(_filter == LogLevel.Error, "Error", GUI.skin.button)) _filter = LogLevel.Error;
        GUILayout.EndHorizontal();

        _scroll = GUILayout.BeginScrollView(_scroll);
        foreach (var (level, message) in _entries)
        {
            if (!Passes(level)) continue;
            var label = level switch
            {
                LogLevel.Info => $"[INFO] {message}",
                LogLevel.Warning => $"[WARN] {message}",
                LogLevel.Error => $"[ERR ] {message}",
                _ => WarnFallback($"[??? ] {message}")
            };
            GUILayout.Label(label);
        }
        GUILayout.EndScrollView();

        GUILayout.EndArea();
    }

    public void TriggerSoftReset()
    {
        StartCoroutine(SoftResetRoutine());
    }

    private IEnumerator SoftResetRoutine()
    {
        Log("[DebugManager] Soft reset starting...");

        GameEvents.RaiseDay();

        var wm = FindAnyObjectByType<WaveManager>(FindObjectsInactive.Exclude);
        if (wm != null) wm.ClearAllSpawns();

        var playerGO = GameManager.Instance ? GameManager.Instance.player?.gameObject : null;
        GameEvents.RaisePlayerSpawned(playerGO);

        yield return new WaitForSeconds(Mathf.Max(0f, _softResetDayPause));

        Log("[DebugManager] Raising NightStarted after soft reset.");
        GameEvents.RaiseNight();
    }

    private void Add(LogLevel level, string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;
        if (_entries.Count >= _maxEntries) _entries.RemoveAt(0);
        _entries.Add((level, msg));

        // Still output to Unity Console for dev convenience
        switch (level)
        {
            case LogLevel.Info: Debug.Log(msg); break;
            case LogLevel.Warning: Debug.LogWarning(msg); break;
            case LogLevel.Error: Debug.LogError(msg); break;
            default: Debug.LogWarning($"[DebugManager] Unknown level logged: {msg}"); break;
        }
    }

    private void OnUnityLog(string condition, string stackTrace, LogType type)
    {
        var level = type switch
        {
            LogType.Log => LogLevel.Info,
            LogType.Warning => LogLevel.Warning,
            LogType.Assert => LogLevel.Warning,
            LogType.Error => LogLevel.Error,
            LogType.Exception => LogLevel.Error,
            _ => WarnLevelFallback()
        };
        Add(level, $"{condition}");
    }

    private bool Passes(LogLevel level)
    {
        // Simple filter: show only selected level
        return level == _filter;
    }

    private string BuildAllText()
    {
        var sb = new StringBuilder(_entries.Count * 64);
        foreach (var (level, message) in _entries)
        {
            sb.AppendLine($"{level}: {message}");
        }
        return sb.ToString();
    }

    // Required by your rule #54: use modern switch with '_' fallback + warning.
    private string WarnFallback(string msg)
    {
        Debug.LogWarning("[DebugManager] Fallback label branch triggered.");
        return msg;
    }

    private LogLevel WarnLevelFallback()
    {
        Debug.LogWarning("[DebugManager] Fallback log level triggered.");
        return LogLevel.Info;
    }
}
