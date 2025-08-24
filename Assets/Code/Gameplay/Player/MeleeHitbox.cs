using UnityEngine;

[RequireComponent(typeof(Collider2D))]

public class MeleeHitbox : MonoBehaviour
{
    [SerializeField] private int _damage = 1;
    [SerializeField] private string _ownerTag = "Player";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!string.IsNullOrEmpty(_ownerTag) && collision.CompareTag(_ownerTag)) return;

        if (collision.TryGetComponent<Health>(out var hp))
            hp.TakeDamage(_damage, transform.position, gameObject);
    }
}
