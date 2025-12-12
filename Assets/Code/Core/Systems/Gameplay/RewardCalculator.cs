using UnityEngine;

public static class RewardCalculator
{
    public readonly struct NightRewards
    {
        public readonly int gold;
        public readonly string summary;

        public NightRewards(int gold, string summary)
        {
            this.gold = gold;
            this.summary = summary;
        }
    }

    /// <summary>
    /// Computes end-of-night rewards (gold + flavour text).
    /// Souls / reputation are handled live elsewhere – this just
    /// decides what kind of payout the player should get AND applies
    /// the gold to the CurrencyWallet.
    /// </summary>
    public static NightRewards ComputeNightRewards(
        int nightIndex,
        int villagersAlive,
        int difficultyTier)
    {
        int nightLevel = Mathf.Max(0, nightIndex + difficultyTier);

        const int baseGold = 20;
        const int goldPerNight = 10;
        const int goldPerVillager = 2;

        int gold =
            baseGold +
            (nightLevel * goldPerNight) +
            (villagersAlive * goldPerVillager);

        // flavour text based on outcome
        string flavour;
        if (villagersAlive <= 0)
        {
            flavour = "No villagers survived. Merchants are hesitant to trade with you.";
        }
        else if (villagersAlive <= 2)
        {
            flavour = "A handful of villagers cling to life. Supplies are scarce.";
        }
        else if (villagersAlive <= 5)
        {
            flavour = "Most of the village made it through. The smith sets aside a few crates for you.";
        }
        else
        {
            flavour = "The village slept light while you did the work. Traders line up to pay you.";
        }

        var wallet = CurrencyWallet.Instance;
        if (wallet != null && gold > 0)
        {
            wallet.AddGold(gold);
        }

        string summary = $"You earned {gold} gold.\n{flavour}";
        return new NightRewards(gold, summary);
    }
}
