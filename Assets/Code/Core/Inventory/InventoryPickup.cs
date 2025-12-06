using Game.Core.Inventory;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class InventoryPickup : MonoBehaviour
{
    [SerializeField] private ItemDef _def;
    [SerializeField, Min(1)] private int _amount = 1;
    [SerializeField] private bool _destroyOnPickup = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var inv = other.GetComponentInParent<Inventory>();
        if (!inv) return;

        inv.TryAdd(_def, _amount, out int leftover);
        if (_destroyOnPickup && leftover < _amount)
            Destroy(gameObject);
        else
            _amount = leftover;
    }
}
