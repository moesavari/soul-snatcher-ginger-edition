using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(9999)]
public class UIRaycastProbe : MonoBehaviour
{
    [Tooltip("If null, grabs the first active EventSystem in the scene")]
    public EventSystem eventSystem;

    [Tooltip("If null, searches all canvases and uses their GraphicRaycasters")]
    public List<GraphicRaycaster> raycasters = new();

    [Header("Display")]
    public bool drawOnScreen = true;
    public int maxResultsShown = 6;

    readonly List<RaycastResult> _results = new();
    string _headline = "";
    string[] _lines = System.Array.Empty<string>();
    GameObject _lastTop;

    void Awake()
    {
        if (!eventSystem) eventSystem = EventSystem.current;

        if (raycasters.Count == 0)
        {
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var gr = canvas.GetComponent<GraphicRaycaster>();
                if (gr) raycasters.Add(gr);
            }
        }
    }

    void Update()
    {
        if (!eventSystem || raycasters.Count == 0) return;

        _results.Clear();
        var ped = new PointerEventData(eventSystem) { position = Input.mousePosition };

        // Query all canvases and merge results (Unity sorts per raycaster, so we’ll re-sort globally)
        foreach (var gr in raycasters)
        {
            if (!gr || !gr.isActiveAndEnabled) continue;
            var tmp = new List<RaycastResult>();
            gr.Raycast(ped, tmp);
            _results.AddRange(tmp);
        }

        _results.Sort((a, b) => b.sortingOrder.CompareTo(a.sortingOrder)); // top first-ish

        // Build display
        if (_results.Count == 0)
        {
            _headline = "UI: (nothing hit)";
            _lines = System.Array.Empty<string>();
            _lastTop = null;
            return;
        }

        _headline = $"UI top: {_results[0].gameObject.name}  (module: {_results[0].module?.GetType().Name})";
        int n = Mathf.Min(maxResultsShown, _results.Count);
        var list = new List<string>(n);
        for (int i = 0; i < n; i++)
        {
            var r = _results[i];
            var rt = r.gameObject.GetComponent<RectTransform>();
            var img = r.gameObject.GetComponent<Image>();
            var rtOn = img ? img.raycastTarget : (r.gameObject.GetComponent<MaskableGraphic>()?.raycastTarget ?? false);
            list.Add($"{i + 1}. {r.gameObject.name}  [raycastTarget={(rtOn ? "ON" : "off")}  layer={r.gameObject.layer}]");
        }
        _lines = list.ToArray();

        // Log when the topmost changes → usually reveals a blocking panel/image
        if (_results[0].gameObject != _lastTop)
        {
            _lastTop = _results[0].gameObject;
            DebugManager.Log($"[UIRaycastProbe] Top UI under mouse: {_lastTop.name} (RaycastTarget={(_lastTop.GetComponent<MaskableGraphic>()?.raycastTarget ?? false)})");
        }
    }

    void OnGUI()
    {
        if (!drawOnScreen) return;

        const int pad = 8;
        int w = 540;
        int h = 26 + _lines.Length * 18 + pad * 2;

        GUI.color = new Color(0, 0, 0, 0.65f);
        GUI.Box(new Rect(10, 10, w, h), GUIContent.none);
        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(10 + pad, 10 + pad, w - pad * 2, h - pad * 2));
        GUILayout.Label(_headline);
        foreach (var l in _lines) GUILayout.Label(l);
        GUILayout.EndArea();
    }
}
