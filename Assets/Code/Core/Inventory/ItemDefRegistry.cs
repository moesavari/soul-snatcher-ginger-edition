using System.Collections.Generic;
using UnityEngine;

public class ItemDefRegistry : MonoSingleton<ItemDefRegistry>
{
    [SerializeField] private List<ItemDef> _itemDefs = new();
    public List<ItemDef> allDefs => _itemDefs;

    private Dictionary<string, ItemDef> _byCode = new();
    public IReadOnlyDictionary<string, ItemDef> byCode => _byCode;

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();

        _byCode = new Dictionary<string, ItemDef>();

        foreach (var def in _itemDefs)
        {
            if (def == null) continue;
            if (string.IsNullOrWhiteSpace(def.itemCode))
            {
                DebugManager.LogWarning($"[ItemDefRegistry] ItemDef '{def.displayName}' has empty itemCode.", this);
                continue;
            }

            if (_byCode.ContainsKey(def.itemCode))
            {
                DebugManager.LogWarning($"[ItemDefRegistry] Duplicate itemCode '{def.itemCode}'.", this);
                continue;
            }

            _byCode[def.itemCode] = def;
        }

        DebugManager.Log($"[ItemDefRegistry] Registered {_byCode.Count} ItemDefs.", this);
    }

    public bool TryGet(string code, out ItemDef def)
    {
        return _byCode.TryGetValue(code, out def);
    }
}
