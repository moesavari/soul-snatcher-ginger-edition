using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NightPresetSet", menuName = "SoulSnatched/Nights/Night Preset Set")]
public class NightPresetSet : ScriptableObject
{
    [Serializable]
    public class NightBucket
    {
        [Tooltip("Optional label for this night (e.g. Night 1, Early Game, etc.)")]
        public string name;

        [Tooltip("Possible presets for this night. One is chosen at random each run.")]
        public List<NightPreset> presets = new List<NightPreset>();
    }

    [SerializeField] private List<NightBucket> _nights = new List<NightBucket>();

    public int nightCount => _nights != null ? _nights.Count : 0;

    public NightPreset GetRandomPresetForNight(int nightIndex)
    {
        if (_nights == null || _nights.Count == 0)
        {
            DebugManager.LogWarning("NightPresetSet has no nights defined.", this);
            return null;
        }

        nightIndex = Mathf.Clamp(nightIndex, 0, _nights.Count - 1);
        var bucket = _nights[nightIndex];

        if (bucket == null || bucket.presets == null || bucket.presets.Count == 0)
        {
            DebugManager.LogWarning($"NightPresetSet: Night bucket {nightIndex} has no presets.", this);
            return null;
        }

        int pick = UnityEngine.Random.Range(0, bucket.presets.Count);
        var preset = bucket.presets[pick];
        if (preset == null)
        {
            DebugManager.LogWarning($"NightPresetSet: Null preset in bucket {nightIndex}.", this);
        }

        return preset;
    }
}
