using System;

public static class HealthEvents
{
    public static event Action<int, int> HealthChanged;
    public static event Action Died;

    public static void RaiseHealthChanged(int current, int max) => HealthChanged?.Invoke(current, max);
    public static void RaiseDied() => Died?.Invoke();
}
