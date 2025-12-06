using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SoulSnatched/Night Preset", fileName = "Night_Preset")]
public class NightPreset : ScriptableObject
{
    [System.Serializable]
    public class EnemyEntry
    {
        public GameObject prefab;      // zombie prefab to spawn
        public int count = 1;          // how many to spawn in this wave
        public float interval = 1.0f;  // seconds between spawns for this entry stream
    }

    [System.Serializable]
    public class Wave
    {
        public string name = "Wave";
        public float startDelay = 2.0f;   // pause before this wave starts
        public float endDelay = 3.0f;     // pause after dispatch finishes, before cleanup wait
        public int maxAliveOverride = 0;
        public List<EnemyEntry> entries = new List<EnemyEntry>();
    }

    [SerializeField] private string _nightName = "Night 01";
    [SerializeField] private List<Wave> _waves = new List<Wave>();

    // Optional public mirrors (follow your convention if you need access outside)
    public string nightName => _nightName;
    public List<Wave> waves => _waves;
}
