using System;

namespace Game.Events
{
    public static class WaveEvents
    {
        public static event Action<int, int> WaveChanged; // (cur, max)
        public static void RaiseWaveChanged(int cur, int max) => WaveChanged?.Invoke(cur, max);
    }
}
