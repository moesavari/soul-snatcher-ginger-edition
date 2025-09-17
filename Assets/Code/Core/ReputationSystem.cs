using System;
using UnityEngine;

public class ReputationSystem : MonoSingleton<ReputationSystem>
{
    [SerializeField] private int _reputation;
    public int reputation => _reputation;
    
    private int _negativeNights;
    
    public event Action<int> OnReputationChanged;
    public event Action OnThrownOut;

    private const int max_negative_nights = 2;
    private const int max_positive_rep = 50;
    private const int max_negative_rep = -50;

    protected override void Awake()
    {
        base.Awake();

        OnReputationChanged?.Invoke(_reputation);
    }

    public void AddReputation(int amount)
    {
        if (amount == 0) return;

        _reputation = Mathf.Clamp(_reputation + amount, max_negative_rep, max_positive_rep);
        OnReputationChanged?.Invoke(_reputation);

        CheckNegativeConsequence();
    }

    public void SetReputation(int amount)
    {
        _reputation = Mathf.Clamp(amount, max_negative_rep, max_positive_rep);
        OnReputationChanged?.Invoke(_reputation);

        CheckNegativeConsequence();
    }

    private void CheckNegativeConsequence()
    {
        if (_reputation > max_negative_rep) _negativeNights = 0;
        else
        {
            _negativeNights++;
            if (_negativeNights >= max_negative_nights) OnThrownOut?.Invoke();
        }
    }
}
