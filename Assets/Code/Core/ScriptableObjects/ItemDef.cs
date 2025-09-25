using UnityEngine;

[CreateAssetMenu(menuName = "Game/Item", fileName = "ItemDef")]
public class ItemDef : ScriptableObject
{
    [Header("Display")]
    public string displayName;
    public Sprite icon;

    [Header("Inventory")]
    public bool stackable = false;   // <-- required by Inventory/ItemStack
    public int maxStack = 1;         // <-- required by Inventory/ItemStack

    [Header("Classification")]
    public ItemKind kind = ItemKind.Other;
    public EquipmentSlotType equipSlot;

    [Header("Rules")]
    public bool twoHanded = false;

    [Header("Stats")]
    public int baseAttack = 0;
    public int baseArmor = 0;
}
