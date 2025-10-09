using System;
using UnityEngine;

namespace Game.Systems
{
    public class ReputationSystem : MonoSingleton<ReputationSystem>
    {
        [SerializeField] private int _reputation;
        public int reputation => _reputation;

        public event Action<int> OnReputationChanged;

        public void SetReputation(int value)
        {
            _reputation = value;
            OnReputationChanged?.Invoke(_reputation);
        }

        public void AddReputation(int delta)
        {
            _reputation += delta;
            OnReputationChanged?.Invoke(_reputation);
        }
    }
}