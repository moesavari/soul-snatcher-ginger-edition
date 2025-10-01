using UnityEngine;

public class InventorySeeder : MonoBehaviour
{
    [System.Serializable]
    public struct Entry { public ItemDef item; public int count; }

    [SerializeField] private Entry[] _items;

    private void Start()
    {
        var inv = PlayerContext.Instance?.facade?.inventory;
        if (inv == null) { DebugManager.LogWarning("No inventory found", this); return; }

        foreach (var e in _items)
        {
            if (!e.item) continue;
            inv.TryAdd(e.item, Mathf.Max(1, e.count), out _);
        }
        DebugManager.Log($"Seeded {_items.Length} entries.", this);
    }
}
