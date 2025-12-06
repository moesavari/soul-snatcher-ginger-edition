using UnityEngine;

public abstract class Stats : MonoBehaviour
{
    public abstract int Health { get; }
    public abstract int Armor { get; }
    public abstract int AttackPower { get; }
    public abstract int SpellPower { get; }
    public abstract float CritChance { get; }
    public abstract float AttackSpeed { get; }
    public abstract float MoveSpeed { get; }
    public abstract float CooldownReduction { get; }
    public abstract int Level { get; }

    public abstract void TakeDamage(int value);
}
