using UnityEngine;
using TMPro;
using System;
using Game.Core.Inventory;

public sealed class CharacterSheetUI : MonoBehaviour
{
    [Header("Refs (auto via PlayerContext if left empty)")]
    [SerializeField] private Equipment _equipment;
    [SerializeField] private CharacterStats _stats;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private ItemDef _selectedItem;
    [SerializeField] private ItemTooltipUI _tooltip;
    [SerializeField] private ItemContextMenuUI _context;

    [Header("Slots")]
    [SerializeField] private EquipmentSlotWidget[] _slots;

    [Header("Quickbar")]
    [SerializeField] private QuickbarUI _quickbar;

    [Header("Stats Value Text")]
    [SerializeField] private TMP_Text _healthValueText;
    [SerializeField] private TMP_Text _armorValueText;
    [SerializeField] private TMP_Text _attackPowerValueText;
    [SerializeField] private TMP_Text _spellPowerValueText;
    [SerializeField] private TMP_Text _attackSpeedValueText;
    [SerializeField] private TMP_Text _critChanceValueText;
    [SerializeField] private TMP_Text _moveSpeedValueText;
    [SerializeField] private TMP_Text _cooldownReductionValueText;

    public ItemDef selectedItem => _selectedItem;
    private bool _subscribed;
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
        SubscribeModel();
        RefreshAll();
        _quickbar?.SetDockVisible(true);
        OnVisibilityChanged?.Invoke(true);
    }
    private void OnDisable()
    {
        UnsubscribeModel();
        _quickbar?.SetDockVisible(false);
        OnVisibilityChanged?.Invoke(false);
    }

    private void TryBindFromContext()
    {
        var f = PlayerContext.Instance ? PlayerContext.Instance.facade : null;
        if (!f) return;
        _equipment = f.equipment;
        _inventory = f.inventory;
        _stats = f.stats;
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
            _inventory?.TryRemove(def, 1);
        RefreshAll();
        return ok;
    }
    public void TryUnequip(EquipmentSlotType slot)
    {
        if (!_equipment) return;
        var def = _equipment.GetEquipped(slot);
        _equipment.Unequip(slot);
        if (def) _inventory?.TryAdd(def, 1, out int leftover);
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
        if (_healthValueText) _healthValueText.text = _stats ? _stats.Health.ToString() : "00";
        if (_armorValueText) _armorValueText.text = _stats ? _stats.Armor.ToString() : "00";
        if (_attackPowerValueText) _attackPowerValueText.text = _stats ? _stats.AttackPower.ToString() : "00";
        if (_spellPowerValueText) _spellPowerValueText.text = _stats ? _stats.SpellPower.ToString() : "00";
        if (_attackSpeedValueText) _attackSpeedValueText.text = _stats ? _stats.AttackSpeed.ToString() : "00";
        if (_critChanceValueText) _critChanceValueText.text = _stats ? _stats.CritChance.ToString("F0") : "00";
        if (_moveSpeedValueText) _moveSpeedValueText.text = _stats ? _stats.MoveSpeed.ToString() : "00";
        if (_cooldownReductionValueText) _cooldownReductionValueText.text = _stats ? _stats.CooldownReduction.ToString() : "00";
    }

    public void SetSelectedItem(ItemDef def) => _selectedItem = def;
}
