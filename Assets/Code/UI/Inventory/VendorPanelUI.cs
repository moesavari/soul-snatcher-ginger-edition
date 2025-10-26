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
    [SerializeField] private Canvas _canvas;                 // for screen-point conversion
    [SerializeField] private ItemContextMenuUI _context;     // existing context UI

    private Vendor _vendor;

    private void Awake()
    {
        if (_root) _root.SetActive(false);
        if (_pool) _pool.BuildOnce();
        DrawEmpty();
        if (_canvas == null) _canvas = GetComponentInParent<Canvas>(true);
    }

    private void OnEnable()
    {
        if (ShopController.IsReady)
        {
            ShopController.Instance.OnChanged += RedrawFromVendor;
            ShopController.Instance.OnShopOpened += Bind;
        }
    }

    private void OnDisable()
    {
        if (ShopController.IsReady)
        {
            ShopController.Instance.OnChanged -= RedrawFromVendor;
            ShopController.Instance.OnShopOpened -= Bind;
        }
    }

    public void Bind(Vendor vendor)
    {
        if (_vendor?.runtimeInventory != null)
            _vendor.runtimeInventory.OnStockChanged -= RedrawFromVendor;

        _vendor = vendor;

        if (_vendor?.runtimeInventory != null)
            _vendor.runtimeInventory.OnStockChanged += RedrawFromVendor;

        _title.text = _vendor != null ? _vendor.vendorName : "SHOP";
        RedrawFromVendor();
    }

    public void Unbind()
    {
        _vendor = null;
        DrawEmpty();
    }

    private void HandleShopOpened(Vendor v)
    {
        Bind(v);
    }

    // ---------------- Drawing / Population ----------------

    public void RedrawFromVendor()
    {
        if (_pool == null) return;

        var inv = _vendor != null ? _vendor.runtimeInventory : null;
        var stock = inv != null ? inv.stock : null;

        // how many slots we want visible regardless of stock
        int targetCount = Mathf.Min(_slotsVisible, _pool.Cells.Count);

        // ensure first targetCount cells are ON (others OFF)
        for (int i = 0; i < _pool.Cells.Count; i++)
            _pool.Cells[i].gameObject.SetActive(i < targetCount);

        // fill visible cells
        for (int i = 0; i < targetCount; i++)
        {
            var cell = _pool.Cells[i];
            var btn = cell.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveAllListeners();

            bool hasStock = stock != null && i < stock.Count && stock[i].item != null && stock[i].quantity > 0;

            if (!hasStock)
            {
                cell.ShowEmpty();
                continue;
            }

            var s = stock[i];
            var def = s.item;
            var qty = Mathf.Max(0, s.quantity);

            // price (same math you use elsewhere)
            int basePrice = (int)def.quality * 25 + 10;
            float norm = ReputationSystem.Instance != null ? ReputationSystem.Instance.Normalized : 0.5f;
            float repMult = _vendor.currentRepPriceMult * Mathf.Lerp(1.2f, 0.85f, norm);

            int priceEach;
            try { priceEach = inv.GetPrice(def, basePrice, repMult); }
            catch { priceEach = Mathf.Max(1, Mathf.RoundToInt(basePrice * repMult)); }

            cell.Bind(def, qty, priceEach, _emptyIcon);

            if (btn != null)
            {
                var rt = (RectTransform)cell.transform;
                btn.onClick.AddListener(() =>
                {
                    if (_context == null || _canvas == null) return;
                    Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, rt.position);
                    _context.ShowShopBuy(def, qty, 0, screenPos);   // uses your existing ShowShopBuy API
                });
            }
        }
    }


    // ---------------- Panel visibility ----------------

    public void Show(string title = "SHOP")
    {
        if (_title) _title.text = title;
        if (_root) _root.SetActive(true);
        RedrawFromVendor();
    }

    public void Hide()
    {
        if (_root) _root.SetActive(false);
    }

    public void Toggle(string title = "SHOP")
    {
        if (_root == null) return;
        bool next = !_root.activeSelf;
        if (next && _title) _title.text = title;
        _root.SetActive(next);
        if (next) RedrawFromVendor();
    }

    // ---------------- Helpers ----------------

    public void DrawEmpty()
    {
        if (_pool == null) return;

        for (int i = 0; i < _pool.Cells.Count; i++)
        {
            var cell = _pool.Cells[i];
            cell.gameObject.SetActive(i < _slotsVisible);
            if (cell.gameObject.activeSelf) cell.ShowEmpty();

            var btn = cell.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveAllListeners();
        }
    }
}
