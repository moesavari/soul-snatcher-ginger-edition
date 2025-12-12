#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NightPresetBatchCreator : EditorWindow
{
    [Serializable]
    private class EnemyConfig
    {
        public string name = "Walker";
        public GameObject prefab;
        public int minCount = 2;
        public int maxCount = 6;
        public float minInterval = 0.75f;
        public float maxInterval = 1.5f;
    }

    [Header("Output")]
    [SerializeField] private DefaultAsset _outputFolder;
    [SerializeField] private string _baseName = "Night";
    [SerializeField] private int _nightCount = 6;
    [SerializeField] private int _wavesPerNight = 3;

    [Header("Difficulty Scaling")]
    [SerializeField]
    private AnimationCurve _nightMultiplierCurve =
        AnimationCurve.Linear(0f, 1f, 5f, 2f); // nightIndex 0..5
    [SerializeField]
    private AnimationCurve _waveMultiplierCurve =
        AnimationCurve.Linear(0f, 1f, 2f, 1.5f); // waveIndex 0..2

    [Header("Enemy Types")]
    [SerializeField] private List<EnemyConfig> _enemyTypes = new List<EnemyConfig>();

    [MenuItem("Tools/SoulSnatched/Nights/Night Preset Generator")]
    public static void Open() => GetWindow<NightPresetBatchCreator>("Night Preset Generator");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        _outputFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Folder",
            _outputFolder,
            typeof(DefaultAsset),
            false);

        _baseName = EditorGUILayout.TextField("Base Name", _baseName);
        _nightCount = Mathf.Max(1, EditorGUILayout.IntField("Number of Nights", _nightCount));
        _wavesPerNight = Mathf.Clamp(EditorGUILayout.IntField("Waves per Night", _wavesPerNight), 1, 10);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Difficulty Scaling", EditorStyles.boldLabel);
        _nightMultiplierCurve = EditorGUILayout.CurveField("Night Multiplier", _nightMultiplierCurve);
        _waveMultiplierCurve = EditorGUILayout.CurveField("Wave Multiplier", _waveMultiplierCurve);

        EditorGUILayout.Space(8);
        DrawEnemyTypesSection();

        EditorGUILayout.Space(8);
        using (new EditorGUI.DisabledScope(!CanGenerate()))
        {
            if (GUILayout.Button($"Generate {_nightCount} Night Presets"))
            {
                GeneratePresets();
            }
        }
    }

    private void DrawEnemyTypesSection()
    {
        EditorGUILayout.LabelField("Enemy Types", EditorStyles.boldLabel);

        if (_enemyTypes == null) _enemyTypes = new List<EnemyConfig>();

        int removeIndex = -1;

        for (int i = 0; i < _enemyTypes.Count; i++)
        {
            var cfg = _enemyTypes[i];
            using (new EditorGUILayout.VerticalScope("box"))
            {
                cfg.name = EditorGUILayout.TextField("Name", cfg.name);
                cfg.prefab = (GameObject)EditorGUILayout.ObjectField(
                    "Prefab",
                    cfg.prefab,
                    typeof(GameObject),
                    false);

                EditorGUILayout.BeginHorizontal();
                cfg.minCount = Mathf.Max(0, EditorGUILayout.IntField("Min Count", cfg.minCount));
                cfg.maxCount = Mathf.Max(cfg.minCount, EditorGUILayout.IntField("Max Count", cfg.maxCount));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                cfg.minInterval = Mathf.Max(0.05f, EditorGUILayout.FloatField("Min Interval", cfg.minInterval));
                cfg.maxInterval = Mathf.Max(cfg.minInterval, EditorGUILayout.FloatField("Max Interval", cfg.maxInterval));
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Remove Type"))
                {
                    removeIndex = i;
                }
            }
        }

        if (removeIndex >= 0)
        {
            _enemyTypes.RemoveAt(removeIndex);
        }

        if (GUILayout.Button("Add Enemy Type"))
        {
            _enemyTypes.Add(new EnemyConfig());
        }
    }

    private bool CanGenerate()
    {
        if (_outputFolder == null) return false;
        if (_enemyTypes == null || _enemyTypes.Count == 0) return false;
        foreach (var cfg in _enemyTypes)
        {
            if (cfg.prefab != null) return true;
        }
        return false;
    }

    private void GeneratePresets()
    {
        string folderPath = AssetDatabase.GetAssetPath(_outputFolder);
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog(
                "Invalid Folder",
                "Please assign a valid folder asset in the project.",
                "OK");
            return;
        }

        int created = 0;

        for (int nightIndex = 0; nightIndex < _nightCount; nightIndex++)
        {
            float nightMultiplier = _nightMultiplierCurve.Evaluate(nightIndex);

            var preset = ScriptableObject.CreateInstance<NightPreset>();
            var so = new SerializedObject(preset);

            so.FindProperty("_nightName").stringValue = $"{_baseName} {nightIndex + 1:00}";

            SerializedProperty wavesProp = so.FindProperty("_waves");
            wavesProp.ClearArray();

            for (int waveIndex = 0; waveIndex < _wavesPerNight; waveIndex++)
            {
                float waveMultiplier = _waveMultiplierCurve.Evaluate(waveIndex);
                float difficulty = Mathf.Max(0.1f, nightMultiplier * waveMultiplier);

                int waveArrayIndex = wavesProp.arraySize;
                wavesProp.InsertArrayElementAtIndex(waveArrayIndex);
                var waveProp = wavesProp.GetArrayElementAtIndex(waveArrayIndex);

                waveProp.FindPropertyRelative("name").stringValue = $"Wave {waveIndex + 1}";
                waveProp.FindPropertyRelative("startDelay").floatValue = 2f + waveIndex;
                waveProp.FindPropertyRelative("endDelay").floatValue = 3f;
                waveProp.FindPropertyRelative("maxAliveOverride").intValue = 0;

                SerializedProperty entriesProp = waveProp.FindPropertyRelative("entries");
                entriesProp.ClearArray();

                for (int typeIndex = 0; typeIndex < _enemyTypes.Count; typeIndex++)
                {
                    var cfg = _enemyTypes[typeIndex];
                    if (cfg.prefab == null) continue;

                    int baseCount = UnityEngine.Random.Range(cfg.minCount, cfg.maxCount + 1);
                    int finalCount = Mathf.RoundToInt(baseCount * difficulty);
                    if (finalCount <= 0) continue;

                    float baseInterval = UnityEngine.Random.Range(cfg.minInterval, cfg.maxInterval);
                    float finalInterval = Mathf.Max(0.05f, baseInterval / Mathf.Max(0.25f, difficulty));

                    int entryIndex = entriesProp.arraySize;
                    entriesProp.InsertArrayElementAtIndex(entryIndex);
                    var entryProp = entriesProp.GetArrayElementAtIndex(entryIndex);

                    entryProp.FindPropertyRelative("prefab").objectReferenceValue = cfg.prefab;
                    entryProp.FindPropertyRelative("count").intValue = finalCount;
                    entryProp.FindPropertyRelative("interval").floatValue = finalInterval;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{folderPath}/{_baseName}_{nightIndex + 1:00}.asset");

            AssetDatabase.CreateAsset(preset, assetPath);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        DebugManager.Log($"NightPresetBatchCreator: Created {created} NightPreset assets in {AssetDatabase.GetAssetPath(_outputFolder)}.");
        EditorUtility.DisplayDialog("Night Presets Generated",
            $"Created {created} NightPreset assets.",
            "OK");
    }
}
#endif
