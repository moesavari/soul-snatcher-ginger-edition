using System.Text;
using UnityEngine;

public static class RewardCalculator
{
    public static NightRewardResult ComputeTierRewards(
        NightRewardProfile profile,
        int nightIndex,
        int villagersAlive)
    {
        var result = new NightRewardResult();

        if (profile == null)
        {
            DebugManager.LogWarning("RewardCalculator: profile is null.", null);
            result.summary = "No reward profile assigned.";
            return result;
        }

        var tier = profile.GetTierForNightIndex(nightIndex);
        if (tier == null)
        {
            result.summary = "No reward tier found.";
            return result;
        }

        result.gold = Random.Range(tier.minGold, tier.maxGold + 1);

        int itemRolls = Random.Range(tier.minItemRolls, tier.maxItemRolls + 1);
        int conRolls = Random.Range(tier.minConsumableRolls, tier.maxConsumableRolls + 1);

        for (int i = 0; i < itemRolls; i++)
        {
            if (tier.itemLootTable != null && tier.itemLootTable.TryRoll(out var item, out var qty))
                result.items.Add((item, qty));
        }

        for (int i = 0; i < conRolls; i++)
        {
            if (tier.consumableLootTable != null && tier.consumableLootTable.TryRoll(out var item, out var qty))
                result.consumables.Add((item, qty));
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Gold earned: {result.gold}");

        if (result.items.Count > 0) sb.AppendLine($"Items: {result.items.Count}");
        if (result.consumables.Count > 0) sb.AppendLine($"Consumables: {result.consumables.Count}");

        sb.AppendLine(villagersAlive <= 0
            ? "No villagers survived. Rewards are… awkward."
            : "Night rewards delivered.");

        result.summary = sb.ToString();
        return result;
    }
}
