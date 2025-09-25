using System;
using UnityEngine;

namespace Game.Systems
{
    public class ReputationSystem : MonoBehaviour
    {
        public static ReputationSystem Instance { get; private set; }

        [SerializeField] private int _reputation;
        public int reputation => _reputation;

        public event Action<int> OnReputationChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

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