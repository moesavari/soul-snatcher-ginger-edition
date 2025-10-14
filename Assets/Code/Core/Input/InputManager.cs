using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-2000)]
public class InputManager : MonoSingleton<InputManager>
{
    // ====== EVENTS ======
    public static event Action<Vector2> Move;
    public static event Action MeleePressed;
    public static event Action RangedPressed;
    public static event Action<int> QuickbarPressed;   
    public static event Action ToggleSheetPressed;     
    public static event Action EscapePressed;
    public static event Action OnBindingsChanged;
    public static event Action InteractRescuePressed;
    public static event Action InteractSiphonPressed;

    private static int _lockCount = 0;
    public static bool isLocked => _lockCount > 0;

    // ====== BINDINGS ======
    public enum InputAction
    {
        ToggleEquipment, ToggleInventory,
        Melee, Ranged,
        Quick1, Quick2, Quick3, Quick4,
        InteractRescue,
        InteractSiphon,
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
        new Binding(InputAction.InteractRescue,  KeyCode.E),
        new Binding(InputAction.InteractSiphon,  KeyCode.Q),
        new Binding(InputAction.Melee,           KeyCode.Mouse0),
        new Binding(InputAction.Ranged,          KeyCode.Mouse1),
        new Binding(InputAction.Quick1,          KeyCode.Alpha1),
        new Binding(InputAction.Quick2,          KeyCode.Alpha2),
        new Binding(InputAction.Quick3,          KeyCode.Alpha3),
        new Binding(InputAction.Quick4,          KeyCode.Alpha4),
    };

    private readonly Dictionary<InputAction, Binding> _bindings = new Dictionary<InputAction, Binding>();

    // keep a single, stable prefs key
    private const string PREFS_KEY = "InputBindings";

    [Header("UI Roots (no faders)")]
    [SerializeField] private GameObject _equipmentRoot;   // InventoryHUD/EquipmentPanel
    [SerializeField] private GameObject _inventoryRoot;   // InventoryHUD/InventoryPanel
    [SerializeField] private bool _equipmentStartClosed = true;
    [SerializeField] private bool _inventoryStartClosed = true;

    private bool _equipmentOpen;
    private bool _inventoryOpen;

    // ---------- lifecycle ----------
    protected override void Awake()
    {
        base.Awake();
        LoadBindings();
    }

    private void Start()
    {
        // Initial visibility (simple SetActive)
        if (_equipmentRoot) { _equipmentOpen = !_equipmentStartClosed; _equipmentRoot.SetActive(_equipmentOpen); }
        if (_inventoryRoot) { _inventoryOpen = !_inventoryStartClosed; _inventoryRoot.SetActive(_inventoryOpen); }
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
        if (GetKeyDown(InputAction.Melee))          MeleePressed?.Invoke();
        if (GetKeyDown(InputAction.Ranged))         RangedPressed?.Invoke();
        if (GetKeyDown(InputAction.InteractRescue)) InteractRescuePressed?.Invoke();
        if (GetKeyDown(InputAction.InteractSiphon)) InteractSiphonPressed?.Invoke();
    }

    private void ReadQuickbar()
    {
        if (GetKeyDown(InputAction.Quick1)) QuickbarPressed?.Invoke(0);
        if (GetKeyDown(InputAction.Quick2)) QuickbarPressed?.Invoke(1);
        if (GetKeyDown(InputAction.Quick3)) QuickbarPressed?.Invoke(2);
        if (GetKeyDown(InputAction.Quick4)) QuickbarPressed?.Invoke(3);
    }

    private void ReadUI()
    {
        // Equipment toggle
        if (GetKeyDown(InputAction.ToggleEquipment))
        {
            _equipmentOpen = !_equipmentOpen;
            if (_equipmentRoot) _equipmentRoot.SetActive(_equipmentOpen);
            ToggleSheetPressed?.Invoke();
        }

        // Inventory toggle
        if (GetKeyDown(InputAction.ToggleInventory))
        {
            _inventoryOpen = !_inventoryOpen;
            if (_inventoryRoot) _inventoryRoot.SetActive(_inventoryOpen);
        }

        // ESC closes whichever is open (Equipment priority)
        if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
        {
            EscapePressed?.Invoke();
            if (_equipmentOpen && _equipmentRoot)       { _equipmentOpen = false; _equipmentRoot.SetActive(false); }
            else if (_inventoryOpen && _inventoryRoot)  { _inventoryOpen = false; _inventoryRoot.SetActive(false); }
        }
    }

    private bool GetKeyDown(InputAction a)
    {
        if(isLocked) return false;

        if (!_bindings.TryGetValue(a, out var b))
        {
            EnsureAllBindingsPresent();
            if (!_bindings.TryGetValue(a, out b))
            {
                // Only log once per action if you want; for now just return false quietly
                return false;
            }
            SaveBindings(); // persist the fix
        }
        return (b.primary != KeyCode.None && Input.GetKeyDown(b.primary)) ||
               (b.secondary != KeyCode.None && Input.GetKeyDown(b.secondary));
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

    // Force a reload from PlayerPrefs at runtime (for Settings/Debug)
    public static void ForceReload()
    {
        if (Instance == null) return;
        Instance.LoadBindings();
        OnBindingsChanged?.Invoke();
        LogBindings("ForceReload");
    }

}
