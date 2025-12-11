using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.Items
{

    public static class ItemJsonImporter
    {
        private const string ItemDefsFolder = "Assets/Game/Items/Defs";

        #region JSON DTOs (match tools backend)

        [Serializable]
        private class ImportItemStat
        {
            [JsonProperty("id")] public string id;
            [JsonProperty("name")] public string name;
            [JsonProperty("value")] public int value;
        }

        [Serializable]
        private class ImportItemEnchantment
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
        private class ImportItem
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
            [JsonProperty("stats")] public List<ImportItemStat> stats;
            [JsonProperty("enchantments")] public List<ImportItemEnchantment> enchantments;
        }

        #endregion

        [MenuItem("Tools/SoulSnatched/Import Items From Selected JSON")]
        public static void ImportFromSelectedJson()
        {
            var selected = Selection.activeObject as TextAsset;
            if (selected == null)
            {
                Debug.LogError("[ItemImporter] Please select the exported items.json TextAsset in the Project window first.");
                return;
            }

            Import(selected);
        }

        private static void Import(TextAsset itemsJsonAsset)
        {
            if (itemsJsonAsset == null)
            {
                Debug.LogError("[ItemImporter] itemsJsonAsset was null.");
                return;
            }

            List<ImportItem> items;
            try
            {
                items = JsonConvert.DeserializeObject<List<ImportItem>>(itemsJsonAsset.text);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ItemImporter] Failed to parse JSON: {ex.Message}");
                return;
            }

            if (items == null || items.Count == 0)
            {
                Debug.LogWarning("[ItemImporter] Parsed items.json but it contained no items.");
                return;
            }

            EnsureItemDefsFolder();
            NukeExistingItemDefs();

            var createdDefs = new List<ItemDef>();
            foreach (var item in items)
            {
                var def = CreateItemDef(item);
                if (def != null)
                    createdDefs.Add(def);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ItemImporter] Imported {createdDefs.Count} ItemDefs from JSON.");

            TryPopulateRegistry(createdDefs);
        }

        private static void EnsureItemDefsFolder()
        {
            if (AssetDatabase.IsValidFolder(ItemDefsFolder))
                return;

            var segments = ItemDefsFolder.Split('/');
            var path = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                var next = path + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(path, segments[i]);
                path = next;
            }
        }

        private static void NukeExistingItemDefs()
        {
            var guids = AssetDatabase.FindAssets("t:ItemDef", new[] { ItemDefsFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.DeleteAsset(path);
            }

            if (guids.Length > 0)
                Debug.Log($"[ItemImporter] Deleted {guids.Length} existing ItemDef assets.");
        }

        private static ItemDef CreateItemDef(ImportItem item)
        {
            var def = ScriptableObject.CreateInstance<ItemDef>();

            def.itemCode = item.id.ToString();
            def.displayName = item.name;
            def.description = item.description ?? string.Empty;

            def.stackable = item.maxStack > 1;
            def.maxStack = Mathf.Max(1, item.maxStack);
            def.value = item.value;

            def.quality = MapRarityIdToQuality(item.rarityId);

            def.kind = MapItemKind(item.type);
            def.equipSlot = MapEquipSlot(item.type, item.armorSlot);
            def.twoHanded = IsTwoHandedWeapon(item.weaponType);

            def.stats = BuildStats(item.stats);

            ApplyEnchantments(def, item);

            if (!string.IsNullOrWhiteSpace(item.iconId))
            {
                var sprite = Resources.Load<Sprite>($"ItemIcons/{item.iconId}");
                if (sprite == null)
                    Debug.LogWarning($"[ItemImporter] No sprite found in Resources/ItemIcons for icon id '{item.iconId}'.");
                def.icon = sprite;
            }

            var safeName = MakeSafeFileName(item.name, item.id);
            var assetPath = Path.Combine(ItemDefsFolder, safeName + ".asset").Replace("\\", "/");
            AssetDatabase.CreateAsset(def, assetPath);

            return def;
        }

        private static ItemQuality MapRarityIdToQuality(int rarityId)
        {
            return rarityId switch
            {
                1 => ItemQuality.Junk,
                2 => ItemQuality.Common,
                3 => ItemQuality.Rare,
                4 => ItemQuality.Legendary,
                5 => ItemQuality.Legendary,
                _ => ItemQuality.Common
            };
        }

        private static ItemKind MapItemKind(string type)
        {
            return type switch
            {
                "weapon" => ItemKind.Weapon,
                "armor" => ItemKind.Armor,
                "consumable" => ItemKind.Consumable,
                "accessory" => ItemKind.Accessory,
                _ => ItemKind.Other
            };
        }

        private static EquipmentSlotType MapEquipSlot(string type, string armorSlot)
        {
            if (type == "weapon")
                return EquipmentSlotType.MainHand;

            if (type == "armor")
            {
                return armorSlot switch
                {
                    "head" => EquipmentSlotType.Head,
                    "chest" => EquipmentSlotType.Chest,
                    "legs" => EquipmentSlotType.Legs,
                    "boots" => EquipmentSlotType.Boots,
                    "amulet" => EquipmentSlotType.Amulet,
                    _ => EquipmentSlotType.None
                };
            }

            return EquipmentSlotType.None;
        }

        private static bool IsTwoHandedWeapon(string weaponType)
        {
            if (string.IsNullOrWhiteSpace(weaponType))
                return false;

            return weaponType.Contains("2h", StringComparison.OrdinalIgnoreCase);
        }

        private static List<GearStat> BuildStats(List<ImportItemStat> stats)
        {
            var list = new List<GearStat>();
            if (stats == null) return list;

            foreach (var s in stats)
            {
                if (!TryMapStatCode(s.id, out var statType))
                {
                    Debug.LogWarning($"[ItemImporter] Unknown stat code '{s.id}' on item stat.");
                    continue;
                }

                list.Add(new GearStat
                {
                    statType = statType,
                    value = s.value
                });
            }

            return list;
        }

        private static bool TryMapStatCode(string code, out StatType statType)
        {
            switch (code)
            {
                case "health": statType = StatType.Health; return true;
                case "armor": statType = StatType.Armor; return true;
                case "attack_power": statType = StatType.AttackPower; return true;
                case "spell_power": statType = StatType.SpellPower; return true;
                case "attack_speed": statType = StatType.AttackSpeed; return true;
                case "crit_chance": statType = StatType.CritChance; return true;
                case "move_speed": statType = StatType.MoveSpeed; return true;
                case "cdr": statType = StatType.CooldownReduction; return true;
                default:
                    statType = default;
                    return false;
            }
        }

        private static void ApplyEnchantments(ItemDef def, ImportItem item)
        {
            if (item.enchantments == null || item.enchantments.Count == 0)
            {
                def.hasEnchantment = false;
                def.enchantmentDescription = string.Empty;
                return;
            }

            def.hasEnchantment = true;

            if (item.enchantments.Count == 1)
            {
                def.enchantmentDescription = item.enchantments[0].description ?? string.Empty;
            }
            else
            {
                var lines = item.enchantments
                    .Select(e => e.description)
                    .Where(d => !string.IsNullOrWhiteSpace(d));
                def.enchantmentDescription = string.Join("\n", lines);
            }
        }

        private static string MakeSafeFileName(string name, int id)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "Item";

            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());

            return $"{cleaned}_{id:D4}";
        }

        private static void TryPopulateRegistry(List<ItemDef> defs)
        {
            if (defs == null || defs.Count == 0)
                return;

            var registry = UnityEngine.Object.FindObjectOfType<ItemDefRegistry>();
            if (registry == null)
                return;

            var so = new SerializedObject(registry);
            var listProp = so.FindProperty("_itemDefs") ?? so.FindProperty("itemDefs");

            if (listProp == null || !listProp.isArray)
            {
                Debug.LogWarning("[ItemImporter] Found ItemDefRegistry but could not locate an ItemDef list property.");
                return;
            }

            listProp.ClearArray();
            for (int i = 0; i < defs.Count; i++)
            {
                listProp.InsertArrayElementAtIndex(i);
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];
            }

            so.ApplyModifiedProperties();

            Debug.Log($"[ItemImporter] Populated ItemDefRegistry with {defs.Count} entries.");
        }
    }
}
