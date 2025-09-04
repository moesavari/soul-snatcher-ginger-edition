using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int _maxHealth = 3;
    [SerializeField] private bool _isPlayerHealth;

    private int _current;
    public int current => _current;
    public int max => _maxHealth;
    public bool isDead => _current <= 0;

    public event Action<Health> OnDeath;
    public event Action<Health, int> OnDamaged;

    private void Awake()
    {
        _current = Mathf.Max(1, _maxHealth);
    }

    public void TakeDamage(int amount, Vector3 hitPoint, GameObject source)
    {
        if (isDead) return;

        _current = Mathf.Max(0, _current - Mathf.Max(0, amount));
        OnDamaged?.Invoke(this, amount);

        if (_current == 0) Die();
    }

    public void Heal(int amount)
    {
        if(isDead) return;

        _current = Mathf.Clamp(_current + Mathf.Max(0, amount), 0, _maxHealth);
    }

    private void Die()
    {
        OnDeath?.Invoke(this);
        if (_isPlayerHealth) GameEvents.RaisePlayerDied();
        Destroy(gameObject);
    }
}
