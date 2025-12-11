
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class NightSpawnBalancer : EditorWindow
{
    private enum CompositionMode { Weights, ThreatBudget }

    [Serializable]
    private class EnemyType
    {
        public string name = "Walker";
        public GameObject prefab;
        [Range(0f, 1f)] public float weight = 0.5f;
        public float cost = 1f;
        public int minPerWave = 0;
        public int maxPerWave = 9999;
        public bool enabled = true;
    }

    [MenuItem("Tools/Balance/Night Spawn Balancer")]
    public static void ShowWindow() => GetWindow<NightSpawnBalancer>("Night Spawn Balancer");

    [Header("Cycle")]
    [SerializeField] private float _dayLength = 105f;
    [SerializeField] private float _nightLength = 150f;
    [SerializeField] private float _cleanupBuffer = 45f;

    [SerializeField] private int _waves = 3;
    [SerializeField]
    private AnimationCurve _waveDistribution = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(0.5f, 1.2f), new Keyframe(1f, 0.8f)
    );

    [SerializeField] private int _totalEnemies = 60;

    [SerializeField] private CompositionMode _mode = CompositionMode.Weights;
    [SerializeField] private float _threatPerEnemy = 1.0f;
    [SerializeField]
    private List<EnemyType> _types = new()
    {
        new EnemyType{ name="Walker", weight=0.55f, cost=1f, enabled=true },
        new EnemyType{ name="Runner", weight=0.30f, cost=1.5f, enabled=true },
        new EnemyType{ name="Brute",  weight=0.15f, cost=3f, enabled=true },
    };

    [SerializeField] private float _minInterval = 0.25f;
    [SerializeField] private float _maxInterval = 8f;

    private float _spawnWindow => Mathf.Max(0f, _nightLength - _cleanupBuffer);
    private float[] _waveEnemyCounts = Array.Empty<float>();
    private float[] _waveStart = Array.Empty<float>();
    private float[] _waveEnd = Array.Empty<float>();

    private Dictionary<int, Dictionary<int, int>> _counts = new();
    private Dictionary<int, Dictionary<int, float>> _intervals = new();

    private Vector2 _scroll;

    private void OnGUI()
    {
        using var scroll = new EditorGUILayout.ScrollViewScope(_scroll);
        _scroll = scroll.scrollPosition;

        DrawCycle();
        EditorGUILayout.Space(6);
        DrawTotals();
        EditorGUILayout.Space(6);
        DrawComposition();
        EditorGUILayout.Space(6);
        DrawConstraints();

        EditorGUILayout.Space(10);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Calculate", GUILayout.Height(26))) Calculate();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Create NightPreset Asset...", GUILayout.Height(26))) ExportNightPreset();
        }

        EditorGUILayout.Space(8);
        DrawResults();
    }

    private void DrawCycle()
    {
        GUILayout.Label("Cycle", EditorStyles.boldLabel);
        _dayLength = EditorGUILayout.Slider("Day Length (s)", _dayLength, 30f, 360f);
        _nightLength = EditorGUILayout.Slider("Night Length (s)", _nightLength, 60f, 600f);
        _cleanupBuffer = EditorGUILayout.Slider("Cleanup Buffer (s)", _cleanupBuffer, 15f, 120f);
        EditorGUILayout.HelpBox($"Spawns must finish by {_spawnWindow:0.##}s after night start; the last {_cleanupBuffer:0.##}s are pure cleanup.", MessageType.Info);
    }

    private void DrawTotals()
    {
        GUILayout.Label("Totals", EditorStyles.boldLabel);
        _waves = Mathf.Clamp(EditorGUILayout.IntField("Wave Count", _waves), 1, 10);
        _waveDistribution = EditorGUILayout.CurveField("Wave Distribution Curve", _waveDistribution);
        _totalEnemies = Mathf.Max(0, EditorGUILayout.IntField("Total Enemies (Night)", _totalEnemies));
    }

    private void DrawComposition()
    {
        GUILayout.Label("Composition", EditorStyles.boldLabel);
        _mode = (CompositionMode)EditorGUILayout.EnumPopup("Mode", _mode);
        if (_mode == CompositionMode.ThreatBudget)
            _threatPerEnemy = Mathf.Max(0.01f, EditorGUILayout.FloatField("Threat / Enemy (budget factor)", _threatPerEnemy));

        EditorGUILayout.Space(3);
        for (int i = 0; i < _types.Count; i++)
        {
            var t = _types[i];
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    t.enabled = EditorGUILayout.Toggle(t.enabled, GUILayout.Width(18));
                    t.name = EditorGUILayout.TextField(t.name);
                    if (GUILayout.Button("X", GUILayout.Width(22))) { _types.RemoveAt(i); i--; continue; }
                }
                t.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", t.prefab, typeof(GameObject), false);
                if (_mode == CompositionMode.Weights)
                    t.weight = EditorGUILayout.Slider("Weight", t.weight, 0f, 1f);
                else
                    t.cost = Mathf.Max(0.01f, EditorGUILayout.FloatField("Cost", t.cost));

                using (new EditorGUILayout.HorizontalScope())
                {
                    t.minPerWave = Mathf.Max(0, EditorGUILayout.IntField("Min / Wave", t.minPerWave));
                    t.maxPerWave = Mathf.Max(1, EditorGUILayout.IntField("Max / Wave", t.maxPerWave));
                }
            }
        }
        if (GUILayout.Button("+ Add Enemy Type")) _types.Add(new EnemyType());
    }

    private void DrawConstraints()
    {
        GUILayout.Label("Spawn Constraints", EditorStyles.boldLabel);
        _minInterval = EditorGUILayout.Slider("Min Interval (s)", _minInterval, 0.05f, 5f);
        _maxInterval = EditorGUILayout.Slider("Max Interval (s)", _maxInterval, 0.2f, 15f);
    }

    private void DrawResults()
    {
        if (_waveEnemyCounts.Length == 0) return;

        GUILayout.Label("Results", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Spawn Window", $"{_spawnWindow:0.##} s");

        for (int w = 0; w < _waves; w++)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"Wave {w + 1}",
                    $"Enemies≈{_waveEnemyCounts[w]:0} | Start={_waveStart[w]:0.##}s | End={_waveEnd[w]:0.##}s | Duration={(_waveEnd[w] - _waveStart[w]):0.##}s");

                if (_counts.TryGetValue(w, out var map) && _intervals.TryGetValue(w, out var imap))
                {
                    foreach (var kv in map)
                    {
                        int tIdx = kv.Key; int c = kv.Value;
                        if (c <= 0) continue;
                        var type = _types[tIdx];
                        float interval = imap[tIdx];
                        EditorGUILayout.LabelField($"  • {type.name}", $"count={c} | interval={interval:0.###} s");
                    }
                }
            }
        }

        if (GUILayout.Button("Copy Summary To Clipboard"))
            EditorGUIUtility.systemCopyBuffer = BuildSummaryText();
    }

    private void Calculate()
    {
        var activeTypes = _types.Select((t, idx) => (t, idx)).Where(p => p.t.enabled).ToList();
        if (activeTypes.Count == 0 || _totalEnemies <= 0 || _spawnWindow <= 0f)
        {
            _waveEnemyCounts = Array.Empty<float>();
            _counts.Clear(); _intervals.Clear();
            return;
        }

        float[] wweights = new float[_waves];
        float step = (_waves == 1) ? 1f : 1f / (_waves - 1);
        float sum = 0f;
        for (int i = 0; i < _waves; i++)
        {
            float x = (_waves == 1) ? 0.5f : i * step;
            float v = Mathf.Max(0.0001f, _waveDistribution.Evaluate(x));
            wweights[i] = v; sum += v;
        }
        for (int i = 0; i < _waves; i++) wweights[i] /= sum;

        _waveEnemyCounts = new float[_waves];
        for (int i = 0; i < _waves; i++) _waveEnemyCounts[i] = _totalEnemies * wweights[i];

        _waveStart = new float[_waves];
        _waveEnd = new float[_waves];
        float cursor = 0f;
        for (int i = 0; i < _waves; i++)
        {
            float Ti = _spawnWindow * wweights[i];
            _waveStart[i] = cursor;
            _waveEnd[i] = cursor + Ti;
            cursor += Ti;
        }

        _counts.Clear(); _intervals.Clear();

        for (int w = 0; w < _waves; w++)
        {
            int Ei = Mathf.Max(0, Mathf.RoundToInt(_waveEnemyCounts[w]));
            float Ti = Mathf.Max(0.01f, _waveEnd[w] - _waveStart[w]);

            var counts = new Dictionary<int, int>();
            if (Ei == 0) { _counts[w] = counts; _intervals[w] = new Dictionary<int, float>(); continue; }

            if (_mode == CompositionMode.Weights)
            {

                float sumWeights = activeTypes.Sum(p => Mathf.Max(0f, p.t.weight));

                if (sumWeights <= 0f) sumWeights = activeTypes.Count;
                var provisional = activeTypes.ToDictionary(p => p.idx, p => (float)Ei * (Mathf.Max(0f, p.t.weight) / sumWeights));
                counts = LargestRemainderRound(provisional, Ei, activeTypes);
                EnforceMinMax(counts, Ei, activeTypes);
            }
            else
            {
                float budget = Mathf.Max(MinBudget(Ei, activeTypes), Ei * _threatPerEnemy);
                counts = ComposeByBudgetGreedy(Ei, budget, activeTypes);
                EnforceMinMax(counts, Ei, activeTypes);

                int totalNow = counts.Values.Sum();
                if (totalNow != Ei)
                {

                    var adjWeights = activeTypes.ToDictionary(p => p.idx, p => 1f / Mathf.Max(0.001f, p.t.cost));
                    counts = RebalanceToTotal(counts, Ei, adjWeights, activeTypes);
                }
            }

            _counts[w] = counts;

            var imap = new Dictionary<int, float>();
            foreach (var kv in counts)
            {
                int c = Mathf.Max(0, kv.Value);
                if (c <= 0) { imap[kv.Key] = 0f; continue; }
                float interval = Mathf.Clamp(Ti / c, _minInterval, _maxInterval);
                imap[kv.Key] = interval;
            }
            _intervals[w] = imap;
        }
    }

    private static Dictionary<int, int> LargestRemainderRound(
        Dictionary<int, float> fractional, int targetTotal,
        List<(EnemyType t, int idx)> activeTypes)
    {
        var floor = fractional.ToDictionary(kv => kv.Key, kv => Mathf.FloorToInt(kv.Value));
        int remainder = targetTotal - floor.Values.Sum();
        var order = fractional
            .OrderByDescending(kv => kv.Value - Mathf.Floor(kv.Value))
            .Select(kv => kv.Key)
            .ToList();
        for (int i = 0; i < remainder && i < order.Count; i++) floor[order[i]]++;

        var allowed = new HashSet<int>(activeTypes.Select(p => p.idx));
        return floor.Where(kv => allowed.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private static void EnforceMinMax(Dictionary<int, int> counts, int Ei, List<(EnemyType t, int idx)> activeTypes)
    {

        foreach (var p in activeTypes)
        {
            if (!counts.ContainsKey(p.idx)) counts[p.idx] = 0;
            counts[p.idx] = Mathf.Clamp(counts[p.idx], p.t.minPerWave, p.t.maxPerWave);
        }
        int delta = Ei - counts.Values.Sum();
        if (delta == 0) return;

        var ordered = (delta > 0)
            ? activeTypes.OrderBy(p => p.t.cost).Select(p => p.idx).ToList()
            : activeTypes.OrderByDescending(p => p.t.cost).Select(p => p.idx).ToList();

        int i = 0;
        while (delta != 0 && i < 100000)
        {
            foreach (var idx in ordered)
            {
                var t = activeTypes.First(p => p.idx == idx).t;
                if (delta > 0)
                {
                    if (counts[idx] < t.maxPerWave) { counts[idx]++; delta--; if (delta == 0) break; }
                }
                else
                {
                    if (counts[idx] > t.minPerWave && counts[idx] > 0) { counts[idx]--; delta++; if (delta == 0) break; }
                }
            }
            i++;
        }
    }

    private static float MinBudget(int Ei, List<(EnemyType t, int idx)> active)
    {
        float minCost = active.Min(p => p.t.cost);
        return Ei * minCost;
    }

    private static Dictionary<int, int> ComposeByBudgetGreedy(int Ei, float budget, List<(EnemyType t, int idx)> active)
    {

        var counts = active.ToDictionary(p => p.idx, p => 0);
        float remaining = Mathf.Max(MinBudget(Ei, active), budget);

        for (int k = 0; k < Ei; k++)
        {

            float ideal = remaining / Mathf.Max(1, (Ei - k));

            var pick = active
                .OrderBy(p => Mathf.Abs(p.t.cost - ideal))
                .ThenByDescending(p => p.t.cost)
                .First();

            counts[pick.idx] += 1;
            remaining -= pick.t.cost;
        }
        return counts;
    }

    private static Dictionary<int, int> RebalanceToTotal(
        Dictionary<int, int> counts, int targetTotal,
        Dictionary<int, float> prefWeights, List<(EnemyType t, int idx)> active)
    {

        foreach (var p in active) if (!counts.ContainsKey(p.idx)) counts[p.idx] = 0;

        int delta = targetTotal - counts.Values.Sum();
        if (delta == 0) return counts;

        float sumW = prefWeights.Values.Sum();
        if (sumW <= 0f) sumW = prefWeights.Count;
        var order = prefWeights.OrderByDescending(kv => kv.Value / sumW).Select(kv => kv.Key).ToList();

        int i = 0;
        while (delta != 0 && i < 100000)
        {
            foreach (var idx in order)
            {
                var t = active.First(p => p.idx == idx).t;
                if (delta > 0)
                {
                    if (counts[idx] < t.maxPerWave) { counts[idx]++; delta--; if (delta == 0) break; }
                }
                else
                {
                    if (counts[idx] > t.minPerWave && counts[idx] > 0) { counts[idx]--; delta++; if (delta == 0) break; }
                }
            }
            i++;
        }
        return counts;
    }

    private void ExportNightPreset()
    {
        Calculate();

        if (_waveEnemyCounts.Length == 0)
        {
            EditorUtility.DisplayDialog("NightPreset", "Nothing to export. Calculate first.", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Create NightPreset", "Night_Preset", "asset", "Choose save location for the NightPreset asset.");

        if (string.IsNullOrEmpty(path)) return;

        var preset = ScriptableObject.CreateInstance<NightPreset>();

        var waves = new List<NightPreset.Wave>();
        for (int w = 0; w < _waves; w++)
        {
            var wave = new NightPreset.Wave
            {
                name = $"Wave {w + 1}",
                startDelay = 0f,
                endDelay = 0f,
                maxAliveOverride = 0,
                entries = new List<NightPreset.EnemyEntry>()
            };

            if (_counts.TryGetValue(w, out var cmap) && _intervals.TryGetValue(w, out var imap))
            {
                foreach (var kv in cmap)
                {
                    int typeIdx = kv.Key;
                    int count = kv.Value;
                    if (count <= 0) continue;

                    var type = _types[typeIdx];
                    var entry = new NightPreset.EnemyEntry
                    {
                        prefab = type.prefab,
                        count = count,
                        interval = Mathf.Clamp(imap[typeIdx], _minInterval, _maxInterval)
                    };
                    wave.entries.Add(entry);
                }
            }

            waves.Add(wave);
        }

        var wavesField = typeof(NightPreset).GetField("_waves", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        wavesField?.SetValue(preset, waves);

        AssetDatabase.CreateAsset(preset, path);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(preset);

        EditorUtility.DisplayDialog("NightPreset", $"Created NightPreset with {_waves} waves at:\n{path}", "Nice");
    }

    private string BuildSummaryText()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Day: {_dayLength:0.##}s | Night: {_nightLength:0.##}s | Buffer: {_cleanupBuffer:0.##}s | SpawnWindow: {_spawnWindow:0.##}s");
        sb.AppendLine($"Total Enemies: {_totalEnemies} | Waves: {_waves} | Mode: {_mode}");
        sb.AppendLine();

        for (int w = 0; w < _waves; w++)
        {
            float Ti = (_waveEnd[w] - _waveStart[w]);
            sb.AppendLine($"Wave {w + 1}: Enemies≈{_waveEnemyCounts[w]:0} | Duration={Ti:0.##}s");
            if (_counts.TryGetValue(w, out var cmap) && _intervals.TryGetValue(w, out var imap))
            {
                foreach (var kv in cmap)
                {
                    var type = _types[kv.Key];
                    int c = kv.Value;
                    if (c <= 0) continue;
                    float interval = imap[kv.Key];
                    sb.AppendLine($"  - {type.name}: count={c}, interval={interval:0.###}s");
                }
            }
        }
        return sb.ToString();
    }
}
