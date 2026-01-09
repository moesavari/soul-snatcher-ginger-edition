using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private CharacterStats _stats;

    [Header("Movement")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private KnockbackReceiver _knockback;

    [SerializeField] private MeleeWeapon _melee;
    [SerializeField] private RangedWeapon _ranged;
    private Vector2 _moveInput;

    private int _currentHealth;
    public int currentHealth => _currentHealth;

    private bool _isDead;
    public bool isDead => _isDead;

    public System.Action<int> OnDamaged;
    public System.Action OnDeath;

    private void Awake()
    {
        _currentHealth = _stats != null ? _stats.Health : 1;
        _isDead = false;

        _melee.SetOwner(_stats);
        _ranged.SetOwner(_stats);
    }

    private void OnEnable()
    {
        InputManager.Move += SetMoveInput;
        _stats.OnStatsChanged += OnStatsChanged;
    }

    private void OnDisable()
    {
        InputManager.Move -= SetMoveInput;
        _stats.OnStatsChanged -= OnStatsChanged;
    }

    private void FixedUpdate()
    {
        if (_knockback != null && _knockback.isStunned) return;

        bool attackLock =
            (_melee != null && _melee.isAttacking) ||
            (_ranged != null && _ranged.isShooting);
        if (attackLock) return;

        _rigidbody.MovePosition(_rigidbody.position + _moveInput * _stats.MoveSpeed * Time.fixedDeltaTime);
    }

    private void OnStatsChanged(CharacterStats stats)
    {
        if (_currentHealth > stats.Health)
            _currentHealth = stats.Health;
    }

    public void SetMoveInput(Vector2 v) { _moveInput = v; }

    public void TakeDamage(int amount)
    {
        if (_isDead) return;

        int prev = _currentHealth;
        _currentHealth = Mathf.Max(0, _currentHealth - Mathf.Max(0, amount));
        OnDamaged?.Invoke(prev - _currentHealth);
        if (_currentHealth <= 0) Die();
    }

    public void Heal(int amount)
    {
        if (_isDead) return;
        _currentHealth = Mathf.Clamp(_currentHealth + Mathf.Max(0, amount), 0, _stats.Health);
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        OnDeath?.Invoke();

        GameEvents.RaisePlayerDied();
        GameEvents.RaiseRoundLost();

        Destroy(gameObject);
    }
}
