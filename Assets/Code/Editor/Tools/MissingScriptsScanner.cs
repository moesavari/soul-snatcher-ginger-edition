using UnityEditor;
using UnityEngine;

public static class MissingScriptsScanner
{
    [MenuItem("Tools/Diagnostics/Find Missing Scripts In Scene")]
    private static void FindMissingInScene()
    {
        int count = 0;
        foreach (var go in Object.FindObjectsOfType<GameObject>(true))
        {
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null)
                {
                    Debug.LogWarning($"{GetFullPath(go)}", go);
                    count++;
                }
            }
        }
        Debug.Log($"Scene scan complete. Found {count} missing scripts.");
    }

    [MenuItem("Tools/Diagnostics/Remove Missing Scripts In Selection")]
    private static void RemoveMissingInSelection()
    {
        int removed = 0;
        foreach (var obj in Selection.gameObjects)
        {
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
            Debug.Log($"Removed on {GetFullPath(obj)}", obj);
        }
        Debug.Log($"Total removed from selection: {removed}");
    }

    private static string GetFullPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}
