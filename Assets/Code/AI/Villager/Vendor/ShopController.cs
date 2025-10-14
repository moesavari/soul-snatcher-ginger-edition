using Game.Systems;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopController : MonoSingleton<ShopController>
{
    [SerializeField] private GameObject _shopRoot;
    [SerializeField] private int _buybackCapacity = 10;

    private Vendor _activeVendor;
    public Vendor activeVendor => _activeVendor;

    private readonly Queue<ItemDef> _buybackItems = new();

    public event Action<Vendor> OnShopOpened;
    public event Action OnShopClosed;
    public event Action OnChanged;

    protected override void Awake()
    {
        base.Awake();
        if(_shopRoot) _shopRoot.SetActive(false);
    }

    public void Open(Vendor v)
    {
        if(!v || v.inventory == null) return;

        _activeVendor = v;

        if (_shopRoot) _shopRoot.SetActive(true);
        InputManager.PushLock();
        OnShopOpened?.Invoke(v);
    }

    public void Close()
    {
        _activeVendor = null;

        if (_shopRoot) _shopRoot.SetActive(false);
        InputManager.PopLock();
        OnShopClosed?.Invoke();
    }

    private int GetBasePrice(ItemDef item)
    {
        return item ? (int)item.quality * 25 + 10 : 0;
    }

    private float GetGlobalRepMultiplier()
    {
        float t = ReputationSystem.Instance.Normalized;
        return Mathf.Lerp(1.2f, 0.85f, t);
    }

    public bool TryBuy(ItemDef item, int qty = 1)
    {
        if(_activeVendor == null || item == null || qty <= 0) return false;

        var inv = PlayerContext.Instance?.facade?.inventory;
        if(inv == null) return false;

        float vendorRepMult = _activeVendor.currentRepPriceMult;
        float globalRepMult = GetGlobalRepMultiplier();
        float totalMult = vendorRepMult * globalRepMult;

        int each = _activeVendor.runtimeInventory.GetPrice(item, GetBasePrice(item), totalMult);
        int total = each * qty;

        if(!CurrencyWallet.Instance.SpendGold(total)) return false;

        for (int i = 0; i < qty; i++)
        {
            if (!_activeVendor.runtimeInventory.RemoveOne(item))
            {
                CurrencyWallet.Instance.AddGold(each * (qty - i));
                return false;
            }
            
            inv.TryAdd(item, 1, out _);
        }

        OnChanged?.Invoke();
        return true;
    }

    public bool TrySell(ItemDef item, int qty = 1, float sellFactor = 0.5f)
    {
        if(_activeVendor == null || item == null || qty <= 0) return false;

        var inv = PlayerContext.Instance?.facade?.inventory;
        if(inv == null) return false;

        if(!inv.Has(item, qty)) return false;
        if (!inv.TryRemove(item, qty)) return false;

        float vendorRepMult = _activeVendor.currentRepPriceMult;
        float globalRepMult = GetGlobalRepMultiplier();
        float totalMult = vendorRepMult * globalRepMult;

        int eachBuy = _activeVendor.runtimeInventory.GetPrice(item, GetBasePrice(item), totalMult);
        int eachSell = Mathf.Max(1, Mathf.RoundToInt(eachBuy * Mathf.Clamp01(sellFactor)));
        CurrencyWallet.Instance.AddGold(eachSell * qty);

        for (int i = 0; i < qty; i++)
        {
            _activeVendor.runtimeInventory.AddOne(item);
            _buybackItems.Enqueue(item);
            if(_buybackItems.Count > _buybackCapacity) _buybackItems.Dequeue();
        }

        OnChanged?.Invoke();

        return true;
    }

    public bool TryBuyBack(ItemDef item)
    {
        if(!_buybackItems.Contains(item)) return false;

        float m = _activeVendor.currentRepPriceMult * GetGlobalRepMultiplier();
        int priceEach = Mathf.RoundToInt(_activeVendor.runtimeInventory.GetPrice(item, GetBasePrice(item), m) * 0.75f);
        if(!CurrencyWallet.Instance.SpendGold(priceEach)) return false;

        PlayerContext.Instance.facade.inventory.TryAdd(item, 1, out _);
        _activeVendor.runtimeInventory.RemoveOne(item);

        var list = new List<ItemDef>(_buybackItems);
        list.Remove(item);
        
        _buybackItems.Clear();
        foreach (var it in list) _buybackItems.Enqueue(it);

        OnChanged?.Invoke();
        return true;
    }
}
