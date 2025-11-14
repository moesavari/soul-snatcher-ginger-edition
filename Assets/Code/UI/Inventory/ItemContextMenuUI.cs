using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Systems;

public class ItemContextMenuUI : MonoBehaviour
{
    public enum EquippedContextAction { Unequip, Sell, Cancel }

    [Header("Root")]
    [SerializeField] private GameObject _root;

    [Header("Text")]
    [SerializeField] private TMP_Text _title;

    [Header("Buttons")]
    [SerializeField] private Button _equipBtn;
    [SerializeField] private Button _unequipBtn;
    [SerializeField] private Button _useBtn;
    [SerializeField] private Button _splitBtn;
    [SerializeField] private Button _destroyBtn;
    [SerializeField] private Button _confirmButton;

    [Header("Helpers")]
    [SerializeField] private QuantityPromptUI _quantity;

    [Header("Positioning")]
    [SerializeField] private bool _followMouse = false;
    [SerializeField] private Vector2 _mouseOffset = new Vector2(24f, -24f);
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Vector2 _screenPadding = new Vector2(8f, 8f); // NEW: keep popup inside view

    private RectTransform _rt;
    private ItemDef _def;

    private System.Action _onBuyOne;
    private System.Action<int> _onBuyMany;
    private System.Action _onSellOne;
    private System.Action<int> _onSellMany;
    private System.Action _onShopChanged;

    private bool _visible;
    private ItemDef _currentItem;
    private Vendor _currentVendor;

    private void Awake()
    {
        if (_canvas == null) _canvas = GetComponentInParent<Canvas>(true);
        _rt = _root ? _root.GetComponent<RectTransform>() : null;
        Hide();
    }

    private void Update()
    {
        if (_visible && _followMouse) PositionAt(Input.mousePosition);
    }

    private static void Set(Button b, bool on, string label = null)
    {
        if (!b) return;
        b.gameObject.SetActive(on);
        if (on && label != null)
        {
            var t = b.GetComponentInChildren<TMP_Text>();
            if (t) t.text = label;
        }
        b.onClick.RemoveAllListeners();
    }

    public void Hide()
    {
        _visible = false;
        if (_root) _root.SetActive(false);
    }

    private void OpenAt(Vector2 screenPos)
    {
        if (!_root) return;
        _visible = true;
        _root.SetActive(true);

        PositionAt(screenPos);
    }

    // ---------------- INVENTORY (no shop) ----------------
    public void ShowInventory(ItemDef item, bool equipped, Vector2 screenPos)
    {
        if (!item) { Hide(); return; }
        if (_title) _title.text = item.displayName;

        bool isConsumable = item.kind == ItemKind.Consumable;
        bool canEquip = item.equipSlot != EquipmentSlotType.None;

        Set(_equipBtn, canEquip && !equipped, "Equip");
        Set(_unequipBtn, equipped, "Unequip");
        Set(_useBtn, isConsumable, "Use");
        Set(_splitBtn, item.stackable, "Split");
        Set(_destroyBtn, true, "Destroy");

        if (canEquip && !equipped) _equipBtn.onClick.AddListener(() => { PlayerContext.Instance?.facade?.equipment?.Equip(item); Hide(); });
        if (equipped) _unequipBtn.onClick.AddListener(() => { PlayerContext.Instance?.facade?.equipment?.Unequip(item.equipSlot); Hide(); });
        if (isConsumable) _useBtn.onClick.AddListener(() => { PlayerContext.Instance?.facade?.inventory?.TryRemove(item, 1); Hide(); });
        if (item.stackable) _splitBtn.onClick.AddListener(() =>
        {
            _quantity.Open(Input.mousePosition, 99, amt => { /* internal split if needed */ }, 1, 1);
            Hide();
        });
        _destroyBtn.onClick.AddListener(() => { PlayerContext.Instance?.facade?.inventory?.TryRemove(item, 1); Hide(); });

        _followMouse = false;
        OpenAt(screenPos);
    }

    public void ShowForEquipped(EquipmentSlotType slot, ItemDef item, Vector2 screenPos, System.Action<EquippedContextAction> onAction = null)
    {
        if (!item) { Hide(); return; }
        if (_title) _title.text = item.displayName;

        // Reset buttons
        Set(_equipBtn, false);
        Set(_splitBtn, false);
        Set(_useBtn, false);
        Set(_destroyBtn, true, "Cancel");
        Set(_unequipBtn, true, "Unequip");

        _unequipBtn.onClick.AddListener(() =>
        {
            PlayerContext.Instance?.facade?.equipment?.Unequip(slot);
            onAction?.Invoke(EquippedContextAction.Unequip);
            Hide();
        });

        // If shop is open, allow selling equipped item
        bool shopOpen = ShopController.IsReady && ShopController.Instance.activeVendor != null;
        if (shopOpen)
        {
            Set(_useBtn, true, "Sell");
            _useBtn.onClick.AddListener(() =>
            {
                PlayerContext.Instance?.facade?.equipment?.Unequip(slot);
                ShopController.Instance?.TrySell(item, 1);
                onAction?.Invoke(EquippedContextAction.Sell);
                Hide();
            });
        }

        _destroyBtn.onClick.AddListener(() =>
        {
            onAction?.Invoke(EquippedContextAction.Cancel);
            Hide();
        });

        _followMouse = false;
        OpenAt(screenPos);
    }

