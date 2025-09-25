using Game.Core.Inventory;
using UnityEngine;

public static class InventoryService
{
    public static int Transfer(Inventory from, Inventory to, ItemDef def, int amount)
    {
        if (!from || !to || def == null || amount <= 0) return 0;

        int toMove = Mathf.Min(amount, Count(from, def));
        if(toMove == 0) return 0;

        from.TryRemove(def, toMove);
        to.TryAdd(def, toMove, out int leftover);

        if (leftover > 0) from.TryAdd(def, leftover, out _);
        return toMove = leftover;
    }

    public static int Count(Inventory inv, ItemDef def)
    {
        if(!inv || def == null) return 0;
        
        int c = 0;
        foreach (var s in inv.contents)
            if (s.def == def)
                c += s.amount;
        
        return c;
    }
}
