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
                Debug.LogWarning($"[ItemDefRegistry] ItemDef '{def.displayName}' has empty itemCode.");
                continue;
            }

            if (_byCode.ContainsKey(def.itemCode))
            {
                Debug.LogWarning($"[ItemDefRegistry] Duplicate itemCode '{def.itemCode}'.");
                continue;
            }

            _byCode[def.itemCode] = def;
        }

        Debug.Log($"[ItemDefRegistry] Registered {_byCode.Count} ItemDefs.");
    }

    public bool TryGet(string code, out ItemDef def)
    {
        return _byCode.TryGetValue(code, out def);
    }
}
