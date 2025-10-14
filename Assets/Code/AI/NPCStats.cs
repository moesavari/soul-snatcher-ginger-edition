using UnityEngine;
using System;

public enum NPCType
{
    Zombie,
    Villager
}

public class NPCStats : Stats
{
    [SerializeField] private NPCType type;

    [Header("Base Stats")]
    [SerializeField] private int _baseHealth = 50;
    [SerializeField] private int _baseArmor = 5;
    [SerializeField] private int _baseAttackPower = 8;
    [SerializeField] private int _baseSpellPower = 0;
    [SerializeField] private float _baseAttackSpeed = 1f;
    [SerializeField] private float _baseCritChance = 0.05f;
    [SerializeField] private float _baseMoveSpeed = 3.5f;

    // Expose stat properties (for reference & calculation)
    public override int Health => _baseHealth;
    public override int Armor => _baseArmor;
    public override int AttackPower => _baseAttackPower;
    public override int SpellPower => _baseSpellPower;
    public override float CritChance => _baseSpellPower;
    public override float AttackSpeed => _baseAttackSpeed;
    public override float MoveSpeed => _baseMoveSpeed;
    public override float CooldownReduction => 0f;
    public override int Level => 1;

    public event Action<NPCStats> OnStatsChanged;
    public event Action OnDeath;

    // For any future stat mod, use setters and fire event
    public void SetBaseHealth(int hp) { _baseHealth = hp; OnStatsChanged?.Invoke(this); }
    public void SetArmor(int arm) { _baseArmor = arm; OnStatsChanged?.Invoke(this); }
    public void SetAttackPower(int atk) { _baseAttackPower = atk; OnStatsChanged?.Invoke(this); }
    public void SetAttackSpeed(float spd) { _baseAttackSpeed = spd; OnStatsChanged?.Invoke(this); }
    public void SetMoveSpeed(float spd) { _baseMoveSpeed = spd; OnStatsChanged?.Invoke(this); }
    public void SetCritChance(float crit) { _baseCritChance = crit; OnStatsChanged?.Invoke(this); }
    public void SetSpellPower(int power) { _baseSpellPower = power; OnStatsChanged?.Invoke(this); }

    // Initialization or level scaling
    public void Initialize(int hp, int armor, int atk, float moveSpd)
    {
        _baseHealth = hp;
        _baseArmor = armor;
        _baseAttackPower = atk;
        _baseMoveSpeed = moveSpd;
        OnStatsChanged?.Invoke(this);
    }

    public override void TakeDamage(int value)
    {
        switch (type)
        {
            case NPCType.Zombie:
                GetComponent<Zombie>().TakeDamage(value); break;
            case NPCType.Villager:
                break;
            default:
                break;
        }
    }

    public void RaiseDeath()
    {
        OnDeath?.Invoke();
    }
}
