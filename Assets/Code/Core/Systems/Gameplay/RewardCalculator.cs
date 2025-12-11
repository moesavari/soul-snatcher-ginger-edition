public struct NightRewards
{
    public int souls;
    public int reputation;
    public string extra;
}

public static class RewardCalculator
{

    public static NightRewards Compute(int wavesCleared, int villagersAlive, int difficultyTier)
    {
        int souls = 10 + (wavesCleared * 2) + (difficultyTier * 3);
        int rep = villagersAlive * 2;
        string extra = villagersAlive > 0 ? $"{villagersAlive} villagers safe" : "No survivors";
        return new NightRewards { souls = souls, reputation = rep, extra = extra };
    }
}
