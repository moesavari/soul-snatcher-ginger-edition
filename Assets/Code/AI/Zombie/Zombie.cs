using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public class Zombie : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer _visual;
    [SerializeField] private Transform _biteRig;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 1.2f;
    [SerializeField] private float _stopDistance = 0.45f;   // prevents body overlap
    [SerializeField] private float _accel = 12f;            // smoothing toward desired vel

    [Header("Targeting")]
    [SerializeField] private Transform _primaryTarget;

    private Rigidbody2D _rb;
    private ZombieSpawner _spawner;
    private Health _health;

    private Vector2 _vel;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _health = this.Require<Health>();
    }

    private void OnEnable() { }

    private void OnDisable()
    {
        _spawner?.NotifyZombieDied();
    }

    private void FixedUpdate()
    {
        Transform target = ResolveTarget();
        if (target == null) return;

        Vector2 pos = _rb.position;
        Vector2 toTarget = (Vector2)target.position - pos;
        float dist = toTarget.magnitude;

        if (_visual != null) _visual.flipX = (toTarget.x < 0f);
        if (_biteRig != null && toTarget.sqrMagnitude > 0.0001f)
            _biteRig.right = toTarget.normalized;

        Vector2 desired = (dist > _stopDistance) ? toTarget.normalized * _moveSpeed : Vector2.zero;

        _vel = Vector2.MoveTowards(_vel, desired, _accel * Time.fixedDeltaTime);

        _rb.linearVelocity = _vel;
    }

    public void RegisterSpawner(ZombieSpawner spawner)
    {
        _spawner = spawner;
        _spawner.NotifyZombieSpawned();
    }

    private Transform ResolveTarget()
    {
        if (_primaryTarget != null) return _primaryTarget;

        var player = GameManager.Instance?.player;
        return player != null ? player.transform : null;
    }
}
