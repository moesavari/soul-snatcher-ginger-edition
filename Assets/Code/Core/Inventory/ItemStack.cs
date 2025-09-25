using System;
using UnityEngine;

[Serializable]
public struct ItemStack
{
    public ItemDef def;
    [Min(0)] public int amount;

    public bool IsEmpty => def == null || amount <= 0;
    public int MaxStack => def != null ? Mathf.Max(1, def.maxStack) : 1;

    public int AddUpTo(int add)
    {
        if(def == null || add <= 0) return 0;

        int space = MaxStack - amount;
        int moved = Mathf.Clamp(add, 0, space);

        amount += moved;
        return moved;
    }
}
