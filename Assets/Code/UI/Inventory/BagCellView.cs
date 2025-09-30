using Game.Core.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BagCellView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerMoveHandler
{
    [Header("Data (bound by InventoryBagUI)")]
    [SerializeField] private ItemDef item;
    [SerializeField] private int count;

    [Header("UI")]
    [SerializeField] private Image _icon;
    [SerializeField] private Sprite _emptyFrame;

    [Header("Shared popups")]
    [SerializeField] private ItemTooltipUI _tooltip;
    [SerializeField] private ItemContextMenuUI _menu;

    private Sprite _baseSprite;
    private Inventory _inv;
    private Equipment _eq;

    private void Reset()
    {
        if (!_icon) _icon = GetComponent<Image>();
    }

    private void Awake()
    {
        if (!_icon) _icon = GetComponent<Image>();
        _baseSprite = _emptyFrame ? _emptyFrame : (_icon ? _icon.sprite : null);

        if (_menu)
        {
            _menu.Init(Equip, Unequip, Use, Destroy, Split);
        }

        TryResolve();
        Refresh();
    }

    // Called by InventoryBagUI when painting the grid
    public void Bind(ItemDef def, int amount)
    {
        item = def;
        count = amount;
        Refresh();
    }

    public void Refresh()
    {
        if (_icon == null) return;
        _icon.enabled = true; // never hide frame entirely
        _icon.sprite = (item && item.icon) ? item.icon : _baseSprite;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        TryResolve();
        if (item != null && _tooltip != null)
            _tooltip.Show(item, e.position);
    }

    public void OnPointerMove(PointerEventData e)
    {
        if (item != null) _tooltip?.Move(e.position);
    }

    public void OnPointerExit(PointerEventData e) => _tooltip?.Hide();

    public void OnPointerClick(PointerEventData e)
    {
        TryResolve();
        if (item == null) return;

        if (e.button == PointerEventData.InputButton.Left)
        {
            bool equipped = IsEquipped(item);
            _menu?.Show(item, equipped, e.position);
        }
        else if (e.button == PointerEventData.InputButton.Right)
        {
            if (IsEquipped(item)) Unequip(item);
            else Equip(item);
        }
    }

    private void TryResolve()
    {
        if (_inv != null && _eq != null) return;
        var ctx = PlayerContext.Instance;
        if (ctx && ctx.facade)
        {
            _inv = ctx.facade.inventory;
            _eq = ctx.facade.equipment;
        }
    }

    private bool IsEquipped(ItemDef def)
    {
        return _eq != null && def != null && _eq.GetEquipped(def.equipSlot) == def;
    }

    private void Equip(ItemDef def)
    {
        TryResolve();
        if (_eq == null || def == null) return;
        var ok = _eq.Equip(def);
        if (!ok) return;

        // If your model doesn't consume, do it here:
        _inv?.TryRemove(def, 1);

        var sheet = FindAnyObjectByType<CharacterSheetUI>();
        sheet?.RefreshAll();

        var bag = FindAnyObjectByType<InventoryBagUI>();
        bag?.Redraw();
    }

    private void Unequip(ItemDef def)
    {
        if (_eq == null || def == null) return;
        _eq.Unequip(def.equipSlot);
    }

    private void Use(ItemDef def)
    {
        DebugManager.Log("[BagCellView] Use not setup yet.");
    }

    private void Destroy()
    {
        DebugManager.Log("[BagCellView] Destroy not setup yet.");
    }

    private void Split(ItemDef def)
    {
        DebugManager.Log("[BagCellView] Split not setup yet.");
    }
}
