using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Vendor Inventory")]
public class VendorInventory : ScriptableObject
{
    [Serializable]
    public class StockEntry
    {
        public ItemDef item;
        [Min(0)] public int quantity = 1;
        [Min(-1)] public int overridePrice = -1;
    }

    public string vendorName = "Shop";
    [Range(0.1f, 5f)] public float priceMultiplier = 1f;
    public List<StockEntry> stock = new List<StockEntry>();

    public event Action OnStockChanged;

    public int GetPrice(ItemDef def, int basePrice, float repMultiplier = 1f)
    {
        int each = basePrice;

        for (int i = 0; i < stock.Count; i++)
        {
            var s = stock[i];
            if (s.item == def)
            {
                int p =(s.overridePrice >= 0) ? s.overridePrice : Mathf.RoundToInt(basePrice * priceMultiplier);
                return Mathf.RoundToInt(p * repMultiplier);
            }
        }

        return Mathf.RoundToInt(each * priceMultiplier * repMultiplier);
    }

    public bool RemoveOne(ItemDef def)
    {
        for (int i = 0; i < stock.Count; i++)
        {
            if (stock[i].item == def)
            {
                var entry = stock[i];
                entry.quantity--;

                if (entry.quantity <= 0)
                    stock.RemoveAt(i);
                else
                    stock[i] = entry;

                OnStockChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    public void AddOne(ItemDef def)
    {
        for (int i = 0; i < stock.Count; i++)
        {
            if (stock[i].item == def)
            {
                stock[i].quantity++;
                OnStockChanged?.Invoke();
                return;
            }
        }
        stock.Add(new StockEntry { item = def, quantity = 1, overridePrice = -1 });
        OnStockChanged?.Invoke();
    }

    public int GetQuantity(ItemDef def)
    {
        foreach (var s in stock)
            if (s.item == def)
            {
                return Mathf.Max(0, s.quantity);
            }
        return 0;
    }

    public void Ensure(ItemDef item, int qtyIfMissing = 1, int overridePrice = -1)
    {
        for (int i = 0; i < stock.Count; i++)
        {
            if (stock[i].item == item)
            {
                if (stock[i].quantity <= 0) stock[i].quantity = qtyIfMissing;
                if(overridePrice >= 0) stock[i].overridePrice = overridePrice;
                return;
            }
        }

        stock.Add(new StockEntry { item = item, quantity = qtyIfMissing, overridePrice = overridePrice });
    }

    public void RemoveAll(ItemDef item)
    {
        for (int i = stock.Count - 1; i >= 0; i--)
        {
            if(stock[i].item == item) stock.RemoveAt(i);
        }
    }

    public void SetOverridePrice(ItemDef item, int price)
    {
        for (int i = 0; i < stock.Count; i++)
        {
            if (stock[i].item == item) { stock[i].overridePrice = price; return; }
        }
    }
}
