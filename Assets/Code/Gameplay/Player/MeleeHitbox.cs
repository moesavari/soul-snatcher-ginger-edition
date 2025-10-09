using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MeleeHitbox : MonoBehaviour
{
    [SerializeField] private int _damage = 1;
    [SerializeField] private string _ownerTag = "Player";     // who owns this hitbox
    [SerializeField] private float _knockback = 0f;           // optional

    private bool _armed;
    private readonly HashSet<Health> _hitThisSwing = new();

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true; // critical
        gameObject.layer = LayerMask.NameToLayer("Hitbox");
    }

    /// <summary>Call right before enabling the swing.</summary>
    public void Arm()
    {
        _armed = true;
        _hitThisSwing.Clear();
    }

    /// <summary>Call after the swing ends.</summary>
    public void Disarm() => _armed = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_armed) return;

        // Get the rigidbody root if present; otherwise fall back to transform root.
        Transform root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform.root;

        // Ignore owner/friendly
        if (!string.IsNullOrEmpty(_ownerTag) && root.CompareTag(_ownerTag))
            return;

        if (!root.TryGetComponent<Health>(out var hp)) return;
        if (_hitThisSwing.Contains(hp)) return; // one hit per swing per target

        hp.TakeDamage(_damage, transform.position, gameObject);
        _hitThisSwing.Add(hp);

        // Optional knockback
        if (_knockback > 0f && root.TryGetComponent<Rigidbody2D>(out var rb))
        {
            Vector2 dir = (root.position - transform.position).normalized;
            rb.AddForce(dir * _knockback, ForceMode2D.Impulse);
        }
    }
}
