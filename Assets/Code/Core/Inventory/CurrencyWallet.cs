using System;
using UnityEngine;

public class CurrencyWallet : MonoSingleton<CurrencyWallet>
{
    [SerializeField] private int _gold;
    public int gold => _gold;

    public event Action<int> OnGoldChanged;

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        _gold += amount;
        OnGoldChanged?.Invoke(_gold);
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0 || _gold < amount) return false;
        _gold -= amount;
        OnGoldChanged?.Invoke(_gold);
        return true;
    }

    public void SetGold(int value)
    {
        _gold = Mathf.Max(0, value);
        OnGoldChanged?.Invoke(_gold);
    }
}
