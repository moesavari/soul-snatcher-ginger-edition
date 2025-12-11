using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopCellView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("UI")]
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _price;
    [SerializeField] private TMP_Text _qty;
    [SerializeField] private Sprite _emptyFrame;

    [Header("Shared popups")]
    [SerializeField] private ItemTooltipUI _tooltip;

    private ItemDef _def;
    private int _vendorQty;
    private int _priceEach;

    public void ShowEmpty()
    {
        _def = null; _vendorQty = 0; _priceEach = 0;

        if (_icon) { _icon.enabled = true; _icon.sprite = _emptyFrame; }
        if (_price) _price.text = string.Empty;
        if (_qty) { _qty.text = string.Empty; _qty.gameObject.SetActive(false); }
    }

    public void Bind(ItemDef def, int vendorQty, int priceEach, Sprite emptyIcon)
    {
        _def = def;
        _vendorQty = Mathf.Max(0, vendorQty);
        _priceEach = Mathf.Max(0, priceEach);

        if (_icon)
        {
            _icon.enabled = true;
            _icon.sprite = def && def.icon ? def.icon : (emptyIcon ? emptyIcon : _emptyFrame);
        }

        if (_price)
            _price.text = (def != null && _vendorQty > 0) ? _priceEach.ToString() : string.Empty;

        if (_qty)
        {
            bool showQty = def && def.stackable && _vendorQty > 1;
            _qty.gameObject.SetActive(showQty);
            _qty.text = showQty ? _vendorQty.ToString() : string.Empty;
        }
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (_def) _tooltip?.ShowFrom(UIPanelID.Shop, this, _def, e.position);
    }

    public void OnPointerMove(PointerEventData e)
    {
        _tooltip?.Move(e.position);
    }

    public void OnPointerExit(PointerEventData e)
    {
        _tooltip?.Hide();
    }
}
