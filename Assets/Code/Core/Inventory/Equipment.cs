using Game.Core.Inventory;
using System;
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

    public event Action<EquipmentSlotType, ItemDef, ItemDef> OnSlotChanged; // (slot, old, @new)
    public event Action<int, ItemDef> OnQuickSet; // (index, item)
    public event Action OnEquipmentChanged; // aggregate change signal

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
            Debug.LogWarning($"[Equipment] Inventory does not contain: {def?.displayName}");
            return false;
        }

        // Two-handed main-hand clears offhand
        if (slot == EquipmentSlotType.MainHand && (def?.twoHanded ?? false))
        {
            if (_offHand != null) ReturnToInventory(_offHand);
            _offHand = null;
            OnSlotChanged?.Invoke(EquipmentSlotType.Offhand, null, null); // clear signal
        }

        // Offhand blocked if main is two-handed
        if (slot == EquipmentSlotType.Offhand && (_mainHand?.twoHanded ?? false))
        {
            Debug.LogWarning("[Equipment] Cannot equip offhand with a two-handed main weapon equipped.");
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
            Debug.LogWarning("[Equipment] Inventory full – cannot unequip.");
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
            Debug.LogWarning("[Equipment] Only consumables can be assigned to quick slots.");
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
            default: Debug.LogWarning("[Equipment] Unknown slot."); break;
        }
        OnSlotChanged?.Invoke(slot, old, def);
        return old;
    }

    private void ReturnToInventory(ItemDef item)
    {
        if (item == null || _inventory == null) return;
        if (!_inventory.TryAdd(item, 1, out int leftover) || leftover > 0)
            Debug.LogWarning("[Equipment] Inventory overflow while returning an item.");
    }

    private static ItemDef WarnAndNull()
    {
        Debug.LogWarning("[Equipment] Unknown equipment slot.");
        return null;
    }
}