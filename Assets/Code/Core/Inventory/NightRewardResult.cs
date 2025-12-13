using System.Collections.Generic;

public sealed class NightRewardResult
{
    public int gold;
    public readonly List<(object item, int quantity)> items = new();
    public readonly List<(object item, int quantity)> consumables = new();
    public string summary;
}
