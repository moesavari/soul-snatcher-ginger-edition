using System;
using UnityEngine;

public class SoulSystem : MonoSingleton<SoulSystem>
{
    [SerializeField] private int _souls;
    public int souls => _souls;

    public event Action<int> OnSoulsChanged;

    public void AddSouls(int amount)
    {
        if (amount <= 0) return;

        _souls += amount;
        OnSoulsChanged?.Invoke(_souls);
    }

    public bool SpendSouls(int amount)
    {
        if (amount <= 0 || _souls < amount) return false;

        _souls -= amount;
        OnSoulsChanged?.Invoke(_souls);

        return true;
    }

    public void SetSouls(int value)
    {
        _souls = Mathf.Max(0, value);
        OnSoulsChanged?.Invoke(_souls);
    }
}
