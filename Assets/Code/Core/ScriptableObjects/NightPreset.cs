using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SoulSnatched/Night Preset", fileName = "Night_Preset")]
public class NightPreset : ScriptableObject
{
    [System.Serializable]
    public class EnemyEntry
    {
        public GameObject prefab;
        public int count = 1;
        public float interval = 1.0f;
    }

    [System.Serializable]
    public class Wave
    {
        public string name = "Wave";
        public float startDelay = 2.0f;
        public float endDelay = 3.0f;
        public int maxAliveOverride = 0;
        public List<EnemyEntry> entries = new List<EnemyEntry>();
    }

    [SerializeField] private string _nightName = "Night 01";
    [SerializeField] private List<Wave> _waves = new List<Wave>();

    public string nightName => _nightName;
    public List<Wave> waves => _waves;
}
