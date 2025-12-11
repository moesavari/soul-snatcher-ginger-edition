using UnityEditor;
using UnityEngine;

public static class MissingScriptsScanner
{
    [MenuItem("Tools/Diagnostics/Find Missing Scripts In Scene")]
    private static void FindMissingInScene()
    {
        var all = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        int count = 0;
        foreach (var go in all)
        {
            var comps = go.GetComponents<Component>();
            foreach (var c in comps)
            {
                if (c == null)
                {
                    DebugManager.LogWarning($"[MissingScripts] {GetFullPath(go)}", go);
                    count++;
                }
            }
        }

        DebugManager.Log($"[MissingScripts] Scan complete. Missing components found: {count}");
    }

    [MenuItem("Tools/Diagnostics/Remove Missing Scripts In Selection")]
    private static void RemoveMissingInSelection()
    {
        int removed = 0;
        foreach (var obj in Selection.gameObjects)
        {
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
            DebugManager.Log($"Removed on {GetFullPath(obj)}", obj);
        }
        DebugManager.Log($"Total removed from selection: {removed}");
    }

    private static string GetFullPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}
