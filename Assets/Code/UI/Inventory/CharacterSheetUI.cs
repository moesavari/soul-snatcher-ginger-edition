using Game.Core.Inventory;
using TMPro;
using UnityEngine;

public class CharacterSheetUI : MonoBehaviour
{
    [Header("Refs (optional; auto via PlayerContext)")]
    [SerializeField] private Equipment _equipment;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private CharacterStats _stats;

    [Header("Slots")]
    [SerializeField] private EquipmentSlotUI[] _slots;

    [Header("Quickbar Dock")]
    [SerializeField] private QuickbarUI _quickbar;

    [Header("Stats Panel")]
    [SerializeField] private TMP_Text _attackText;
    [SerializeField] private TMP_Text _armorText;

    [Header("Selection")]
    [SerializeField] private ItemDef _selectedItem;
    public ItemDef selectedItem => _selectedItem;

    private void Awake()
    {
        if (_slots == null || _slots.Length == 0) _slots = GetComponentsInChildren<EquipmentSlotUI>(true);
        TryBindFromContext();
        for (int i = 0; i < _slots.Length; i++) _slots[i]?.Bind(_equipment, this);
    }

    private void OnEnable()
    {
        if (PlayerContext.Instance != null)
        {
            PlayerContext.Instance.OnPlayerReady += HandlePlayerReady;
            PlayerContext.Instance.OnPlayerCleared += HandlePlayerCleared;
        }
        Subscribe();
        RefreshAll();
        _quickbar?.SetDockVisible(true);
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (PlayerContext.Instance != null)
        {
            PlayerContext.Instance.OnPlayerReady -= HandlePlayerReady;
            PlayerContext.Instance.OnPlayerCleared -= HandlePlayerCleared;
        }
        _quickbar?.SetDockVisible(false);
    }

    private void HandlePlayerReady()
    {
        TryBindFromContext();
        for (int i = 0; i < _slots.Length; i++) _slots[i]?.Bind(_equipment, this);
        Subscribe();
        RefreshAll();
    }

    private void HandlePlayerCleared()
    {
        Unsubscribe();
        _equipment = null; _inventory = null; _stats = null;
        RefreshAll();
    }

    private void TryBindFromContext()
    {
        if (PlayerContext.Instance?.facade == null) return;
        var f = PlayerContext.Instance.facade;
        if (_equipment == null) _equipment = f.equipment;
        if (_inventory == null) _inventory = f.inventory;
        if (_stats == null) _stats = f.stats;
        if (_quickbar != null && _quickbar.equipment == null) _quickbar.BindEquipment(f.equipment);
    }

    private void Subscribe()
    {
        if (_equipment != null)
        {
            _equipment.OnEquipmentChanged += RefreshAll;
            _equipment.OnSlotChanged += (_, __, ___) => RefreshAll();
            _equipment.OnQuickSet += (_, __) => _quickbar?.RefreshIcons();
        }
        if (_stats != null) _stats.OnStatsChanged += _ => RefreshStats();
    }

    private void Unsubscribe()
    {
        if (_equipment != null)
        {
            _equipment.OnEquipmentChanged -= RefreshAll;
            _equipment.OnSlotChanged -= (_, __, ___) => RefreshAll();
            _equipment.OnQuickSet -= (_, __) => _quickbar?.RefreshIcons();
        }
        if (_stats != null) _stats.OnStatsChanged -= _ => RefreshStats();
    }

    public void SetSelectedItem(ItemDef def) { _selectedItem = def; }
    public void TryEquipTo(EquipmentSlotType slot)
    {
        if (_equipment == null) return;
        if (_selectedItem == null) { Debug.LogWarning("Select an item from the bag first."); return; }
        if (_selectedItem.equipSlot != slot) { Debug.LogWarning("Item doesn't fit that slot."); return; }
        if (_equipment.Equip(_selectedItem)) _selectedItem = null;
    }
    public void TryUnequip(EquipmentSlotType slot) { _equipment?.Unequip(slot); }

    private void RefreshAll()
    {
        for (int i = 0; i < _slots.Length; i++) _slots[i]?.Refresh();
        _quickbar?.RefreshIcons();
        RefreshStats();
    }

    private void RefreshStats()
    {
        if (_attackText) _attackText.text = _stats != null ? _stats.attack.ToString() : "0";
        if (_armorText) _armorText.text = _stats != null ? _stats.armor.ToString() : "0";
    }

    public void TryEquipDirect(ItemDef def)
    {
        if (!def) return;
        if (_equipment && def.equipSlot != EquipmentSlotType.None)
        {
            _equipment.Equip(def);
        }
    }
}