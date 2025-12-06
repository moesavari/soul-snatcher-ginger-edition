// Code/Editor/Tools/UnusedScriptFinder.cs
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

public class UnusedScriptFinder : EditorWindow
{
    private const string RootFolder = "Assets/Code";

    private Vector2 _scroll;
    private bool _searched;
    private bool _includeEditorScripts; // optional toggle
    private List<string> _unusedScriptPaths = new();
    private HashSet<int> _selected = new();

    [MenuItem("Tools/Find Unused MonoBehaviour Scripts")]
    public static void ShowWindow()
    {
        GetWindow<UnusedScriptFinder>("Unused Script Finder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Scan Settings", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Scope", RootFolder);
            _includeEditorScripts = EditorGUILayout.ToggleLeft("Include scripts in /Editor", _includeEditorScripts);
            if (GUILayout.Button("Find Unused MonoBehaviour Scripts", GUILayout.Height(28)))
            {
                _unusedScriptPaths = FindUnusedMonoBehaviours();
                _selected.Clear();
                _searched = true;
            }
        }

        if (!_searched) return;

        EditorGUILayout.Space(6);
        GUILayout.Label($"Results ({_unusedScriptPaths.Count})", EditorStyles.boldLabel);

        using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
        {
            _scroll = scroll.scrollPosition;

            for (int i = 0; i < _unusedScriptPaths.Count; i++)
            {
                string path = _unusedScriptPaths[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool sel = _selected.Contains(i);
                    bool newSel = GUILayout.Toggle(sel, GUIContent.none, GUILayout.Width(18));
                    if (newSel != sel)
                    {
                        if (newSel) _selected.Add(i); else _selected.Remove(i);
                    }

                    EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath<MonoScript>(path), typeof(MonoScript), false);

                    if (GUILayout.Button("Ping", GUILayout.Width(60)))
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<MonoScript>(path));

                    if (GUILayout.Button("Open", GUILayout.Width(60)))
                        AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(path));

                    if (GUILayout.Button("Copy Path", GUILayout.Width(90)))
                        EditorGUIUtility.systemCopyBuffer = path;
                }
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            GUI.enabled = _selected.Count > 0;
            if (GUILayout.Button($"Delete Selected ({_selected.Count})", GUILayout.Width(180)))
                DeleteSelected();
            GUI.enabled = true;
        }
    }

    private void DeleteSelected()
    {
        if (_selected.Count == 0) return;

        var toDelete = _selected.Select(idx => _unusedScriptPaths[idx]).Distinct().ToList();
        if (!EditorUtility.DisplayDialog(
            "Delete Scripts",
            $"Are you sure you want to delete {toDelete.Count} script file(s)? This cannot be undone.",
            "Delete", "Cancel"))
        {
            return;
        }

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var p in toDelete)
                AssetDatabase.DeleteAsset(p);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // refresh results view
        _unusedScriptPaths = FindUnusedMonoBehaviours();
        _selected.Clear();
    }

    private List<string> FindUnusedMonoBehaviours()
    {
        // 1) Collect all MonoScripts under Assets/Code (optionally excluding /Editor)
        var allScriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { RootFolder });
        var allScriptPaths = allScriptGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => _includeEditorScripts || !IsEditorPath(p))
            .ToList();

        // Build map: scriptPath -> (guid, class)
        var candidates = new List<(string path, string guid, System.Type klass)>();
        foreach (var path in allScriptPaths)
        {
            var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (!ms) continue;
            var klass = ms.GetClass();
            if (klass == null) continue;                          // no compiled type yet
            if (!typeof(MonoBehaviour).IsAssignableFrom(klass)) continue; // only MonoBehaviours can sit on prefabs/scenes
            if (klass.IsAbstract) continue;                       // abstract cannot be attached
            if (IsEditorType(klass) && !_includeEditorScripts) continue;

            var guid = AssetDatabase.AssetPathToGUID(path);
            candidates.Add((path, guid, klass));
        }

        // Early out: if no candidates, nothing to do
        if (candidates.Count == 0) return new List<string>();

        // 2) Gather all prefab + scene assets in project and compute dependencies (by GUID) once
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        var sceneGuids = AssetDatabase.FindAssets("t:Scene");
        var assetPaths = prefabGuids.Concat(sceneGuids).Select(AssetDatabase.GUIDToAssetPath).ToList();

        // Build a set of script GUIDs that are referenced by any prefab/scene
        var referencedGuids = new HashSet<string>();
        for (int i = 0; i < assetPaths.Count; i++)
        {
            string aPath = assetPaths[i];
            // robust: use dependencies so it works with binary or text serialization
            var deps = AssetDatabase.GetDependencies(aPath, true);
            foreach (var d in deps)
            {
                if (d.EndsWith(".cs"))
                {
                    var g = AssetDatabase.AssetPathToGUID(d);
                    if (!string.IsNullOrEmpty(g))
                        referencedGuids.Add(g);
                }
            }
        }

        // 3) Anything not referenced by any prefab/scene is considered "unused" per your rule
        var unused = candidates
            .Where(c => !referencedGuids.Contains(c.guid))
            .Select(c => c.path)
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        return unused;
    }

    private static bool IsEditorPath(string path)
        => path.Contains("/Editor/") || path.EndsWith("/Editor") || path.Contains("\\Editor\\");

    private static bool IsEditorType(System.Type t)
    {
        // quick heuristic: EditorWindow & UnityEditor.* derive from ScriptableObject; we only gathered MonoBehaviours,
        // but someone might put an editor MonoBehaviour under Editor. This check keeps things tidy.
        return t.Namespace != null && t.Namespace.StartsWith("UnityEditor", System.StringComparison.Ordinal);
    }
}
