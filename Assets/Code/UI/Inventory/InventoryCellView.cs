using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryCellView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _qty;
    [SerializeField] private Sprite _emptyFrame;

    [Header("Shared popups")]
    [SerializeField] private ItemTooltipUI _tooltip;

    public event Action<ItemDef, int, Vector2> Clicked;

    private ItemDef _def;
    private int _count;

    public void ShowEmpty()
    {
        _def = null; _count = 0;
        if (_icon) { _icon.enabled = true; _icon.sprite = _emptyFrame; }
        if (_qty) { _qty.text = string.Empty; _qty.gameObject.SetActive(false); }
    }

    public void Bind(ItemDef def, int count)
    {
        _def = def; _count = Mathf.Max(0, count);

        if (_icon)
        {
            _icon.enabled = true;
            _icon.sprite = def && def.icon ? def.icon : _emptyFrame;
        }

        if (_qty)
        {
            bool showQty = def && def.stackable && _count > 1;
            _qty.gameObject.SetActive(showQty);
            _qty.text = showQty ? _count.ToString() : string.Empty;
        }
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (_def) _tooltip?.ShowFrom(UIPanelID.Inventory, this, _def, e.position);
    }

    public void OnPointerMove(PointerEventData e)
    {
        _tooltip?.Move(e.position);
    }

    public void OnPointerExit(PointerEventData e)
    {
        _tooltip?.Hide();
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (_def == null) return;
        Clicked?.Invoke(_def, _count, e.position);
    }
}
