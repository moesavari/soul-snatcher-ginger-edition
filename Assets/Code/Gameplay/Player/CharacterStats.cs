using UnityEngine;
using System;

public class CharacterStats : Stats
{
    [Header("Refs")]
    [SerializeField] private Equipment _equipment;
    [SerializeField] private PlayerController _pc;

    [Header("Base Stats")]
    [SerializeField] private int _baseHealth = 100;
    [SerializeField] private int _baseArmor = 0;
    [SerializeField] private int _baseAttackPower = 10;
    [SerializeField] private int _baseSpellPower = 0;
    [SerializeField] private float _baseAttackSpeed = 1f;
    [SerializeField] private float _baseCritChance = 0f;
    [SerializeField] private float _baseMoveSpeed = 4f;
    [SerializeField] private float _baseCooldownReduction = 0f;

    private int _gearHealth;
    private int _gearArmor;
    private int _gearAttackPower;
    private int _gearSpellPower;
    private float _gearAttackSpeed;
    private float _gearCritChance;
    private float _gearMoveSpeed;
    private float _gearCooldownReduction;

    private int _totalHealth;
    private int _totalArmor;
    private int _totalAttackPower;
    private int _totalSpellPower;
    private float _totalAttackSpeed;
    private float _totalCritChance;
    private float _totalMoveSpeed;
    private float _totalCooldownReduction;

    public override int Health => _totalHealth;
    public override int Armor => _totalArmor;
    public override int AttackPower => _totalAttackPower;
    public override int SpellPower => _totalSpellPower;
    public override float CritChance => _totalCritChance;
    public override float AttackSpeed => _totalAttackSpeed;
    public override float MoveSpeed => _totalMoveSpeed;
    public override float CooldownReduction => _totalCooldownReduction;
    public override int Level => 0;

    public event Action<CharacterStats> OnStatsChanged;

    private void Awake()
    {
        RecalculateStats();
    }

    private void OnEnable()
    {
        if (_equipment != null)
        {
            _equipment.OnEquipmentChanged += RecalculateStats;
            _equipment.OnSlotChanged += (_, __, ___) => RecalculateStats();
        }
        RecalculateStats();
    }

    private void OnDisable()
    {
        if (_equipment != null)
        {
            _equipment.OnEquipmentChanged -= RecalculateStats;
            _equipment.OnSlotChanged -= (_, __, ___) => RecalculateStats();
        }
    }

    public void RecalculateStats()
    {
        // Reset all gear-contributed stats
        _gearHealth = 0;
        _gearArmor = 0;
        _gearAttackPower = 0;
        _gearSpellPower = 0;
        _gearAttackSpeed = 0f;
        _gearCritChance = 0f;
        _gearMoveSpeed = 0f;
        _gearCooldownReduction = 0f;

        if (_equipment != null)
        {
            foreach (EquipmentSlotType slot in Enum.GetValues(typeof(EquipmentSlotType)))
            {
                var item = _equipment.GetEquipped(slot);
                if (item != null && item.stats != null)
                {
                    foreach (var stat in item.stats)
                    {
                        switch (stat.statType)
                        {
                            case StatType.Health:
                                _gearHealth += stat.value; break;
                            case StatType.Armor:
                                _gearArmor += stat.value; break;
                            case StatType.AttackPower:
                                _gearAttackPower += stat.value; break;
                            case StatType.SpellPower:
                                _gearSpellPower += stat.value; break;
                            case StatType.AttackSpeed:
                                _gearAttackSpeed += stat.value; break;
                            case StatType.CritChance:
                                _gearCritChance += stat.value; break;
                            case StatType.MoveSpeed:
                                _gearMoveSpeed += stat.value; break;
                            case StatType.CooldownReduction:
                                _gearCooldownReduction += stat.value; break;
                        }
                    }
                }
            }
        }

        _totalHealth = _baseHealth + _gearHealth;
        _totalArmor = _baseArmor + _gearArmor;
        _totalAttackPower = _baseAttackPower + _gearAttackPower;
        _totalSpellPower = _baseSpellPower + _gearSpellPower;
        _totalAttackSpeed = _baseAttackSpeed + _gearAttackSpeed;
        _totalCritChance = _baseCritChance + _gearCritChance;
        _totalMoveSpeed = _baseMoveSpeed + _gearMoveSpeed;
        _totalCooldownReduction = _baseCooldownReduction + _gearCooldownReduction;
        
        OnStatsChanged?.Invoke(this);
        EquipmentEvents.RaiseStatsChanged(this);
    }

    public override void TakeDamage(int value)
    {
        _pc.TakeDamage(value);
    }
}
