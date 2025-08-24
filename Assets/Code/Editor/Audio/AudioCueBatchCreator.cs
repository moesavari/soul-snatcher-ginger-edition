using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

public class AudioCueBatchCreator : EditorWindow
{
    private Object[] _clips;
    private AudioChannel _channel = AudioChannel.SFX;
    private string _folder = "Assets/Audio/Cues";

    [MenuItem("Tools/Audio/Batch Create Cues")]
    public static void Open() => GetWindow<AudioCueBatchCreator>("Batch Create Cues");

    private void CreateGUI()
    {
        var root = rootVisualElement;

        var objField = new ObjectField("Clips (multi-select)")
        {
            allowSceneObjects = false,
            objectType = typeof(AudioClip)
        };
        objField.RegisterValueChangedCallback(_ =>
        {
            // not used; we’ll rely on selection below
        });
        root.Add(new Label("Tip: select multiple AudioClips in Project and click 'Use Selection'."));
        root.Add(objField);

        var useSel = new Button(() =>
        {
            _clips = Selection.objects.Where(o => o is AudioClip).ToArray();
            EditorUtility.DisplayDialog("Clips", $"Selected {_clips.Length} clip(s).", "OK");
        })
        { text = "Use Selection" };
        root.Add(useSel);

        var chan = new EnumField("Channel", _channel);
        chan.RegisterValueChangedCallback(e => _channel = (AudioChannel)e.newValue);
        root.Add(chan);

        var folder = new TextField("Output Folder") { value = _folder };
        folder.RegisterValueChangedCallback(e => _folder = e.newValue);
        root.Add(folder);

        var make = new Button(CreateCues) { text = "Create Cues" };
        root.Add(make);
    }

    private void CreateCues()
    {
        if (_clips == null || _clips.Length == 0)
        {
            EditorUtility.DisplayDialog("No Clips", "Select AudioClips first.", "OK");
            return;
        }
        if (!AssetDatabase.IsValidFolder(_folder))
            AssetDatabase.CreateFolder("Assets", "Audio/Cues");

        foreach (var o in _clips)
        {
            var clip = o as AudioClip;
            if (clip == null) continue;

            var cue = ScriptableObject.CreateInstance<AudioCue>();
            var path = $"{_folder}/{clip.name}_Cue.asset";
            AssetDatabase.CreateAsset(cue, path);

            var so = new SerializedObject(cue);
            so.FindProperty("_clips").arraySize = 1;
            so.FindProperty("_clips").GetArrayElementAtIndex(0).objectReferenceValue = clip;
            so.FindProperty("_channel").enumValueIndex = (int)_channel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done", "Created cues.", "OK");
    }
}
