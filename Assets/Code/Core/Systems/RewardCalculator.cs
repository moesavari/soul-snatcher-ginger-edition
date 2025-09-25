public struct NightRewards
{
    public int souls;
    public int reputation;
    public string extra; // e.g., "2 villagers rescued" or drops summary
}

public static class RewardCalculator
{
    // Plug in villagers alive, difficulty, streaks, etc.
    public static NightRewards Compute(int wavesCleared, int villagersAlive, int difficultyTier)
    {
        int souls = 10 + (wavesCleared * 2) + (difficultyTier * 3);
        int rep = villagersAlive * 2; // example
        string extra = villagersAlive > 0 ? $"{villagersAlive} villagers safe" : "No survivors";
        return new NightRewards { souls = souls, reputation = rep, extra = extra };
    }
}
