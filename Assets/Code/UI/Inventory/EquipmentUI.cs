// EquipmentUI.cs
using Game.Core.Inventory;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject _root;   // EquipmentPanel

    [Header("Optional explicit refs (can be left blank)")]
    [SerializeField] private Inventory _inventory;
    [SerializeField] private Equipment _equipment;
    [SerializeField] private ItemTooltipUI _tooltip;
    [SerializeField] private ItemContextMenuUI _context;

    private readonly List<EquipmentSlotWidget> _slots = new();

    private int _lastToggleFrame = -1;

    private void Awake()
    {
        if (_root) _root.SetActive(false);

        // Auto-resolve if not assigned
        if (!_inventory) _inventory = PlayerContext.Instance?.facade?.inventory;
        if (!_equipment) _equipment = PlayerContext.Instance?.facade?.equipment;
        if (!_tooltip) _tooltip = FindFirstObjectByType<ItemTooltipUI>();
        if (!_context) _context = FindFirstObjectByType<ItemContextMenuUI>();

        _slots.Clear();
        if (_root)
            _root.GetComponentsInChildren(true, _slots);
        else
            GetComponentsInChildren(true, _slots);

        // Bind every slot widget to the same data sources
        foreach (var w in _slots)
            if (w) w.Bind(_equipment, this, _inventory, _tooltip, _context);
    }

    private void OnEnable()
    {
        RefreshAll();

        InputManager.ToggleEquipmentPressed += OnToggleEquipment;
        InputManager.EscapePressed          += OnEscape;
    }

    private void OnDisable()
    {
        InputManager.ToggleEquipmentPressed -= OnToggleEquipment;
        InputManager.EscapePressed          -= OnEscape;
    }

    private void OnToggleEquipment()
    {
        if (!_root) return;
        if (_lastToggleFrame == Time.frameCount) return;

        _lastToggleFrame = Time.frameCount;
        _root.SetActive(!_root.activeSelf);

        if (_root.activeSelf) RefreshAll();
    }

    private void OnEscape()
    {
        if (_root && _root.activeSelf) _root.SetActive(false);
    }

    public void Show() { if (_root) { _root.SetActive(true); RefreshAll(); } }
    public void Hide() { if (_root) _root.SetActive(false); }
    public void Toggle() { if (_root) { _root.SetActive(!_root.activeSelf); if (_root.activeSelf) RefreshAll(); } }

    public void RefreshAll()
    {
        foreach (var w in _slots) if (w) w.Refresh();
    }
}
