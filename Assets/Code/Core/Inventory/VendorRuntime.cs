using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Inventory
{
    public class VendorRuntime : MonoBehaviour
    {
        [Header("Vendor Identity")]
        [SerializeField] private int _vendorId = 1;
        [SerializeField] private string _vendorNameOverride;

        [Header("Runtime Stock (read-only for debugging)")]
        [SerializeField] private List<VendorRuntimeItem> _stock = new();

        public int vendorId => _vendorId;
        public IReadOnlyList<VendorRuntimeItem> stock => _stock;

        private void Start()
        {
            BuildStockFromJson();
        }

        private void BuildStockFromJson()
        {
            var db = ItemDatabaseRuntime.Instance;
            var registry = ItemDefRegistry.Instance;

            if (db == null || registry == null)
            {
                Debug.LogWarning("[VendorRuntime] Missing ItemDatabaseRuntime or ItemDefRegistry.");
                return;
            }

            if (!db.TryGetVendor(_vendorId, out var vendorJson))
            {
                Debug.LogWarning($"[VendorRuntime] No vendor JSON found for vendorId={_vendorId}.");
                return;
            }

            var vendorName = string.IsNullOrWhiteSpace(_vendorNameOverride)
                ? vendorJson.name
                : _vendorNameOverride;

            var stockRows = db.GetVendorStock(_vendorId);
            _stock.Clear();

            foreach (var row in stockRows)
            {
                // item_id from JSON -> ItemDef via itemCode we set in the importer
                var itemCode = row.itemId.ToString();
                if (!registry.TryGet(itemCode, out var itemDef))
                {
                    Debug.LogWarning(
                        $"[VendorRuntime] Vendor '{vendorName}' could not find ItemDef for item_id={row.itemId} (itemCode='{itemCode}').");
                    continue;
                }

                // Quantity range
                var quantity = Mathf.Clamp(
                    Random.Range(row.minQuantity, row.maxQuantity + 1),
                    0,
                    999
                );

                if (quantity <= 0) continue;

                // Price: override if provided, else base value * multiplier * (1 + rarity_boost * 10%)
                var basePrice = itemDef.value <= 0 ? 1 : itemDef.value;
                var mult = vendorJson.basePriceMultiplier <= 0f ? 1f : vendorJson.basePriceMultiplier;
                var rarityBoostMult = 1f + (row.rarityBoost * 0.10f);

                var price = row.overridePrice > 0
                    ? row.overridePrice
                    : Mathf.RoundToInt(basePrice * mult * rarityBoostMult);

                _stock.Add(new VendorRuntimeItem(itemDef, quantity, price));
            }

            Debug.Log($"[VendorRuntime] Vendor '{vendorName}' built runtime stock from JSON: {_stock.Count} entries.");

            // ---------------------------------------------------------
            // Bridge: push JSON stock into Vendor.runtimeInventory so
            // ShopController buy/sell logic keeps working.
            // ---------------------------------------------------------
            var vendor = GetComponent<Vendor>();
            if (vendor == null || vendor.runtimeInventory == null)
            {
                Debug.Log($"[VendorRuntime] Vendor '{vendorName}' has no Vendor component or runtimeInventory; JSON stock will be UI-only.");
                return;
            }

            var inv = vendor.runtimeInventory;

            // Wipe old stock and rebuild from JSON
            if (inv.stock != null)
                inv.stock.Clear();

            foreach (var entry in _stock)
            {
                if (entry == null || entry.item == null || entry.quantity <= 0)
                    continue;

                // Use overridePrice so VendorInventory.GetPrice() will respect
                // this value and still apply reputation multipliers.
                inv.Ensure(entry.item, entry.quantity, entry.price);
            }

            Debug.Log(
                $"[VendorRuntime] Synced {_stock.Count} JSON entries into runtimeInventory for vendor '{vendorName}'.");
        }
    }
}
