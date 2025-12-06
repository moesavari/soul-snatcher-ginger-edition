using System;

namespace Game.Events
{
    public static class CombatEvents
    {
        public static event Action UsedMelee;
        public static event Action UsedRanged;
        public static void RaiseUsedMelee() => UsedMelee?.Invoke();
        public static void RaiseUsedRanged() => UsedRanged?.Invoke();
    }
}
