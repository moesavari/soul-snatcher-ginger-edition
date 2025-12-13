using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NightRewardProfile", menuName = "SoulSnatched/Rewards/Night Reward Profile")]
public class NightRewardProfile : ScriptableObject
{
    [SerializeField] private List<NightRewardTier> _tiers = new();

    public NightRewardTier GetTierForNightIndex(int nightIndex)
    {
        if (_tiers == null || _tiers.Count == 0)
        {
            DebugManager.LogWarning("NightRewardProfile has no tiers.", this);
            return null;
        }

        for (int i = 0; i < _tiers.Count; i++)
        {
            var t = _tiers[i];
            if (t == null) continue;

            if (nightIndex >= t.minNightIndexInclusive && nightIndex <= t.maxNightIndexInclusive)
            {
                return t;
            }
        }

        return _tiers[_tiers.Count - 1];
    }
}
