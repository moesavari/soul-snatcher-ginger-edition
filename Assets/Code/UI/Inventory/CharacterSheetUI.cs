using UnityEngine;
using TMPro;
using Game.Core.Inventory;
using System;

public sealed class CharacterSheetUI : MonoBehaviour
{
    [Header("Refs (auto via PlayerContext if left empty)")]
    [SerializeField] private Equipment _equipment;
    [SerializeField] private CharacterStats _stats;
    [SerializeField] private Inventory _inventory;
    
    [Space(8)]
    [SerializeField] private ItemDef _selectedItem;
    [SerializeField] private ItemTooltipUI _tooltip;
    [SerializeField] private ItemContextMenuUI _context;

    [Header("Slots")]
    [SerializeField] private EquipmentSlotWidget[] _slots;

    [Header("Quickbar")]
    [SerializeField] private QuickbarUI _quickbar;

    [Header("Stats Text")]
    [SerializeField] private TMP_Text _attackText;
    [SerializeField] private TMP_Text _armorText;

    private bool _subscribed;
    public ItemDef selectedItem => _selectedItem;

    public static event Action<bool> OnVisibilityChanged;

    private void Awake()
    {
        if (_slots == null || _slots.Length == 0)
            _slots = GetComponentsInChildren<EquipmentSlotWidget>(true);

        TryBindFromContext();
        BindSlots();
        SubscribeModel();
        RefreshAll();
    }

    private void OnEnable()
    {
        if (PlayerContext.Instance != null)
        {
            PlayerContext.Instance.OnPlayerReady += HandlePlayerReady;
            PlayerContext.Instance.OnPlayerCleared += HandlePlayerCleared;
        }

        SubscribeModel();
        RefreshAll();
        _quickbar?.SetDockVisible(true);

        OnVisibilityChanged?.Invoke(true);
    }

    private void OnDisable()
    {
        if (PlayerContext.Instance != null)
        {
            PlayerContext.Instance.OnPlayerReady -= HandlePlayerReady;
            PlayerContext.Instance.OnPlayerCleared -= HandlePlayerCleared;
        }

        UnsubscribeModel();
        _quickbar?.SetDockVisible(false);

        OnVisibilityChanged?.Invoke(false);
    }

    private void HandlePlayerReady()
    {
        TryBindFromContext();  
        BindSlots();           
        SubscribeModel();      
        RefreshAll();
    }

    private void HandlePlayerCleared()
    {
        UnsubscribeModel();
        _equipment = null; _inventory = null; _stats = null;
        BindSlots();           
        RefreshAll();
    }

    private void TryBindFromContext()
    {
        var f = PlayerContext.Instance ? PlayerContext.Instance.facade : null;

        if (!f) return;

        _equipment  = f.equipment;
        _inventory  = f.inventory;
        _stats      = f.stats;

        BindSlots();
        SubscribeModel();
        RefreshAll();
    }

    private void BindSlots()
    {
        if (_slots == null || _slots.Length == 0)
            _slots = GetComponentsInChildren<EquipmentSlotWidget>(true);

        for (int i = 0; i < _slots.Length; i++)
            _slots[i]?.Bind(_equipment, this, _inventory, _tooltip, _context);
    }

    private void SubscribeModel()
    {
        if (_subscribed || !_equipment) return;

        // prefer method-group handlers so unsubscribe is correct
        _equipment.OnEquipmentChanged += OnEquipmentChanged;
        _equipment.OnSlotChanged += OnSlotChanged;
        _equipment.OnQuickSet += OnQuickSet;

        if (_stats) _stats.OnStatsChanged += OnStatsChanged;

        _subscribed = true;
    }

    private void UnsubscribeModel()
    {
        if (!_subscribed) return;

        if (_equipment)
        {
            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
            _equipment.OnSlotChanged -= OnSlotChanged;
            _equipment.OnQuickSet -= OnQuickSet;
        }
        if (_stats) _stats.OnStatsChanged -= OnStatsChanged;

        _subscribed = false;
    }

    private void OnEquipmentChanged() => RefreshAll();
    private void OnSlotChanged(EquipmentSlotType _, ItemDef __, ItemDef ___) => RefreshAll();
    private void OnQuickSet(int __, ItemDef ___) => _quickbar?.RefreshIcons();
    private void OnStatsChanged(CharacterStats __) => RefreshStats();

    public bool TryEquip(ItemDef def)
    {
        if (!_equipment || !def) return false;
        var ok = _equipment.Equip(def);
        if (ok)
        {
            _inventory?.TryRemove(def, 1); // if model doesn't consume, do it here
            RefreshAll();
        }
        return ok;
    }

    public void TryUnequip(EquipmentSlotType slot)
    {
        if (!_equipment) return;
        var def = _equipment.GetEquipped(slot);
        _equipment.Unequip(slot);
        if (def) _inventory?.TryAdd(def, 1, out int leftover); // match your Inventory API
        RefreshAll();
    }

    public void RefreshAll()
    {
        if (_slots != null)
            for (int i = 0; i < _slots.Length; i++)
                _slots[i]?.Refresh();

        _quickbar?.RefreshIcons();
        RefreshStats();
    }

    private void RefreshStats()
    {
        if (_attackText) _attackText.text = _stats ? _stats.attack.ToString() : "0";
        if (_armorText) _armorText.text = _stats ? _stats.armor.ToString() : "0";
    }

    public void SetSelectedItem(ItemDef def)
    {
        _selectedItem = def;
    }
}
