using Game.Core.Inventory;
using System;
using UnityEngine;

public static class GameEvents
{
    public static event Action DayStarted;
    public static event Action NightStarted;
    public static event Action AllZombiesCleared;
    public static event Action PlayerDied;
    public static event Action RoundLost;
    public static event Action<GameObject> PlayerSpawned;
    public static event Action<Inventory, ItemStack> ItemAdded;
    public static event Action<Inventory, ItemStack> ItemRemoved;

    public static void RaiseDay()                                       => DayStarted?.Invoke();
    public static void RaiseNight()                                     => NightStarted?.Invoke();
    public static void RaiseAllZombiesCleared()                         => AllZombiesCleared?.Invoke();
    public static void RaisePlayerDied()                                => PlayerDied?.Invoke();
    public static void RaiseRoundLost()                                 => RoundLost?.Invoke();
    public static void RaisePlayerSpawned(GameObject go)                => PlayerSpawned?.Invoke(go);
    public static void RaiseItemAdded(Inventory inv, ItemStack stack)   => ItemAdded?.Invoke(inv, stack);
    public static void RaiseItemRemoved(Inventory inv, ItemStack stack) => ItemRemoved?.Invoke(inv, stack);
}
