using Game.Core.Inventory;
using Game.Systems;
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class CharacterStats : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Equipment _equipment;
    [SerializeField] private ReputationSystem _reputationSystem; // Manager, singleton

    [Header("Computed")]
    [SerializeField] private int _attack;
    [SerializeField] private int _armor;

    public int attack => _attack;
    public int armor => _armor;

    public event Action<CharacterStats> OnStatsChanged;

    private void OnEnable()
    {
        if (_equipment != null)
        {
            _equipment.OnEquipmentChanged += Recalculate;
            _equipment.OnSlotChanged += (_, __, ___) => Recalculate();
        }
        if (_reputationSystem == null) _reputationSystem = ReputationSystem.Instance;
        if (_reputationSystem != null) _reputationSystem.OnReputationChanged += _ => Recalculate();
        Recalculate();
    }

    private void OnDisable()
    {
        if (_equipment != null)
        {
            _equipment.OnEquipmentChanged -= Recalculate;
            _equipment.OnSlotChanged -= (_, __, ___) => Recalculate();
        }
        if (_reputationSystem != null) _reputationSystem.OnReputationChanged -= _ => Recalculate();
    }

    public void Recalculate()
    {
        int baseAtk = 0;
        int baseArm = 0;

        Sum(_equipment?.GetEquipped(EquipmentSlotType.MainHand), ref baseAtk, ref baseArm);
        Sum(_equipment?.GetEquipped(EquipmentSlotType.Offhand), ref baseAtk, ref baseArm);
        Sum(_equipment?.GetEquipped(EquipmentSlotType.Head), ref baseAtk, ref baseArm);
        Sum(_equipment?.GetEquipped(EquipmentSlotType.Chest), ref baseAtk, ref baseArm);
        Sum(_equipment?.GetEquipped(EquipmentSlotType.Legs), ref baseAtk, ref baseArm);
        Sum(_equipment?.GetEquipped(EquipmentSlotType.Boots), ref baseAtk, ref baseArm);
        Sum(_equipment?.GetEquipped(EquipmentSlotType.Amulet), ref baseAtk, ref baseArm);
        Sum(_equipment?.GetEquipped(EquipmentSlotType.Relic), ref baseAtk, ref baseArm);

        var rep = _reputationSystem != null ? _reputationSystem.reputation : 0;
        var mult = rep switch
        {
            >= 40 => 1.30f,
            >= 25 => 1.20f,
            >= 10 => 1.10f,
            >= 0 => 1.00f,
            >= -15 => 0.95f,
            _ => WarnAndReturn(0.90f)
        };

        _attack = Mathf.RoundToInt(baseAtk * mult);
        _armor = Mathf.RoundToInt(baseArm * mult);

        OnStatsChanged?.Invoke(this);
        EquipmentEvents.RaiseStatsChanged(this);

        float WarnAndReturn(float v)
        {
            Debug.LogWarning("[CharacterStats] Very low reputation; applying penalty.");
            return v;
        }
    }

    private static void Sum(ItemDef def, ref int atk, ref int arm)
    {
        if (def == null) return;
        atk += Mathf.Max(0, def.baseAttack);
        arm += Mathf.Max(0, def.baseArmor);
    }
}