    // ---------------- SHOP: BUY ----------------
    public void ShowShopBuy(ItemDef item, int maxQty, int priceEach, Vector2 screenPos)
    {
        if (!item) { Hide(); return; }
        if (_title) _title.text = item.displayName;

        _currentItem = item;
        _currentVendor = ShopController.IsReady ? ShopController.Instance.activeVendor : null;

        bool stack = item.stackable;

        Set(_equipBtn, false);
        Set(_unequipBtn, false);
        Set(_destroyBtn, false);
        Set(_useBtn, !stack, "Buy");
        Set(_splitBtn, stack, "Buy Amount");

        _useBtn.onClick.RemoveAllListeners();
        _splitBtn.onClick.RemoveAllListeners();

        if (!stack)
        {
            _useBtn.onClick.AddListener(() =>
            {
                // Buy 1 via controller; panel will redraw through controller events.
                ShopController.Instance?.TryBuy(item, 1);
                Hide();
            });
        }
        else
        {
            _splitBtn.onClick.AddListener(() =>
            {
                int clampedMax = Mathf.Max(1, maxQty);

                // If we know the price, clamp by what the player can afford
                var wallet = CurrencyWallet.Instance;
                if (wallet != null && priceEach > 0)
                {
                    int maxAffordable = wallet.gold / priceEach;
                    clampedMax = Mathf.Clamp(maxAffordable, 0, maxQty);
                }

                // If they can’t afford even 1, don’t bother opening the prompt
                if (clampedMax <= 0)
                {
                    Debug.Log("Not enough gold to buy any of this item.");
                    return;
                }

                _quantity.Open(
                    Input.mousePosition,
                    clampedMax,
                    amt =>
                    {
                        ShopController.Instance?.TryBuy(item, amt);
                    },
                    1,
                    Mathf.Clamp(1, 1, clampedMax)
                );

                Hide();
            });
        }

        _followMouse = false;
        OpenAt(screenPos);
    }

    // ---------------- SHOP: SELL ----------------
    public void ShowShopSell(ItemDef item, Vector2 screenPos)
    {
        if (!item) { Hide(); return; }
        if (_title) _title.text = item.displayName;

        bool stack = item.stackable;

        Set(_equipBtn, false);
        Set(_unequipBtn, false);
        Set(_destroyBtn, false);
        Set(_useBtn, !stack, "Sell");
        Set(_splitBtn, stack, "Sell Amount");

        int playerQty = GetPlayerQuantity(item);
        int sellEach = GetSellPriceEach(item);

        _useBtn.onClick.RemoveAllListeners();
        _splitBtn.onClick.RemoveAllListeners();

        if (!stack)
        {
            _useBtn.onClick.AddListener(() =>
            {
                ShopController.Instance?.TrySell(item, 1);
                Hide();
            });
        }
        else
        {
            _splitBtn.onClick.AddListener(() =>
            {
                _quantity.Open(Input.mousePosition, Mathf.Max(1, playerQty), amt =>
                {
                    ShopController.Instance?.TrySell(item, amt);
                }, 1, Mathf.Clamp(1, 1, playerQty));
                Hide();
            });
        }

        _followMouse = false;
        OpenAt(screenPos);
    }

    // ---- helpers ----

    private int GetPlayerQuantity(ItemDef item)
    {
        var inv = PlayerContext.Instance?.facade?.inventory;
        if (inv == null || item == null) return 0;

        var m = inv.GetType().GetMethod("GetCount", new[] { typeof(ItemDef) })
              ?? inv.GetType().GetMethod("Count", new[] { typeof(ItemDef) })
              ?? inv.GetType().GetMethod("GetQuantity", new[] { typeof(ItemDef) });

        if (m != null) return (int)m.Invoke(inv, new object[] { item });

        return 1;
    }

    private int GetSellPriceEach(ItemDef item)
    {
        var ctrl = ShopController.Instance;
        var v = ctrl?.activeVendor;
        if (item == null || v == null) return 0;

        int basePrice = (int)item.quality * 25 + 10;
        float repMult = v.currentRepPriceMult *
                        Mathf.Lerp(1.2f, 0.85f, ReputationSystem.Instance.Normalized);
        int buyEach = v.runtimeInventory.GetPrice(item, basePrice, repMult);
        return Mathf.Max(1, Mathf.RoundToInt(buyEach * 0.5f));
    }

    // ---- positioning (CLAMPED to canvas) ----
    private void PositionAt(Vector2 screenPos)
    {
        if (_rt == null || _canvas == null) return;

        var canvasRT = _canvas.transform as RectTransform;

        // Use the given screenPos (or mouse if follow enabled), then add offset
        var sp = _followMouse ? (Vector2)Input.mousePosition : screenPos;
        sp += _mouseOffset;

        // Convert to canvas-local position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, sp, _canvas.worldCamera, out var local);

        // CLAMP: keep the popup fully inside the canvas rect
        var panelHalf = _rt.rect.size * 0.5f;
        var cRect = canvasRT.rect;

        var min = new Vector2(cRect.xMin + panelHalf.x + _screenPadding.x,
                              cRect.yMin + panelHalf.y + _screenPadding.y);
        var max = new Vector2(cRect.xMax - panelHalf.x - _screenPadding.x,
                              cRect.yMax - panelHalf.y - _screenPadding.y);

        local.x = Mathf.Clamp(local.x, min.x, max.x);
        local.y = Mathf.Clamp(local.y, min.y, max.y);

        _rt.anchoredPosition = local;
    }
}
