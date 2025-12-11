using Game.Core.Inventory;
using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Equipment : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Inventory _inventory;
    public Inventory inventory => _inventory;

    [Header("Slots")]
    [SerializeField] private ItemDef _mainHand;
    [SerializeField] private ItemDef _offHand;
    [SerializeField] private ItemDef _head;
    [SerializeField] private ItemDef _chest;
    [SerializeField] private ItemDef _legs;
    [SerializeField] private ItemDef _boots;
    [SerializeField] private ItemDef _amulet;
    [SerializeField] private ItemDef _relic;

    [Header("Quickbar (consumables only)")]
    [SerializeField, Range(1, 6)] private int _quickSlots = 4;
    [SerializeField] private ItemDef[] _quickItems;

    public int quickSlots => _quickSlots;
    public ItemDef[] quickItems => _quickItems;

    public event Action<EquipmentSlotType, ItemDef, ItemDef> OnSlotChanged;
    public event Action<int, ItemDef> OnQuickSet;
    public event Action OnEquipmentChanged;

    private void OnValidate()
    {
        if (_quickItems == null || _quickItems.Length != _quickSlots)
            _quickItems = new ItemDef[_quickSlots];
    }

    public ItemDef GetEquipped(EquipmentSlotType slot) => slot switch
    {
        EquipmentSlotType.MainHand => _mainHand,
        EquipmentSlotType.Offhand => _offHand,
        EquipmentSlotType.Head => _head,
        EquipmentSlotType.Chest => _chest,
        EquipmentSlotType.Legs => _legs,
        EquipmentSlotType.Boots => _boots,
        EquipmentSlotType.Amulet => _amulet,
        EquipmentSlotType.Relic => _relic,
        _ => WarnAndNull()
    };

    public bool Equip(ItemDef def)
    {
        if (!CanEquip(def)) return false;
        var slot = def.equipSlot;

        if (_inventory != null && !_inventory.TryRemove(def, 1))
        {
            DebugManager.LogWarning($"Inventory does not contain: {def?.displayName}", this);
            return false;
        }

        if (slot == EquipmentSlotType.MainHand && (def?.twoHanded ?? false))
        {
            if (_offHand != null) ReturnToInventory(_offHand);
            _offHand = null;
            OnSlotChanged?.Invoke(EquipmentSlotType.Offhand, null, null);
        }

        if (slot == EquipmentSlotType.Offhand && (_mainHand?.twoHanded ?? false))
        {
            DebugManager.LogWarning("Cannot equip offhand with a two-handed main weapon equipped.", this);
            if (_inventory != null) _inventory.TryAdd(def, 1, out _);
            return false;
        }

        var prev = SetSlot(slot, def);
        if (prev != null) ReturnToInventory(prev);

        OnEquipmentChanged?.Invoke();
        EquipmentEvents.RaiseEquipmentChanged();
        return true;
    }

    public bool Unequip(EquipmentSlotType slot)
    {
        var current = GetEquipped(slot);
        if (current == null) return false;

        if (_inventory != null && !_inventory.TryAdd(current, 1, out int leftover))
        {
            DebugManager.LogWarning("Inventory full  cannot unequip.", this);
            return false;
        }

        SetSlot(slot, null);
        OnEquipmentChanged?.Invoke();
        EquipmentEvents.RaiseEquipmentChanged();
        return true;
    }

    public bool CanEquip(ItemDef def)
    {
        if (def == null) return false;
        if (def.kind != ItemKind.Weapon && def.kind != ItemKind.Armor && def.kind != ItemKind.Accessory) return false;
        return true;
    }

    public bool SetQuickItem(int index, ItemDef def)
    {
        if (index < 0 || index >= _quickItems.Length) return false;
        if (def != null && def.kind != ItemKind.Consumable)
        {
            DebugManager.LogWarning("Only consumables can be assigned to quick slots.", this);
            return false;
        }

        _quickItems[index] = def;
        OnQuickSet?.Invoke(index, def);
        EquipmentEvents.RaiseQuickbarChanged(index, def);
        return true;
    }

    public ItemDef GetQuickItem(int index)
    {
        if (index < 0 || index >= _quickItems.Length) return null;
        return _quickItems[index];
    }

    private ItemDef SetSlot(EquipmentSlotType slot, ItemDef def)
    {
        ItemDef old = null;
        switch (slot)
        {
            case EquipmentSlotType.MainHand: old = _mainHand; _mainHand = def; break;
            case EquipmentSlotType.Offhand: old = _offHand; _offHand = def; break;
            case EquipmentSlotType.Head: old = _head; _head = def; break;
            case EquipmentSlotType.Chest: old = _chest; _chest = def; break;
            case EquipmentSlotType.Legs: old = _legs; _legs = def; break;
            case EquipmentSlotType.Boots: old = _boots; _boots = def; break;
            case EquipmentSlotType.Amulet: old = _amulet; _amulet = def; break;
            case EquipmentSlotType.Relic: old = _relic; _relic = def; break;
            default: DebugManager.LogWarning("Unknown slot.", this); break;
        }
        OnSlotChanged?.Invoke(slot, old, def);
        return old;
    }

    private void ReturnToInventory(ItemDef item)
    {
        if (item == null || _inventory == null) return;
        if (!_inventory.TryAdd(item, 1, out int leftover) || leftover > 0)
            DebugManager.LogWarning("Inventory overflow while returning an item.", this);
    }

    private static ItemDef WarnAndNull()
    {
        DebugManager.LogWarning("Unknown equipment slot.");
        return null;
    }

    [Serializable]
    public struct EquippedEntry
    {
        public EquipmentSlotType slot;
        public ItemDef item;
        public Sprite icon;
    }

    public List<EquippedEntry> SnapshotEquipped()
    {
        var list = new List<EquippedEntry>();
        foreach (EquipmentSlotType slot in Enum.GetValues(typeof(EquipmentSlotType)))
        {
            var def = GetEquipped(slot);
            list.Add(new EquippedEntry
            {
                slot = slot,
                item = def,
                icon = def ? def.icon : null
            });
        }
        return list;
    }
}
