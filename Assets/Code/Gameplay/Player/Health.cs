using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int _baseMaxHealth = 100;
    [SerializeField] private int _currentHealth;
    [SerializeField] private int _gearHealth = 0; // total health from equipment

    public int max => _baseMaxHealth + _gearHealth;
    public int current => _currentHealth;
    public bool isDead => _currentHealth <= 0;

    public event Action<Health, int> OnDamaged;
    public event Action<Health> OnDeath;

    private GameObject _damageSource;

    private int _totalHealth;

    private void Awake()
    {
        _currentHealth = Mathf.Max(1, max);
    }

    public virtual void TakeDamage(int amount, Vector3 hitPoint, GameObject source)
    {
        if (isDead) return;

        _damageSource = source;
        int prev = _currentHealth;

        _currentHealth = Mathf.Max(0, _currentHealth - Mathf.Max(0, amount));
        OnDamaged?.Invoke(this, amount);

        if (_currentHealth == 0) Die();
    }

    public virtual void TakeStatDamage(Stats attackerStats, Weapon weapon, Vector3 hitPoint, GameObject source, bool isSpell = false)
    {
        //int dmg = DamageCalculator.CalculateDamage(attackerStats, weapon, GetComponent<Stats>(), isSpell);
        //TakeDamage(dmg, hitPoint, source);
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        _currentHealth = Mathf.Clamp(_currentHealth + Mathf.Max(0, amount), 0, max);
    }

    public virtual void Die()
    {
        OnDeath?.Invoke(this);

        // Example: If a villager and player killed, call soul/reputation
        if (CompareTag("Villager") && _damageSource && _damageSource.CompareTag("Player"))
            GetComponent<Villager>().OnSoulAbsorb();

        Destroy(gameObject);
    }
}
