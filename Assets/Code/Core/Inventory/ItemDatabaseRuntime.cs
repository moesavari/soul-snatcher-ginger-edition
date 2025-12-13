using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Core.Inventory
{
    [Serializable]
    public class ItemJsonStat
    {
        [JsonProperty("code")] public string code;
        [JsonProperty("min_value")] public int minValue;
        [JsonProperty("max_value")] public int maxValue;
    }

    [Serializable]
    public class ItemJsonEnchantment
    {
        [JsonProperty("id")] public int id;
        [JsonProperty("name")] public string name;
        [JsonProperty("description")] public string description;
        [JsonProperty("kind")] public string kind;
        [JsonProperty("affected_stat_code")] public string affectedStatCode;
        [JsonProperty("min_value")] public int minValue;
        [JsonProperty("max_value")] public int maxValue;
    }

    [Serializable]
    public class ItemJson
    {
        [JsonProperty("id")] public int id;
        [JsonProperty("name")] public string name;
        [JsonProperty("description")] public string description;
        [JsonProperty("rarity_id")] public int rarityId;
        [JsonProperty("type")] public string type;
        [JsonProperty("armor_slot")] public string armorSlot;
        [JsonProperty("weapon_type")] public string weaponType;
        [JsonProperty("value")] public int value;
        [JsonProperty("max_stack")] public int maxStack;
        [JsonProperty("icon_id")] public string iconId;
        [JsonProperty("base_item_id")] public int? baseItemId;
        [JsonProperty("stats")] public List<ItemJsonStat> stats;
        [JsonProperty("enchantments")] public List<ItemJsonEnchantment> enchantments;
    }

    [Serializable]
    public class VendorJson
    {
        [JsonProperty("id")] public int id;
        [JsonProperty("name")] public string name;
        [JsonProperty("base_price_multiplier")] public float basePriceMultiplier;
    }

    [Serializable]
    public class VendorStockJson
    {
        [JsonProperty("vendor_id")] public int vendorId;
        [JsonProperty("item_id")] public int itemId;
        [JsonProperty("min_quantity")] public int minQuantity;
        [JsonProperty("max_quantity")] public int maxQuantity;
        [JsonProperty("rep_min")] public int repMin;
        [JsonProperty("rep_max")] public int repMax;
        [JsonProperty("rarity_boost")] public int rarityBoost;
        [JsonProperty("override_price")] public int overridePrice;
    }

    public class ItemDatabaseRuntime : MonoSingleton<ItemDatabaseRuntime>
    {
        [Header("JSON Sources (TextAssets from Resources or direct refs)")]
        [SerializeField] private TextAsset _itemsJson;
        [SerializeField] private TextAsset _vendorsJson;
        [SerializeField] private TextAsset _vendorStockJson;

        private Dictionary<int, ItemJson> _itemsById = new();
        private Dictionary<int, VendorJson> _vendorsById = new();
        private Dictionary<int, List<VendorStockJson>> _stockByVendorId = new();

        [SerializeField] private bool _logSyncDetails = false;

        private Dictionary<string, ItemJson> _itemsByIconId = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<int, ItemJson> itemsById => _itemsById;
        public IReadOnlyDictionary<int, VendorJson> vendorsById => _vendorsById;
        public IReadOnlyDictionary<int, List<VendorStockJson>> stockByVendorId => _stockByVendorId;

        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            LoadAll();

            DebugManager.Log($"[ItemDB] Items loaded: {_itemsById.Count}, " +
                      $"vendors: {_vendorsById.Count}, " +
                      $"vendor stock groups: {_stockByVendorId.Count}", this);
        }

        private void LoadAll()
        {
            LoadItems();
            LoadVendors();
            LoadVendorStock();
        }

        private void LoadItems()
        {
            var json = GetJson(_itemsJson, "ItemData/items");
            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                var list = JsonConvert.DeserializeObject<List<ItemJson>>(json);
                _itemsById = new Dictionary<int, ItemJson>();
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        if (!_itemsById.ContainsKey(item.id))
                            _itemsById[item.id] = item;
                        else
                            DebugManager.LogWarning($"[ItemDB] Duplicate item id {item.id} in JSON.", this);

                        if (!string.IsNullOrWhiteSpace(item.iconId))
                        {
                            _itemsByIconId[item.iconId] = item;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugManager.LogError($"[ItemDB] Failed to parse items JSON: {ex.Message}", this);
            }
        }

        private void LoadVendors()
        {
            var json = GetJson(_vendorsJson, "ItemData/vendors");
            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                var list = JsonConvert.DeserializeObject<List<VendorJson>>(json);
                _vendorsById = new Dictionary<int, VendorJson>();
                if (list != null)
                {
                    foreach (var v in list)
                    {
                        if (!_vendorsById.ContainsKey(v.id))
                            _vendorsById[v.id] = v;
                        else
                            DebugManager.LogWarning($"[ItemDB] Duplicate vendor id {v.id} in JSON.", this);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugManager.LogError($"[ItemDB] Failed to parse vendors JSON: {ex.Message}", this);
            }
        }

        private void LoadVendorStock()
        {
            var json = GetJson(_vendorStockJson, "ItemData/vendor_stock");
            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                var list = JsonConvert.DeserializeObject<List<VendorStockJson>>(json);
                _stockByVendorId = new Dictionary<int, List<VendorStockJson>>();
                if (list != null)
                {
                    foreach (var s in list)
                    {
                        if (!_stockByVendorId.TryGetValue(s.vendorId, out var bucket))
                        {
                            bucket = new List<VendorStockJson>();
                            _stockByVendorId[s.vendorId] = bucket;
                        }
                        bucket.Add(s);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugManager.LogError($"[ItemDB] Failed to parse vendor_stock JSON: {ex.Message}", this);
            }
        }

        private string GetJson(TextAsset asset, string fallbackResourcePath)
        {
            if (asset != null) return asset.text;

            var fromResources = Resources.Load<TextAsset>(fallbackResourcePath);
            if (fromResources != null) return fromResources.text;

            DebugManager.LogWarning($"[ItemDB] No JSON TextAsset assigned and nothing found at Resources/{fallbackResourcePath}.", this);
            return null;
        }

        public bool TryGetItem(int id, out ItemJson item)
        {
            return _itemsById.TryGetValue(id, out item);
        }

        public bool TryGetVendor(int id, out VendorJson vendor)
        {
            return _vendorsById.TryGetValue(id, out vendor);
        }

        public IReadOnlyList<VendorStockJson> GetVendorStock(int vendorId)
        {
            return _stockByVendorId.TryGetValue(vendorId, out var list) ? list : Array.Empty<VendorStockJson>();
        }

        private void SyncJsonIntoItemDefs()
        {
            var registry = ItemDefRegistry.Instance;
            if (registry == null)
            {
                DebugManager.LogWarning("[ItemDB] No ItemDefRegistry found, cannot sync ItemDefs.", this);
                return;
            }

            int synced = 0;

            foreach (var def in registry.allDefs)
            {
                if (def == null) continue;

                ItemJson json = null;

                if (!string.IsNullOrWhiteSpace(def.itemCode) &&
                    _itemsByIconId.TryGetValue(def.itemCode, out json))
                {

                }
                else
                {

                    if (!_itemsByIconId.TryGetValue(def.name, out json))
                    {
                        if (_logSyncDetails)
                            DebugManager.LogWarning($"[ItemDB] No JSON item found for ItemDef '{def.name}' (itemCode='{def.itemCode}').", this);
                        continue;
                    }
                }

                ApplyItemJsonToDef(def, json);
                synced++;
            }

            DebugManager.Log($"[ItemDB] Synced {synced} ItemDefs with JSON data.", this);
        }

        private void ApplyItemJsonToDef(ItemDef def, ItemJson data)
        {
            def.stackable = data.maxStack > 1;
            def.maxStack = Mathf.Max(1, data.maxStack);

            def.quality = MapRarity(data.rarityId);

            var statList = new List<GearStat>();

            if (data.stats != null)
            {
                foreach (var s in data.stats)
                {
                    var type = MapStatCode(s.code);
                    if (type == null)
                    {
                        if (_logSyncDetails)
                            DebugManager.LogWarning($"[ItemDB] Unknown stat code '{s.code}' for '{data.name}'.", this);
                        continue;
                    }

                    statList.Add(new GearStat
                    {
                        statType = type.Value,
                        value = s.minValue
                    });
                }
            }

            def.stats = statList;

            if (data.enchantments != null && data.enchantments.Count > 0)
            {
                var e = data.enchantments[0];
                def.hasEnchantment = true;
                def.enchantmentDescription = e.description ?? e.name;
            }
            else
            {
                def.hasEnchantment = false;
                def.enchantmentDescription = string.Empty;
            }
        }
        private ItemQuality MapRarity(int rarity)
        {
            return rarity switch
            {
                1 => ItemQuality.Junk,
                2 => ItemQuality.Common,
                3 => ItemQuality.Rare,
                4 => ItemQuality.Legendary,
                5 => ItemQuality.Cursed,
                _ => ItemQuality.Common
            };
        }

        private StatType? MapStatCode(string code)
        {
            return code switch
            {
                "health" => StatType.Health,
                "armor" => StatType.Armor,
                "attack_power" => StatType.AttackPower,
                "move_speed" => StatType.MoveSpeed,
                _ => null
            };
        }
    }
}
