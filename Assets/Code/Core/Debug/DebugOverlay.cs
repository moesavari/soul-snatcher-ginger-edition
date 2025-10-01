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
    [SerializeField] private float width = 260f;

    private bool _visible;
    private Rect _rect;

    private void Awake()
    {
        _visible = startVisible;
        _rect = new Rect(10, 10, width, 10);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey)) _visible = !_visible;
    }

    private void OnGUI()
    {
        if (!_visible) return;

        // window height grows with content; passing 0 lets GUILayout calculate it
        _rect = GUILayout.Window(GetInstanceID(), _rect, DrawWindow, "Debug Overlay");
    }

    private void DrawWindow(int id)
    {
        GUILayout.Space(4);

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
            DebugManager.Log("Saved bindings wiped (now using defaults.", this);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        GUILayout.Label("<b>PlayerPrefs</b>", Rich());
        if (GUILayout.Button("Delete ALL PlayerPrefs"))
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            DebugManager.LogWarning("All PlayerPrefs deleted.", this);
        }

        GUILayout.Space(6);
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
        GUILayout.Label("<b>Input Debug</b>", Rich());
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Log Bindings")) InputManager.LogBindings();
        if (GUILayout.Button("Force Reload")) InputManager.ForceReload();
        GUILayout.EndHorizontal();

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
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
