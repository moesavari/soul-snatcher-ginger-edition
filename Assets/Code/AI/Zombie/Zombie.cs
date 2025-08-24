using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]

public class Zombie : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 1.2f;
    [SerializeField] private int _maxHealth = 3;
    [SerializeField] private Transform _primaryTarget;

    [SerializeField] private AudioCue _deathCue;

    private Rigidbody2D _rb;
    private ZombieSpawner _spawner;
    private Health _health;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _health = this.Require<Health>();
        _health.OnDeath += HandleDeath;
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

    private Transform ResolveTarget()
    {
        if(_primaryTarget != null) return _primaryTarget;

        var player = GameManager.Instance?.player;
        return player != null ? player.transform : null;
    }

    private void HandleDeath(Health hp)
    {
        if (_deathCue != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayCue(_deathCue, transform.position);

        Debug.Log("[Zombie] Died at " + transform.position);
    }
}
