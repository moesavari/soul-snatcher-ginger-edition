using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LootTable", menuName = "SoulSnatched/Rewards/Loot Table")]
public class LootTable : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public ScriptableObject item;
        public int weight = 10;
        public int minQuantity = 1;
        public int maxQuantity = 1;
    }

    [SerializeField] private List<Entry> _entries = new();

    public bool TryRoll(out ScriptableObject item, out int quantity)
    {
        item = null;
        quantity = 0;

        if (_entries == null || _entries.Count == 0)
        {
            DebugManager.LogWarning("LootTable has no entries.", this);
            return false;
        }

        int total = 0;
        for (int i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];
            if (e == null || e.item == null || e.weight <= 0) continue;
            total += e.weight;
        }

        if (total <= 0)
        {
            DebugManager.LogWarning("LootTable total weight is 0.", this);
            return false;
        }

        int roll = UnityEngine.Random.Range(0, total);
        for (int i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];
            if (e == null || e.item == null || e.weight <= 0) continue;

            roll -= e.weight;
            if (roll < 0)
            {
                item = e.item;
                quantity = UnityEngine.Random.Range(e.minQuantity, e.maxQuantity + 1);
                quantity = Mathf.Max(1, quantity);
                return true;
            }
        }

        return false;
    }
}
