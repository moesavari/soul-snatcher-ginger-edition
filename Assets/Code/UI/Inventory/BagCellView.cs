// BagCellView.cs
using Game.Core.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;  // add
using UnityEngine.UI;

public class BagCellView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Data")]
    public ItemDef item;
    public int count;

    [Header("UI")]
    [SerializeField] private Image _icon;

    [Header("Refs (assign in inspector once)")]
    [SerializeField] private ItemTooltipUI _tooltip;
    [SerializeField] private ItemContextMenuUI _menu;

    private Sprite _baseSprite;
    private Inventory _inv;
    private Equipment _eq;
    private bool _menuWired;

    private void Awake()
    {
        if (_icon) _baseSprite = _icon.sprite;            // keep your frame sprite
        var ctx = PlayerContext.Instance;
        if (ctx && ctx.facade) { _inv = ctx.facade.inventory; _eq = ctx.facade.equipment; }

        // Wire menu once to avoid stacking listeners
        if (_menu != null && !_menuWired)
        {
            _menu.Init(Equip, Unequip, Use, Destroy, Split);
            _menuWired = true;
        }
    }

    public void Refresh()
    {
        // DO NOT disable the image; only swap sprite if we have an item icon
        if (item != null && item.icon != null)
            _icon.sprite = item.icon;
        else
            _icon.sprite = _baseSprite;
        // (count label update stays wherever you already handle it)
    }

    // ---------- pointer ----------
    public void OnPointerEnter(PointerEventData e)
    {
        if (item != null && _tooltip != null)
            _tooltip.Show(item, e.position);
    }

    public void OnPointerExit(PointerEventData e)
    {
        _tooltip?.Hide();
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (item == null) return;

        if (e.button == PointerEventData.InputButton.Left)
        {
            bool isEquipped = IsEquipped(item);
            _menu?.Show(item, isEquipped, e.position);
        }
        else if (e.button == PointerEventData.InputButton.Right)
        {
            if (IsEquipped(item)) Unequip(item);
            else Equip(item);
        }
    }

    // ---------- actions ----------
    private bool IsEquipped(ItemDef def)
    {
        if (_eq == null || def == null || def.equipSlot == EquipmentSlotType.None) return false;
        return _eq.GetEquipped(def.equipSlot) == def;
    }

    private void Equip(ItemDef def)
    {
        if (_eq == null || def == null || def.equipSlot == EquipmentSlotType.None) return;
        if (_eq.Equip(def))
        {
            // If your Equip() already removes from inventory, do nothing else here.
            // Otherwise uncomment this next line:
            // _inv?.TryRemove(def, 1);
        }
    }

    private void Unequip(ItemDef def)
    {
        if (_eq == null || def == null) return;
        _eq.Unequip(def.equipSlot);
    }

    private void Use(ItemDef def)
    {
        if (_inv == null || def == null || def.kind != ItemKind.Consumable) return;
        _inv.TryRemove(def, 1);
    }

    private void Destroy(ItemDef def)
    {
        if (_inv == null || def == null) return;
        _inv.TryRemove(def, 1);
    }

    private void Split(ItemDef def)
    {
        // hook to your split API later
        Debug.Log("[Inventory] Split not implemented yet.");
    }
}
