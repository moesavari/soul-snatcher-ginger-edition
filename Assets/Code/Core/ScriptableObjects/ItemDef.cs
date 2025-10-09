using UnityEngine;
using System.Collections.Generic;

public enum ItemQuality
{
    Junk,
    Common,
    Rare,
    Legendary
}

public enum StatType
{
    Health,
    Armor,
    AttackPower,
    SpellPower,
    AttackSpeed,
    CritChance,
    MoveSpeed,
    CooldownReduction
}


[System.Serializable]
public class GearStat
{
    public StatType statType;
    public int value;
}

[CreateAssetMenu(menuName = "Game/Item", fileName = "ItemDef")]
public class ItemDef : ScriptableObject
{
    [Header("Display")]
    public string displayName;
    public Sprite icon;
    public string description;

    [Header("Inventory")]
    public bool stackable = false;
    public int maxStack = 1;

    [Header("Classification")]
    public ItemKind kind = ItemKind.Other;
    public EquipmentSlotType equipSlot;

    [Header("Rules")]
    public bool twoHanded = false;

    [Header("Gear System")]
    [SerializeField] private ItemQuality _quality;
    [SerializeField] private List<GearStat> _stats = new List<GearStat>();
    [SerializeField] private bool _hasEnchantment;
    [SerializeField] private string _enchantmentDescription;

    public ItemQuality quality { get => _quality; set => _quality = value; }
    public List<GearStat> stats { get => _stats; set => _stats = value; }
    public bool hasEnchantment { get => _hasEnchantment; set => _hasEnchantment = value; }
    public string enchantmentDescription { get => _enchantmentDescription; set => _enchantmentDescription = value; }

}
