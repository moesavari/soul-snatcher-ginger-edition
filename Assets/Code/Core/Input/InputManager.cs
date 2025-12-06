using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-2000)]
public class InputManager : MonoSingleton<InputManager>
{
    // ====== EVENTS ======
    public static event Action<Vector2> Move;
    public static event Action MeleePressed;
    public static event Action RangedPressed;
    public static event Action<int> QuickbarPressed;   
    public static event Action EscapePressed;
    public static event Action OnBindingsChanged;
    public static event Action InteractPressed;
    public static event Action SiphonPressed;
    public static event Action CancelPressed;
    public static event Action ToggleInventoryPressed;
    public static event Action ToggleEquipmentPressed;

    private static int _lockCount = 0;
    public static bool isLocked => _lockCount > 0;

    private static readonly HashSet<InputAction> _allowWhileLocked =
    new() {
        InputAction.Cancel,
        InputAction.ToggleInventory,
        InputAction.ToggleEquipment
    };

    // ====== BINDINGS ======
    public enum InputAction
    {
        ToggleEquipment, ToggleInventory,
        Melee, Ranged,
        Quick1, Quick2, Quick3, Quick4,
        Interact,
        Siphon,
        Cancel,
    }

    [Serializable]
    public class Binding
    {
        public InputAction action;
        public KeyCode primary;
        public KeyCode secondary;
        public Binding(InputAction a, KeyCode p, KeyCode s = KeyCode.None)
        { action = a; primary = p; secondary = s; }
    }

    [Header("Movement")]
    [SerializeField] private bool _useRawAxes = true;
    [SerializeField] private string _horizontalAxis = "Horizontal";
    [SerializeField] private string _verticalAxis = "Vertical";
    [SerializeField, Range(0f, 1f)] private float _deadZone = 0.05f;

    [Header("Bindings (defaults)")]
    [SerializeField]
    private List<Binding> _defaults = new List<Binding>()
    {
        new Binding(InputAction.ToggleEquipment, KeyCode.C),
        new Binding(InputAction.ToggleInventory, KeyCode.I),
        new Binding(InputAction.Interact,        KeyCode.E),
        new Binding(InputAction.Siphon,          KeyCode.Q),
        new Binding(InputAction.Melee,           KeyCode.Mouse0),
        new Binding(InputAction.Ranged,          KeyCode.Mouse1),
        new Binding(InputAction.Quick1,          KeyCode.Alpha1),
        new Binding(InputAction.Quick2,          KeyCode.Alpha2),
        new Binding(InputAction.Quick3,          KeyCode.Alpha3),
        new Binding(InputAction.Quick4,          KeyCode.Alpha4),
        new Binding(InputAction.Cancel,          KeyCode.Escape),
    };

    private readonly Dictionary<InputAction, Binding> _bindings = new Dictionary<InputAction, Binding>();

    private const string PREFS_KEY = "InputBindings";

    // ---------- lifecycle ----------
    protected override void Awake()
    {
        base.Awake();
        LoadBindings();
        UpgradeDefaultsIfNeeded();
        EnsureAllBindingsPresent();
    }

    private void Update()
    {
        ReadMovement();
        ReadActions();
        ReadQuickbar();
        ReadUI();
    }

    // ---------- readers ----------
    private void ReadMovement()
    {
        float h = _useRawAxes ? Input.GetAxisRaw(_horizontalAxis) : Input.GetAxis(_horizontalAxis);
        float v = _useRawAxes ? Input.GetAxisRaw(_verticalAxis)   : Input.GetAxis(_verticalAxis);
        var mv = new Vector2(h, v);
        if (mv.sqrMagnitude < _deadZone * _deadZone) mv = Vector2.zero;
        Move?.Invoke(mv);
    }

    private void ReadActions()
    {
        if (GetKeyDown(InputAction.Cancel)) CancelPressed?.Invoke();

        bool overUI = IsPointerOverUI();
        if (!overUI)
        {
            if (GetKeyDown(InputAction.Melee))      MeleePressed?.Invoke();
            if (GetKeyDown(InputAction.Ranged))     RangedPressed?.Invoke();
            if (GetKeyDown(InputAction.Interact))   InteractPressed?.Invoke();
            if (GetKeyDown(InputAction.Siphon))     SiphonPressed?.Invoke();
        }
    }

    private void ReadQuickbar()
    {
        if (!IsPointerOverUI())
        {
            if (GetKeyDown(InputAction.Quick1)) QuickbarPressed?.Invoke(0);
            if (GetKeyDown(InputAction.Quick2)) QuickbarPressed?.Invoke(1);
            if (GetKeyDown(InputAction.Quick3)) QuickbarPressed?.Invoke(2);
            if (GetKeyDown(InputAction.Quick4)) QuickbarPressed?.Invoke(3);
        }
    }

    private void ReadUI()
    {
        if (GetKeyDown(InputAction.ToggleEquipment))
            ToggleEquipmentPressed?.Invoke();

        if (GetKeyDown(InputAction.ToggleInventory))
            ToggleInventoryPressed?.Invoke();

        if (Input.GetKeyDown(KeyCode.Escape))
            EscapePressed?.Invoke();
    }

