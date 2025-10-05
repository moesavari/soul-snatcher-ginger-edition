using Game.Core.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlotWidget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private EquipmentSlotType _slot;
    [SerializeField] private Image _icon;          
    [SerializeField] private Sprite _emptyFrame;   

    [SerializeField] private Inventory _inventory;
    [SerializeField] private Equipment _equipment;
    [SerializeField] private ItemTooltipUI _tooltip;
    [SerializeField] private ItemContextMenuUI _context;

    private CharacterSheetUI _sheet;
    private Sprite _baseSprite;
    private bool _subscribed;

    public EquipmentSlotType slot => _slot;

    private void HandleEquipmentChanged()  
    {
        Refresh();
    }

    private void HandleSlotChanged(EquipmentSlotType s, ItemDef oldItem, ItemDef newItem)
    {
        if (s == _slot) Refresh();
    }

    public void Bind(Equipment eq, CharacterSheetUI sheet, Inventory inv, ItemTooltipUI tip, ItemContextMenuUI ctx)
    {
        _equipment = eq; 
        _inventory = inv;
        _tooltip = tip;
        _context = ctx; 
        _sheet = sheet;

        // Be resilient: auto-locate an Image if not assigned
        if (!_icon)
        {
            _icon = GetComponent<Image>();
            if (!_icon) _icon = GetComponentInChildren<Image>(true);
        }

        if (_icon)
        {
            if (_baseSprite == null)
                _baseSprite = _emptyFrame ? _emptyFrame : _icon.sprite;

            // make sure it can actually render
            _icon.enabled = true;
            var c = _icon.color; c.a = 1f; _icon.color = c;
            _icon.raycastTarget = true; // avoid blocking clicks accidentally
        }

        SubscribeModel();
        Refresh();
    }

    private void OnDestroy()
    {
        UnsubscribeModel();
    }

    private void SubscribeModel()
    {
        if (_subscribed || _equipment == null) return;

        _equipment.OnEquipmentChanged += HandleEquipmentChanged;
        _equipment.OnSlotChanged += HandleSlotChanged;

        _subscribed = true;
    }

    private void UnsubscribeModel()
    {
        if (!_subscribed || _equipment == null) return;

        _equipment.OnEquipmentChanged -= HandleEquipmentChanged;
        _equipment.OnSlotChanged -= HandleSlotChanged;

        _subscribed = false;
    }

    public void Refresh()
    {
        if (!_icon)
        {
            DebugManager.LogWarning($"No Image found for slot {_slot}. " +
                             $"Assign _icon or add an Image as a child.", this);
            return;
        }

        if (_equipment == null)
        {
            SetSprite(_baseSprite, "(no equipment bound)");
            return;
        }

        var def = _equipment.GetEquipped(_slot);
        var targetSprite = (def && def.icon) ? def.icon : _baseSprite;
        SetSprite(targetSprite, def ? def.name : "(empty)");

        // Extra visibility diagnostics
        if (!gameObject.activeInHierarchy)
            DebugManager.LogWarning($"GameObject inactive; icon won’t render.", this);
        var cg = GetComponentInParent<CanvasGroup>();
        if (cg && cg.alpha < 0.99f)
            DebugManager.LogWarning($"CanvasGroup alpha={cg.alpha} (may hide icon).", this);
        if (!_icon.enabled)
            DebugManager.LogWarning($"Image.enabled is false (forcing true).", this);
    }

    private void SetSprite(Sprite sprite, string label)
    {
        if (!_icon) return;

        _icon.enabled = true;
        var c = _icon.color; c.a = 1f; _icon.color = c;
        _icon.sprite = sprite;

        //DebugManager.Log($"Slot {_slot} -> {label} | sprite={(sprite ? sprite.name : "null")}", this);
    }

    // ---------- UI Events ----------
    public void OnPointerEnter(PointerEventData e)
    {
        var def = _equipment ? _equipment.GetEquipped(_slot) : null;
        if (!def) return;
        _tooltip?.ShowFrom(UIPanelID.Equipment, this, def, e.position); // adapt to your tooltip API
    }

    public void OnPointerExit(PointerEventData e)
    {
        _tooltip?.Hide();
    }

    public void OnPointerClick(PointerEventData e)
    {
        var def = _equipment ? _equipment.GetEquipped(_slot) : null;
        if (!def) { DebugManager.Log("Click on empty slot", this); return; }

        switch (e.button)
        {
            case PointerEventData.InputButton.Left:
                // Open context menu (Unequip / Inspect, etc.)
                _context?.ShowForEquipped(_slot, def, e.position, OnContextAction);
                break;

            case PointerEventData.InputButton.Right:
                // Quick Unequip to inventory
                TryUnequipToInventory(def);
                break;
            default:
                DebugManager.LogWarning($"Unhandled mouse button: {e.button}", this);
                break;
        }
    }

    private void TryUnequipToInventory(ItemDef def)
    {
        if (_equipment == null) { DebugManager.LogWarning("Unequip missing _equipment", this); return; }

        var current = _equipment.GetEquipped(_slot);
        if (!current) { DebugManager.LogWarning("Unequip: slot empty", this); return; }

        // Equipment handles moving the item back to inventory internally.
        var ok = _equipment.Unequip(_slot);
        if (!ok) { DebugManager.LogWarning("Unequip failed", this); return; }

        _tooltip?.Hide();
        _sheet?.RefreshAll();
    }

    private void OnContextAction(string action, EquipmentSlotType s, ItemDef d)
    {
        switch (action)
        {
            case "Unequip":
                TryUnequipToInventory(d);
                break;

            case "Inspect":
                _tooltip?.Show(d, Input.mousePosition);
                break;

            default:
                DebugManager.LogWarning($"Unknown context action: {action}", this);
                break;
        }
    }
}
