using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Zombie : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 1.2f;
    [SerializeField] private int _maxHealth = 3;
    [SerializeField] private Transform _primaryTarget;

    private int _health;
    private Rigidbody2D _rb;
    private ZombieSpawner _spawner;

    public int health => _health;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _health = _maxHealth;
    }

    private void OnEnable()
    {
        _spawner?.NotifyZombieSpawned();
    }

    private void OnDisable()
    {
        _spawner?.NotifyZombieDied();
    }

    private void FixedUpdate()
    {
        Transform target = ResolveTarget();

        if (target == null) return;

        Vector2 direction = (target.position - transform.position).normalized;
        _rb.MovePosition(_rb.position + direction * _moveSpeed * Time.fixedDeltaTime);
    }

    public void RegisterSpawner(ZombieSpawner spawner)
    {
        _spawner = spawner;
        _spawner.NotifyZombieSpawned();
    }


    public void TakeDamage(int amount)
    {
        _health -= amount;
        if (_health <= 0) Die();
    }

    private void Die()
    {
        Destroy(gameObject);
    }

    private Transform ResolveTarget()
    {
        if(_primaryTarget != null) return _primaryTarget;

        var player = GameManager.Instance?.player;
        return player != null ? player.transform : null;
    }
}