    private bool GetKeyDown(InputAction a)
    {
        if (isLocked && !_allowWhileLocked.Contains(a)) return false;

        if (!_bindings.TryGetValue(a, out var b))
        {
            EnsureAllBindingsPresent();
            _bindings.TryGetValue(a, out b);
        }

        if (b.primary == KeyCode.None && b.secondary == KeyCode.None) return false;

        return (b.primary != KeyCode.None && Input.GetKeyDown(b.primary))
            || (b.secondary != KeyCode.None && Input.GetKeyDown(b.secondary));
    }


    private static bool IsPointerOverUI()
    {
        var es = EventSystem.current;
        if(es == null) return false;

        return es.IsPointerOverGameObject();
    }

    // ---------- binding API ----------
    public static KeyCode GetPrimary(InputAction a)     => Instance._bindings[a].primary;
    public static KeyCode GetSecondary(InputAction a)   => Instance._bindings[a].secondary;

    public static void SetPrimary(InputAction a, KeyCode key)
    {
        Instance._bindings[a].primary = key;
        Instance.SaveBindings(); OnBindingsChanged?.Invoke();
    }
    public static void SetSecondary(InputAction a, KeyCode key)
    {
        Instance._bindings[a].secondary = key;
        Instance.SaveBindings(); OnBindingsChanged?.Invoke();
    }
    public static void ClearSecondary(InputAction a)
    {
        Instance._bindings[a].secondary = KeyCode.None;
        Instance.SaveBindings(); OnBindingsChanged?.Invoke();
    }
    public static void ResetToDefaults()
    {
        var im = Instance;
        im._bindings.Clear();
        foreach (var def in im._defaults)
            im._bindings[def.action] = new Binding(def.action, def.primary, def.secondary);
        im.SaveBindings();
        OnBindingsChanged?.Invoke();
        LogBindings("ResetToDefaults");
    }

    public static void WipeSavedBindings()
    {
        PlayerPrefs.DeleteKey(PREFS_KEY);
        PlayerPrefs.Save();
        Instance.LoadBindings();
        OnBindingsChanged?.Invoke();
        LogBindings("WipeSavedBindings");
    }

    public static void PushLock() => _lockCount++;
    public static void PopLock() => _lockCount = Mathf.Max(0, _lockCount - 1);

    // ---------- persistence ----------
    [Serializable] private class SaveBlob { public List<Binding> list = new List<Binding>(); }

    private void LoadBindings()
    {
        _bindings.Clear();
        string json = PlayerPrefs.GetString(PREFS_KEY, "");
        if (string.IsNullOrEmpty(json))
        {
            foreach (var def in _defaults)
                _bindings[def.action] = new Binding(def.action, def.primary, def.secondary);
            EnsureAllBindingsPresent();
            SaveBindings();
            return;
        }

        try
        {
            var blob = JsonUtility.FromJson<SaveBlob>(json);
            if (blob != null && blob.list != null && blob.list.Count > 0)
            {
                foreach (var b in blob.list)
                    _bindings[b.action] = new Binding(b.action, b.primary, b.secondary);
            }
            else
            {
                foreach (var def in _defaults)
                    _bindings[def.action] = new Binding(def.action, def.primary, def.secondary);
            }
        }
        catch
        {
            foreach (var def in _defaults)
                _bindings[def.action] = new Binding(def.action, def.primary, def.secondary);
        }

        // Always backfill and persist the repaired map
        EnsureAllBindingsPresent();
        SaveBindings();
    }


    private void SaveBindings()
    {
        var blob = new SaveBlob { list = new List<Binding>() };
        foreach (var kv in _bindings)
            blob.list.Add(new Binding(kv.Key, kv.Value.primary, kv.Value.secondary));
        PlayerPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(blob));
        PlayerPrefs.Save();
    }

    // --- DIAGNOSTICS ---
    public static void LogBindings(string label = "Bindings")
    {
        if (Instance == null) { DebugManager.LogWarning("Instance is null"); return; }
        var im = Instance;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"{label}:");
        foreach (var def in im._defaults)
        {
            if (!im._bindings.TryGetValue(def.action, out var b))
                sb.AppendLine($" - {def.action}: <missing>");
            else
                sb.AppendLine($" - {def.action}: primary={b.primary}, secondary={b.secondary}");
        }
        DebugManager.Log(sb.ToString());
    }

    // Backfill any missing keys (e.g. new actions added after the blob was saved)
    private void EnsureAllBindingsPresent()
    {
        foreach (var def in _defaults)
            if (!_bindings.ContainsKey(def.action))
                _bindings[def.action] = new Binding(def.action, def.primary, def.secondary);
    }

    private void UpgradeDefaultsIfNeeded()
    {
        bool changed = false;
        void Ensure(InputAction a, KeyCode primary, KeyCode secondary = KeyCode.None)
        {
            if (_defaults.Find(b => b.action == a).action != a)
            {
                _defaults.Add(new Binding(a, primary, secondary)); changed = true;
            }
        }
        Ensure(InputAction.Cancel, KeyCode.Escape, KeyCode.JoystickButton1);
        if (changed) SaveBindings();
    }


    // Force a reload from PlayerPrefs at runtime (for Settings/Debug)
    public static void ForceReload()
    {
        if (Instance == null) return;
        Instance.LoadBindings();
        OnBindingsChanged?.Invoke();
        LogBindings("ForceReload");
    }

}
