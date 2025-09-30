#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Equipment))]
public class EquipmentDebugEditor : Editor
{
    private bool _showDebug;

    public override void OnInspectorGUI()
    {
        // Draw the normal Equipment inspector first.
        base.OnInspectorGUI();

        EditorGUILayout.Space(8);
        _showDebug = EditorGUILayout.Foldout(_showDebug, "⚙ Equipment Debug (Play Mode)", true);
        if (!_showDebug) return;

        var eq = (Equipment)target;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to inspect live equipped items.", MessageType.Info);
            return;
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            var data = eq.SnapshotEquipped();
            for (int i = 0; i < data.Count; i++)
            {
                var e = data[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(e.slot.ToString(), GUILayout.Width(100));
                    EditorGUILayout.ObjectField(e.item, typeof(ItemDef), false);
                    GUILayout.FlexibleSpace();
                    var tex = e.icon ? e.icon.texture : null;
                    var rect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32), GUILayout.Height(32));
                    if (tex) GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
                    else EditorGUI.HelpBox(rect, "—", MessageType.None);
                }
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Dump To Console"))
            {
                for (int i = 0; i < data.Count; i++)
                {
                    var e = data[i];
                    Debug.Log($"[Equipment] {e.slot}: {(e.item ? e.item.name : "(empty)")}");
                }
            }
        }
    }
}
#endif
