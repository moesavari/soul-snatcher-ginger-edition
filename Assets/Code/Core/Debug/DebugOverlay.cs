using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
/// Simple on-screen dev overlay with handy buttons.
/// Shows only in Editor or Development builds.
[DefaultExecutionOrder(10000)]
public class DebugOverlay : MonoBehaviour
{
    [Header("UI Panels (optional)")]
    [SerializeField] private GameObject equipmentPanel;   // InventoryHUD/EquipmentPanel
    [SerializeField] private GameObject inventoryPanel;   // InventoryHUD/InventoryPanel

    [Header("Overlay")]
    [SerializeField] private bool startVisible = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F10;
    [SerializeField] private float width = 300f;

    [Header("Gameplay Refs")]
    [SerializeField] private ReputationMeterController _reputation; // drag if you want; otherwise auto-find

    private bool _visible;
    private Rect _rect;

    // Reputation debug input
    [SerializeField] private string _repInput = "0";  // textfield contents
    [SerializeField] private float _repStep = 10f;    // quick step

    private void Awake()
    {
        _visible = startVisible;
        _rect = new Rect(10, 10, width, 10);

        if (_reputation == null)
            _reputation = FindObjectOfType<ReputationMeterController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey)) _visible = !_visible;
    }

    private void OnGUI()
    {
        // Tiny reopen button when hidden
        if (!_visible)
        {
            var btnRect = new Rect(10, 10, 70, 22);
            if (GUI.Button(btnRect, "≡ Debug")) _visible = true;
            return;
        }

        // window height grows with content; passing 0 lets GUILayout calculate it
        _rect = GUILayout.Window(GetInstanceID(), _rect, DrawWindow, "Debug Overlay");
    }

    private void DrawWindow(int id)
    {
        GUILayout.Space(4);

        // --- Reputation ------------------------------------------------------
        GUILayout.Label("<b>Reputation</b>", Rich());
        if (_reputation == null)
        {
            GUILayout.Label("<color=orange>No ReputationMeterController found.</color>", Rich());
            if (GUILayout.Button("Find Now"))
            {
                _reputation = FindObjectOfType<ReputationMeterController>();
            }
        }
        else
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Value:", GUILayout.Width(44));
            _repInput = GUILayout.TextField(_repInput, GUILayout.MinWidth(80));
            if (GUILayout.Button("Set", GUILayout.Width(50)))
            {
                if (float.TryParse(_repInput, out var v))
                    _reputation.AnimateTo(v);
                else
                    Debug.LogWarning("DebugOverlay: Invalid reputation value text.");
            }
            if (GUILayout.Button("Neutral (0)", GUILayout.Width(90)))
            {
                _repInput = "0";
                _reputation.AnimateTo(0f);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-" + _repStep, GUILayout.Width(60)))
            {
                float v = ParseOrZero(_repInput) - _repStep;
                _repInput = v.ToString("0");
                _reputation.AnimateTo(v);
            }
            if (GUILayout.Button("+" + _repStep, GUILayout.Width(60)))
            {
                float v = ParseOrZero(_repInput) + _repStep;
                _repInput = v.ToString("0");
                _reputation.AnimateTo(v);
            }
            if (GUILayout.Button("Snap Current", GUILayout.Width(100)))
            {
                // read back current displayed value from controller if you want to sync the field
                _repInput = (_reputation != null ? _reputation.value.ToString("0.##") : _repInput);
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(8);

        // --- Panels ----------------------------------------------------------
        GUILayout.Label("<b>Panels</b>", Rich());
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Toggle Equipment")) Toggle(equipmentPanel);
        if (GUILayout.Button("Toggle Inventory")) Toggle(inventoryPanel);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Close Equipment")) SetActive(equipmentPanel, false);
        if (GUILayout.Button("Close Inventory")) SetActive(inventoryPanel, false);
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        // --- Input Bindings --------------------------------------------------
        GUILayout.Label("<b>Input Bindings</b>", Rich());
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset to Defaults"))
        {
            InputManager.ResetToDefaults();
            DebugManager.Log("Input bindings reset to defaults.", this);
        }
        if (GUILayout.Button("Wipe Saved Bindings"))
        {
            InputManager.WipeSavedBindings();
            DebugManager.Log("Saved bindings wiped (now using defaults).", this);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        // --- PlayerPrefs -----------------------------------------------------
        GUILayout.Label("<b>PlayerPrefs</b>", Rich());
        if (GUILayout.Button("Delete ALL PlayerPrefs"))
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            DebugManager.LogWarning("All PlayerPrefs deleted.", this);
        }

        GUILayout.Space(6);

        // --- Scene -----------------------------------------------------------
        GUILayout.Label("<b>Scene</b>", Rich());
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reload Scene"))
        {
            var s = SceneManager.GetActiveScene();
            SceneManager.LoadScene(s.buildIndex);
        }
        if (GUILayout.Button("Close Overlay")) _visible = false;
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        // --- Input Debug -----------------------------------------------------
        GUILayout.Label("<b>Input Debug</b>", Rich());
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Log Bindings")) InputManager.LogBindings();
        if (GUILayout.Button("Force Reload")) InputManager.ForceReload();
        GUILayout.EndHorizontal();

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    // helpers -----------------------------------------------------------------
    private static float ParseOrZero(string s)
    {
        return float.TryParse(s, out var v) ? v : 0f;
    }

    private static void Toggle(GameObject go)
    {
        if (!go) return;
        go.SetActive(!go.activeSelf);
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (!go) return;
        go.SetActive(active);
    }

    private static GUIStyle Rich()
    {
        var st = new GUIStyle(GUI.skin.label) { richText = true };
        return st;
    }
}
#endif
