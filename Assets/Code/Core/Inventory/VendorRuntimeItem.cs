using UnityEngine;

namespace Game.Core.Inventory
{
    [System.Serializable]
    public class VendorRuntimeItem
    {
        [SerializeField] private ItemDef _item;
        [SerializeField] private int _quantity;
        [SerializeField] private int _price;

        public ItemDef item => _item;
        public int quantity => _quantity;
        public int price => _price;

        public VendorRuntimeItem(ItemDef itemDef, int quantity, int price)
        {
            _item = itemDef;
            _quantity = quantity;
            _price = price;
        }
    }
}
