using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]

public class Zombie : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 1.2f;
    [SerializeField] private float _stopDistance = 0.5f;    // prevents body overlap with player
    [SerializeField] private float _accel = 12f;            // smoothing toward desired velocity

    [Header("Targeting")]
    [SerializeField] private Transform _primaryTarget;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer _visual;        // assign the visible SR (child "Visual")
    [SerializeField] private Transform _biteRig;            // child that rotates; bite hitbox sits under here (local +X forward)

    [Header("AudioCues")]
    [SerializeField] private AudioCue _spawnCue;

    private Rigidbody2D _rb;
    private Health _health;

    private Vector2 _vel;
    private bool _deathNotified;

    private void Awake()
    {
        _rb = this.Require<Rigidbody2D>();
        _health = this.Require<Health>();
    }

    private void OnEnable()
    {
        _deathNotified = false;
        _health.OnDeath += OnDied;

        if(_spawnCue != null) AudioManager.Instance.PlayCue(_spawnCue, worldPos: transform.position);
    }

    private void OnDisable()
    {
        _health.OnDeath -= OnDied;
        // Do NOT notify spawner here; death uses Health.OnDeath.
        // If you later pool zombies (disable without death), decide explicitly how to handle alive counts.
    }

    private void FixedUpdate()
    {
        Transform target = ResolveTarget();
        if (target == null) return;

        Vector2 pos = _rb.position;
        Vector2 toTarget = (Vector2)target.position - pos;
        float dist = toTarget.magnitude;

        // Face + aim bite rig
        if (_visual != null) _visual.flipX = (toTarget.x < 0f);
        if (_biteRig != null && toTarget.sqrMagnitude > 0.0001f)
            _biteRig.right = toTarget.normalized; // +X is forward

        // Stop before overlapping; otherwise move toward target
        Vector2 desired = (dist > _stopDistance) ? toTarget.normalized * _moveSpeed : Vector2.zero;
        _vel = Vector2.MoveTowards(_vel, desired, _accel * Time.fixedDeltaTime);

#if UNITY_6000_0_OR_NEWER
        _rb.linearVelocity = _vel;
#else
        _rb.velocity = _vel;
#endif
    }

    private Transform ResolveTarget()
    {
        if (_primaryTarget != null) return _primaryTarget;
        var gmPlayer = GameManager.Instance?.player;
        return gmPlayer != null ? gmPlayer.transform : null;
    }

    private void OnDied(Health _)
    {
        if (_deathNotified) return;
        _deathNotified = true;
    }

    public void SetMoveSpeed(float speed)
    {
        _moveSpeed = Mathf.Max(0f, speed);
    }
}