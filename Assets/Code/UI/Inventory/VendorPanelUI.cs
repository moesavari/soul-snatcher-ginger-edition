using Game.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VendorPanelUI : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject _root;
    [SerializeField] private TMP_Text _title;

    [Header("Grid")]
    [SerializeField] private UIGridPool<ShopCellView> _pool;
    [SerializeField] private Sprite _emptyIcon;
    [SerializeField, Min(1)] private int _slotsVisible = 20;

    [Header("Refs")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private ItemContextMenuUI _context;

    private Vendor _vendor;

    private void Awake()
    {
        if (_root != null) _root.SetActive(false);
        if (_pool != null) _pool.BuildOnce();

        DrawEmpty();

        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>(true);
    }

    private void OnEnable()
    {
        if (!ShopController.IsReady) return;

        ShopController.Instance.OnChanged += RedrawFromVendor;
        ShopController.Instance.OnShopOpened += Bind;
    }

    private void OnDisable()
    {
        if (ShopController.IsReady)
        {
            ShopController.Instance.OnChanged -= RedrawFromVendor;
            ShopController.Instance.OnShopOpened -= Bind;
        }

        if (_vendor?.runtimeInventory != null)
            _vendor.runtimeInventory.OnStockChanged -= RedrawFromVendor;
    }

    public void Bind(Vendor vendor)
    {

        if (_vendor?.runtimeInventory != null)
            _vendor.runtimeInventory.OnStockChanged -= RedrawFromVendor;

        _vendor = vendor;

        if (_vendor?.runtimeInventory != null)
            _vendor.runtimeInventory.OnStockChanged += RedrawFromVendor;

        if (_title != null)
            _title.text = _vendor != null ? _vendor.vendorName : "SHOP";

        RedrawFromVendor();
    }

    public void Unbind()
    {
        if (_vendor?.runtimeInventory != null)
            _vendor.runtimeInventory.OnStockChanged -= RedrawFromVendor;

        _vendor = null;
        DrawEmpty();
    }

    public void RedrawFromVendor()
    {
        if (_pool == null) return;

        var inv = _vendor != null ? _vendor.runtimeInventory : null;
        var stock = inv != null ? inv.stock : null;

        int targetCount = Mathf.Min(_slotsVisible, _pool.Cells.Count);

        for (int i = 0; i < _pool.Cells.Count; i++)
            _pool.Cells[i].gameObject.SetActive(i < targetCount);

        for (int i = 0; i < targetCount; i++)
        {
            var cell = _pool.Cells[i];
            var btn = cell.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveAllListeners();

            bool hasStock =
                stock != null &&
                i < stock.Count &&
                stock[i].item != null &&
                stock[i].quantity > 0;

            if (!hasStock)
            {
                cell.ShowEmpty();
                continue;
            }

            var s = stock[i];
            var def = s.item;
            var qty = Mathf.Max(0, s.quantity);

            int basePrice = (int)def.quality * 25 + 10;
            float norm = ReputationSystem.Instance != null
                ? ReputationSystem.Instance.Normalized
                : 0.5f;
            float repMult = _vendor.currentRepPriceMult *
                            Mathf.Lerp(1.2f, 0.85f, norm);

            int priceEach;
            try
            {
                priceEach = inv.GetPrice(def, basePrice, repMult);
            }
            catch
            {
                priceEach = Mathf.Max(1, Mathf.RoundToInt(basePrice * repMult));
            }

            cell.Bind(def, qty, priceEach, _emptyIcon);

            if (btn != null)
            {
                var rt = (RectTransform)cell.transform;
                btn.onClick.AddListener(() =>
                {
                    if (_context == null || _canvas == null) return;

                    Vector2 screenPos =
                        RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, rt.position);

                    _context.ShowShopBuy(def, qty, priceEach, screenPos);
                });
            }
        }
    }

    public void Show(string title = "SHOP")
    {
        if (_title != null) _title.text = title;
        if (_root != null) _root.SetActive(true);

        RedrawFromVendor();
    }

    public void Hide()
    {
        if (_root != null) _root.SetActive(false);
        _context?.Hide();
    }

    public void Toggle(string title = "SHOP")
    {
        if (_root == null) return;

        bool next = !_root.activeSelf;

        if (next && _title != null)
            _title.text = title;

        _root.SetActive(next);

        if (next) RedrawFromVendor();
        else _context?.Hide();
    }

    public void DrawEmpty()
    {
        if (_pool == null) return;

        for (int i = 0; i < _pool.Cells.Count; i++)
        {
            var cell = _pool.Cells[i];
            cell.gameObject.SetActive(i < _slotsVisible);
            if (cell.gameObject.activeSelf)
                cell.ShowEmpty();

            var btn = cell.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveAllListeners();
        }
    }
}
