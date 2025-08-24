﻿using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(AudioCue))]
public class AudioCueEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        InspectorElement.FillDefaultInspector(root, serializedObject, this);

        // spacer above controls
        var spacer = new VisualElement();
        spacer.style.marginTop = 6;
        root.Add(spacer);

        // a row container (no .gap — use margins on children instead)
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        root.Add(row);

        var playBtn = new Button(() =>
        {
            var cue = (AudioCue)target;
            if (cue == null) return;

            if (Application.isPlaying && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCue(cue);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Play Preview",
                    "Enter Play Mode to preview this cue through AudioManager (mixer routing & ducking).",
                    "OK"
                );
            }
        })
        { text = "▶ Test Cue (Play Mode)" };

        // add the button with right margin to simulate gap
        playBtn.style.marginRight = 6;
        row.Add(playBtn);

        // Hint when not in play mode
        if (!Application.isPlaying)
        {
            var help = new HelpBox(
                "Enter Play Mode to preview this cue via AudioManager. Unity 6000 no longer exposes internal AudioUtil for editor-time preview.",
                HelpBoxMessageType.Info
            );
            help.style.marginTop = 6;
            root.Add(help);
        }

        return root;
    }
}
