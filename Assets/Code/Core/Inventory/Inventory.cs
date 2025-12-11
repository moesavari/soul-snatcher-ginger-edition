using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.Core.Inventory
{
    public class Inventory : MonoBehaviour
    {
        [SerializeField] private int _slots = 12;
        [SerializeField] private List<ItemStack> _contents = new List<ItemStack>();
        [SerializeField] private TMP_Text _goldText;

        public int slots => _slots;
        public IReadOnlyList<ItemStack> contents => _contents;

        public event Action<Inventory, ItemStack> OnItemAdded;
        public event Action<Inventory, ItemStack> OnItemRemoved;

        private void Awake()
        {
            if (_contents.Count < _slots)
                for (int i = _contents.Count; i < _slots; i++)
                    _contents.Add(new ItemStack());
        }

        private void OnEnable()
        {
            if (_goldText) _goldText.text = CurrencyWallet.Instance ? CurrencyWallet.Instance.gold.ToString() : "0";

            if (CurrencyWallet.Instance != null) CurrencyWallet.Instance.OnGoldChanged += OnGoldChanged;
        }

        private void OnDisable()
        {
            if(CurrencyWallet.Instance != null) CurrencyWallet.Instance.OnGoldChanged -= OnGoldChanged;
        }

        public bool TryAdd(ItemDef def, int amount, out int leftover)
        {
            leftover = Mathf.Max(0, amount);

            if(def == null || leftover == 0) return false;

            if (def.stackable)
            {
                for (int i = 0; i < _contents.Count && leftover > 0; i++)
                {
                    var s = _contents[i];
                    if (s.def != def || s.amount >= s.MaxStack) continue;

                    int space = s.MaxStack - s.amount;
                    int moved = Mathf.Clamp(leftover, 0, space);
                    if (moved <= 0) continue;

                    s.amount += moved;
                    _contents[i] = s;
                    leftover -= moved;

                    FireAdded(new ItemStack { def = def, amount = moved });
                }
            }

            for (int i = 0; i < _contents.Count && leftover > 0; ++i)
            {
                if (!_contents[i].IsEmpty) continue;

                int moved = Mathf.Min(def.stackable ? def.maxStack : 1, leftover);
                _contents[i] = new ItemStack { def = def, amount = moved };

                leftover -= moved;

                FireAdded(new ItemStack { def = def, amount = moved });
                if (!def.stackable) break;
            }

            return leftover == 0;
        }

        public bool TryRemove(ItemDef def, int amount)
        {
            if(def == null || amount <= 0) return false;

            int need = amount;
            int have = 0;

            foreach (var s in _contents) if (s.def == def) have += s.amount;
            if(have < need) return false;

            for (int i = 0; i < _contents.Count && need > 0; i++)
            {
                var s = _contents[i];
                if (s.def != def || s.amount <= 0) continue;

                int take = Mathf.Min(s.amount, need);
                s.amount -= take;
                need -= take;

                if (s.amount <= 0) s = new ItemStack();
                _contents[i] = s;

                FireRemoved(new ItemStack { def = def, amount = take });
            }
            return true;
        }

        public bool HasAt(int index, ItemDef def, int amount)
        {
            if (def == null || amount <= 0) return false;
            if (index < 0 || index >= _contents.Count) return false;

            var s = _contents[index];
            return s.def == def && s.amount >= amount;
        }

        public bool TryRemoveAt(int index, ItemDef def, int amount)
        {
            if (!HasAt(index, def, amount)) return false;

            var s = _contents[index];
            s.amount -= amount;

            if (s.amount <= 0)
                s = new ItemStack();

            _contents[index] = s;

            FireRemoved(new ItemStack { def = def, amount = amount });
            return true;
        }

        public bool Has(ItemDef def, int amount)
        {
            if (def == null || amount <= 0) return false;

            int have = 0;

            foreach (var s in _contents) if (s.def == def) have += s.amount;
            return have >= amount;
        }

        public void Clear()
        {
            for (int i = 0; i < _contents.Count; i++)
            {
                var s = _contents[i];
                if (s.IsEmpty) continue;

                FireRemoved(new ItemStack { def = s.def, amount = s.amount });

                _contents[i] = new ItemStack();
            }
        }

        private void FireAdded(ItemStack s) { OnItemAdded?.Invoke(this, s); GameEvents.RaiseItemAdded(this, s); }

        private void FireRemoved(ItemStack s) { OnItemRemoved?.Invoke(this, s); GameEvents.RaiseItemRemoved(this, s); }

        private void OnGoldChanged(int value) { if (_goldText) _goldText.text = value.ToString(); }
    }
}
