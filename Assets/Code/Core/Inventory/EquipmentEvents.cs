using Game.Core.Inventory;
using System;

public static class EquipmentEvents
{
    public static event Action EquipmentChanged;
    public static void RaiseEquipmentChanged() => EquipmentChanged?.Invoke();

    public static event Action<int, ItemDef> QuickbarChanged;
    public static void RaiseQuickbarChanged(int index, ItemDef item) => QuickbarChanged?.Invoke(index, item);

    public static event Action<CharacterStats> StatsChanged;
    public static void RaiseStatsChanged(CharacterStats stats) => StatsChanged?.Invoke(stats);
}
