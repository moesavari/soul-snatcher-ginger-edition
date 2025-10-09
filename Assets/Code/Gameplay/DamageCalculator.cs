using UnityEngine;

public static class DamageCalculator
{
    private const float ArmorK = 100f;
    private const float CritDamage = 2f;

    // Utility for armor mitigation (returns 0..1, where 1 is full mitigation)
    public static float ArmorMitigation(int armor)
    {
        return armor / (armor + ArmorK);
    }

    public static int CalculateDamage(
                        int attackerLevel,
                        int power,
                        int attackStat,
                        int defenderArmor,
                        float critChance)
    {
        float random = Random.Range(0.85f, 1.15f);
        bool isCrit = Random.value < critChance;
        float crit = isCrit ? CritDamage : 1f;
        float baseDamage = (((2f * attackerLevel / 5f + 2f) * power * attackStat / Mathf.Max(1, 1)) / 50f) + 2f;
        float mitigation = 1f - ArmorMitigation(defenderArmor);

        float total = baseDamage * random * crit * mitigation;
        return Mathf.Max(1, Mathf.RoundToInt(total));
    }

}
