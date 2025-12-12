using Game.Core.Inventory;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject _root;

    [Header("Optional explicit refs (can be left blank)")]
    [SerializeField] private Inventory _inventory;
    [SerializeField] private Equipment _equipment;
    [SerializeField] private ItemTooltipUI _tooltip;
    [SerializeField] private ItemContextMenuUI _context;
    [SerializeField] private EquipmentPaperdollUI _paperdoll;
    [SerializeField] private HairVisuals _playerHair;

    private readonly List<EquipmentSlotWidget> _slots = new();

    private int _lastToggleFrame = -1;

    private void Awake()
    {
        if (_root) _root.SetActive(false);

        if (!_inventory) _inventory = PlayerContext.Instance?.facade?.inventory;
        if (!_equipment) _equipment = PlayerContext.Instance?.facade?.equipment;
        if (!_tooltip) _tooltip = FindFirstObjectByType<ItemTooltipUI>();
        if (!_context) _context = FindFirstObjectByType<ItemContextMenuUI>();

        _slots.Clear();
        if (_root)
            _root.GetComponentsInChildren(true, _slots);
        else
            GetComponentsInChildren(true, _slots);

        foreach (var w in _slots)
            if (w) w.Bind(_equipment, this, _inventory, _tooltip, _context);

        RefreshPaperdollFromPlayer();
    }

    private void OnEnable()
    {
        RefreshAll();

        InputManager.ToggleEquipmentPressed += OnToggleEquipment;
        InputManager.EscapePressed          += OnEscape;

        if (_playerHair == null)
            _playerHair = FindFirstObjectByType<HairVisuals>();

        if (_playerHair != null)
            _playerHair.OnHairStageChanged += HandleHairStageChanged;

        RefreshPaperdollFromPlayer();
    }

    private void OnDisable()
    {
        InputManager.ToggleEquipmentPressed -= OnToggleEquipment;
        InputManager.EscapePressed          -= OnEscape;

        if (_playerHair != null)
            _playerHair.OnHairStageChanged -= HandleHairStageChanged;
    }

    private void OnToggleEquipment()
    {
        if (!_root) return;
        if (_lastToggleFrame == Time.frameCount) return;

        _lastToggleFrame = Time.frameCount;

        bool next = !_root.activeSelf;
        _root.SetActive(next);

        if (next) RefreshAll();
        else _context.Hide();
    }

    private void OnEscape()
    {
        if (_root && _root.activeSelf) { _root.SetActive(false); _context?.Hide(); }
    }

    public void Show() { if (_root) { _root.SetActive(true); RefreshAll(); } }
    public void Hide() { if (_root) _root.SetActive(false); _context?.Hide(); }
    public void Toggle()
    {
        if (!_root) return;

        bool next = !_root.activeSelf;
        _root.SetActive(next);

        if (next) RefreshAll();
        else _context?.Hide();
    }

    public void RefreshAll()
    {
        foreach (var w in _slots) if (w) w.Refresh();
    }

    private void HandleHairStageChanged(int newStage)
    {
        RefreshPaperdollFromPlayer();
    }

    public void RefreshPaperdollFromPlayer()
    {
        if (_paperdoll == null) return;

        if (_playerHair == null)
            _playerHair = FindFirstObjectByType<HairVisuals>();

        if (_playerHair == null)
        {
            Debug.LogWarning("[EquipmentUI] No HairVisuals found for paperdoll.");
            return;
        }

        bool isMale = _playerHair.genderType == GenderType.Male;
        int stage = _playerHair.CurrentStage;

        if (stage <= 0) stage = 1;

        _paperdoll.SetPaperdoll(isMale, stage);
    }

}
