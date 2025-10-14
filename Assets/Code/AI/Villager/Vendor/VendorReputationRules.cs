using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/Vendor/Rep Rules")]
public class VendorReputationRules : ScriptableObject
{
    [Serializable]
    public class Range
    {
        public int min = -100;
        public int max = 100;
        [Tooltip("Items to guarantee in stock (will be added if missing).")]
        public List<ItemDef> add = new();

        [Tooltip("Items to remove/hide at this rep.")]
        public List<ItemDef> remove = new();

        [Tooltip("Extra price multiplier (e.g., 0.9 at high rep, 1.15 at low rep).")]
        public float priceMultiplier = 1f;

        [Serializable] public class OverridePrice { public ItemDef item; public int price = -1; }
        [Tooltip("Optional hard prices for some items at this rep.")]
        public List<OverridePrice> overrides = new();
    }

    public List<Range> ranges = new();

    public Range GetRangeFor(int rep)
    {
        for (int i = 0; i < ranges.Count; i++)
        {
            var r = ranges[i];
            if (rep >= r.min && rep <= r.max) return r;
        }
        return null;
    }
}
