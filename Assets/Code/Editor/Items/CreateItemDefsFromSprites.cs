#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CreateItemDefsFromSprites
{
    private const string OUTPUT_DIR = "Assets/Game/Items/Defs";

    [MenuItem("Tools/Items/Create ItemDefs From Selected Sprites")]
    public static void CreateDefsFromSelection()
    {
        var sprites = Selection.objects
            .SelectMany(obj =>
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) return Enumerable.Empty<Sprite>();
                var all = AssetDatabase.LoadAllAssetsAtPath(path);
                return all.OfType<Sprite>();
            })
            .Distinct()
            .ToList();

        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog("Create ItemDefs", "Select one or more textures/sprites in the Project window.", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder(OUTPUT_DIR))
        {
            Directory.CreateDirectory(OUTPUT_DIR);
            AssetDatabase.Refresh();
        }

        int created = 0, skipped = 0;
        foreach (var sp in sprites)
        {
            var cleanName = Clean(sp.name);
            var assetPath = $"{OUTPUT_DIR}/{cleanName}.asset";
            if (AssetDatabase.LoadAssetAtPath<ItemDef>(assetPath) != null)
            {
                skipped++;
                continue;
            }

            var def = ScriptableObject.CreateInstance<ItemDef>();
            def.displayName = ToDisplayName(cleanName);
            def.icon = sp;

            // Guess fields from filename/path
            Guess(def, sp);

            AssetDatabase.CreateAsset(def, assetPath);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CreateItemDefs] Created {created} ItemDefs. Skipped {skipped} (already existed). Output: {OUTPUT_DIR}");
    }

    private static string Clean(string n) => n.Replace(' ', '_').Replace("__", "_");

    private static string ToDisplayName(string n)
    {
        var s = n.Replace('_', ' ').Trim();
        return char.ToUpper(s[0]) + s.Substring(1);
    }

    // Simple keyword-based guesser. Tweak to your naming.
    private static void Guess(ItemDef def, Sprite sp)
    {
        var path = AssetDatabase.GetAssetPath(sp).ToLowerInvariant();
        var name = (sp.name ?? "").ToLowerInvariant();
        string s = path + " " + name;

        // Consumables first (stackables)
        if (HasAny(s, "potion", "elixir", "vial", "flask", "food", "meat", "bread", "apple", "bandage"))
        {
            def.kind = ItemKind.Consumable;
            def.stackable = true;
            def.maxStack = 10;
            def.equipSlot = EquipmentSlotType.None;
            return;
        }

        // Ammo/misc stackables
        if (HasAny(s, "arrow", "bolt", "bomb", "shuriken"))
        {
            def.kind = ItemKind.Consumable;
            def.stackable = true;
            def.maxStack = 99;
            def.equipSlot = EquipmentSlotType.None;
            return;
        }

        // Weapons
        if (HasAny(s, "sword", "blade", "katana", "sabre", "saber"))
        {
            def.kind = ItemKind.Weapon;
            def.equipSlot = EquipmentSlotType.MainHand;
            def.baseAttack = 4;
            return;
        }
        if (HasAny(s, "axe", "greataxe", "battleaxe"))
        {
            def.kind = ItemKind.Weapon;
            def.equipSlot = EquipmentSlotType.MainHand;
            def.twoHanded = HasAny(s, "great", "twohand", "2h");
            def.baseAttack = def.twoHanded ? 7 : 5;
            return;
        }
        if (HasAny(s, "mace", "hammer", "club"))
        {
            def.kind = ItemKind.Weapon;
            def.equipSlot = EquipmentSlotType.MainHand;
            def.baseAttack = 5;
            return;
        }
        if (HasAny(s, "bow", "longbow"))
        {
            def.kind = ItemKind.Weapon;
            def.equipSlot = EquipmentSlotType.MainHand;
            def.twoHanded = true;
            def.baseAttack = 6;
            return;
        }
        if (HasAny(s, "crossbow"))
        {
            def.kind = ItemKind.Weapon;
            def.equipSlot = EquipmentSlotType.MainHand;
            def.twoHanded = true;
            def.baseAttack = 7;
            return;
        }
        if (HasAny(s, "staff", "scepter"))
        {
            def.kind = ItemKind.Weapon;
            def.equipSlot = EquipmentSlotType.MainHand;
            def.twoHanded = HasAny(s, "great", "twohand", "2h");
            def.baseAttack = def.twoHanded ? 6 : 4;
            return;
        }
        if (HasAny(s, "shield", "buckler", "kite"))
        {
            def.kind = ItemKind.Weapon; // or Accessory if shields are special
            def.equipSlot = EquipmentSlotType.Offhand;
            def.baseArmor = 3;
            return;
        }

        // Armor
        if (HasAny(s, "helm", "helmet", "hood", "cap"))
        {
            def.kind = ItemKind.Armor;
            def.equipSlot = EquipmentSlotType.Head;
            def.baseArmor = 2;
            return;
        }
        if (HasAny(s, "armor", "chest", "breastplate", "tunic", "robe"))
        {
            def.kind = ItemKind.Armor;
            def.equipSlot = EquipmentSlotType.Chest;
            def.baseArmor = 4;
            return;
        }
        if (HasAny(s, "leg", "pants", "greaves"))
        {
            def.kind = ItemKind.Armor;
            def.equipSlot = EquipmentSlotType.Legs;
            def.baseArmor = 3;
            return;
        }
        if (HasAny(s, "boot", "shoe", "sandal"))
        {
            def.kind = ItemKind.Armor;
            def.equipSlot = EquipmentSlotType.Boots;
            def.baseArmor = 2;
            return;
        }

        // Accessories
        if (HasAny(s, "amulet", "necklace", "talisman"))
        {
            def.kind = ItemKind.Accessory;
            def.equipSlot = EquipmentSlotType.Amulet;
            return;
        }
        if (HasAny(s, "relic", "artifact", "idol", "charm"))
        {
            def.kind = ItemKind.Accessory;
            def.equipSlot = EquipmentSlotType.Relic;
            return;
        }

        // Fallback
        def.kind = ItemKind.Other;
        def.equipSlot = EquipmentSlotType.None;
    }

    private static bool HasAny(string hay, params string[] needles)
        => needles.Any(hay.Contains);
}
#endif
