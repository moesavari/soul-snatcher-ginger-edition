using System;
using UnityEngine;

namespace Game.Systems
{
    [DefaultExecutionOrder(-200)]
    public class ReputationSystem : MonoSingleton<ReputationSystem>, IReputationReadOnly
    {
        public const int MIN_REPUTATION = -100;
        public const int MAX_REPUTATION = 100;

        [Header("Runtime Value")]
        [SerializeField] private int _reputation;
        public int reputation => _reputation;

        public event Action<int> OnReputationChanged;

        protected override void Awake()
        {
            base.Awake();
            _reputation = Mathf.Clamp(_reputation, MIN_REPUTATION, MAX_REPUTATION);
        }

        public void SetReputation(int value)
        {
            _reputation = Mathf.Clamp(value, MIN_REPUTATION, MAX_REPUTATION);
            OnReputationChanged?.Invoke(_reputation);
        }

        public void AddReputation(int delta)
        {
            _reputation = Mathf.Clamp(_reputation + delta, MIN_REPUTATION, MAX_REPUTATION);
            OnReputationChanged?.Invoke(_reputation);
        }

        public float Normalized => Mathf.InverseLerp(MIN_REPUTATION, MAX_REPUTATION, _reputation);
    }
}
