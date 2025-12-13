using UnityEngine;

[CreateAssetMenu(fileName = "NightRewardTier", menuName = "SoulSnatched/Rewards/Night Reward Tier")]
public class NightRewardTier : ScriptableObject
{
    [Header("Tier Range")]
    [SerializeField] private int _minNightIndexInclusive = 0;
    [SerializeField] private int _maxNightIndexInclusive = 0;

    [Header("Gold")]
    [SerializeField] private int _minGold = 20;
    [SerializeField] private int _maxGold = 40;

    [Header("Loot")]
    [SerializeField] private LootTable _itemLootTable;
    [SerializeField] private LootTable _consumableLootTable;

    [Header("Roll Counts")]
    [SerializeField] private int _minItemRolls = 0;
    [SerializeField] private int _maxItemRolls = 1;

    [SerializeField] private int _minConsumableRolls = 0;
    [SerializeField] private int _maxConsumableRolls = 2;

    public int minNightIndexInclusive => _minNightIndexInclusive;
    public int maxNightIndexInclusive => _maxNightIndexInclusive;

    public int minGold => _minGold;
    public int maxGold => _maxGold;

    public LootTable itemLootTable => _itemLootTable;
    public LootTable consumableLootTable => _consumableLootTable;

    public int minItemRolls => _minItemRolls;
    public int maxItemRolls => _maxItemRolls;

    public int minConsumableRolls => _minConsumableRolls;
    public int maxConsumableRolls => _maxConsumableRolls;
}
