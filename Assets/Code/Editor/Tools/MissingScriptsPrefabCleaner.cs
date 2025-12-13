using UnityEditor;
using UnityEngine;

public static class MissingScriptsPrefabCleaner
{
    [MenuItem("Tools/SoulSnatched/Diagnostics/Remove Missing Scripts In All Prefabs")]
    private static void CleanAllPrefabs()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab");
        int totalRemoved = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefab);
            if (removed > 0)
            {
                totalRemoved += removed;
                DebugManager.Log($"Removed {removed} on prefab: {path}", prefab);
                EditorUtility.SetDirty(prefab);
            }
        }
        if (totalRemoved > 0) AssetDatabase.SaveAssets();
        DebugManager.Log($"Done. Total removed: {totalRemoved}", null);
    }
}
