using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Audio;
using UnityEngine.UIElements;

public class AudioSetupValidator : EditorWindow
{
    private AudioMixer _mixer;
    private string[] _requiredParams = { "MasterVol", "MusicVol", "SFXVol", "VoiceVol", "AmbientVol", "UIVol", "FootstepsVol" };

    [MenuItem("Tools/Audio/Setup & Validate")]
    public static void Open() => GetWindow<AudioSetupValidator>("Audio Setup");

    private void CreateGUI()
    {
        var mixerField = new ObjectField("AudioMixer") { objectType = typeof(AudioMixer), allowSceneObjects = false };
        mixerField.RegisterValueChangedCallback(e => _mixer = e.newValue as AudioMixer);
        rootVisualElement.Add(mixerField);

        var btn = new Button(Validate) { text = "Validate Exposed Params" };
        rootVisualElement.Add(btn);
    }

    private void Validate()
    {
        if (_mixer == null)
        {
            EditorUtility.DisplayDialog("No Mixer", "Assign an AudioMixer first.", "OK");
            return;
        }

        string report = "";
        foreach (var p in _requiredParams)
        {
            bool ok = _mixer.GetFloat(p, out _);
            report += ok ? $"✔ {p}\n" : $"✖ Missing exposed param: {p}\n";
        }
        EditorUtility.DisplayDialog("Mixer Validation", report, "OK");
    }
}
