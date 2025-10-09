using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]

public class Zombie : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _stopDistance = 0.5f;    // prevents body overlap with player
    [SerializeField] private float _accel = 12f;            // smoothing toward desired velocity

    [Header("Targeting")]
    [SerializeField] private Transform _primaryTarget;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer _visual;        // assign the visible SR (child "Visual")
    [SerializeField] private Transform _biteRig;            // child that rotates; bite hitbox sits under here (local +X forward)

    [Header("AudioCues")]
    [SerializeField] private AudioCue _spawnCue;

    [Header("AI Settings")]
    [SerializeField] private NPCStats _stats;
    [SerializeField] private LayerMask _zombieMask;
    [SerializeField] private bool _includeTriggers = false;
    [SerializeField] private float _separationStrength = 0.5f;
    [SerializeField] private float _separationRadius = 0.25f;
    [SerializeField] private ZombieBite _zombieBite;

    [Header("Sunrise Enraged-Burning")]
    [SerializeField] private float _burnDuration = 7f;
    [SerializeField] private float _burnDPS = 0.5f;
    [SerializeField] private float _enrageSpeedMult = 1.35f;
    [SerializeField] private GameObject _burnFxPrefab;

    private Rigidbody2D _rb;
    private ContactFilter2D _filter;

    private Vector2 _vel;
    private bool _deathNotified;
    private readonly Collider2D[] _overlapBuffer = new Collider2D[16];

    private bool _isEnragedBurning;
    private bool _morgensternToten;
    private float _burnTimer;

    private int _currentHealth;
    public int currentHealth => _currentHealth;

    public System.Action OnDeath;
    public System.Action OnDamaged;

    private void Awake()
    {
        _rb = this.Require<Rigidbody2D>();
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        _currentHealth = _stats != null ? _stats.Health : 1;

        if (_zombieBite == null) _zombieBite = GetComponentInChildren<ZombieBite>();
        if (_zombieBite != null) _zombieBite.InitBaseValues();

        _zombieBite.SetOwner(_stats);
    }

    private void OnEnable()
    {
        _filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = _zombieMask,
            useTriggers = _includeTriggers,   // true if your separation colliders are triggers
            useDepth = false                  // set true + min/max if you need depth filtering
        };

        _deathNotified = false;
        //_health.OnDeath += OnDied;

        if (_spawnCue != null) AudioManager.Instance.PlayCue(_spawnCue, worldPos: transform.position);

        if (!TimeCycleManager.Instance.isNight)
        {
            EnterEnragedForm();
        }

        GameEvents.DayStarted += OnDayStarted;
    }

    private void OnDisable()
    {
        //_health.OnDeath -= OnDied;
        GameEvents.DayStarted -= OnDayStarted;
    }

    //private void Update()
    //{
    //    if (_isEnragedBurning) DoBurnTick();
    //}

    private void FixedUpdate()
    {
        Transform target = ResolveTarget();

        var kb = GetComponent<KnockbackReceiver>();
        if (kb != null && kb.isStunned) return;

        if (target == null)
        {
            _vel = Vector2.MoveTowards(_vel, Vector2.zero, _accel * Time.fixedDeltaTime);
            _rb.MovePosition(_rb.position + _vel * Time.fixedDeltaTime);
            return;
        }

        Vector2 pos = _rb.position;
        Vector2 toTarget = (Vector2)target.position - pos;
        float dist = toTarget.magnitude;

        // Face + aim bite rig
        if (_visual != null) _visual.flipX = (toTarget.x < 0f);
        if (_biteRig != null && toTarget.sqrMagnitude > 0.0001f)
            _biteRig.right = toTarget.normalized;

        Vector2 desired = (dist > _stopDistance) ? toTarget.normalized * _stats.MoveSpeed : Vector2.zero;

        // Pause movement if biting
        if (_zombieBite != null && _zombieBite.IsBiting) desired = Vector2.zero;

        // Soft separation (GC-free)
        ContactFilter2D filter = _filter; // struct copy; _filter set up once

        int count = Physics2D.OverlapCircle(pos, _separationRadius, filter, _overlapBuffer);

        for (int i = 0; i < count; i++)
        {
            var hit = _overlapBuffer[i];
            if (hit == null) continue;
            var otherRb = hit.attachedRigidbody;
            if (otherRb == null || otherRb == _rb) continue;

            Vector2 away = (pos - (Vector2)otherRb.position);
            float d = away.magnitude;
            if (d > 0.0001f)
            {
                // Taper separation by distance so close overlaps push stronger
                float falloff = Mathf.Clamp01(1f - (d / _separationRadius));
                desired += away.normalized * (_separationStrength * falloff);
            }
        }

        // Clamp final desired speed so separation can�t exceed _moveSpeed wildly
        if (desired.sqrMagnitude > (_stats.MoveSpeed * _stats.MoveSpeed))
            desired = desired.normalized * _stats.MoveSpeed;

        // Smooth toward desired velocity
        _vel = Vector2.MoveTowards(_vel, desired, _accel * Time.fixedDeltaTime);

        // Move via Rigidbody for solid collisions
        _rb.MovePosition(_rb.position + _vel * Time.fixedDeltaTime);
    }

    private Transform ResolveTarget()
    {
        if (_primaryTarget != null) return _primaryTarget;
        var gmPlayer = GameManager.Instance?.player;
        return gmPlayer != null ? gmPlayer.transform : null;
    }

    private void OnDied()
    {
        if (_deathNotified) return;

        if (_zombieBite != null) _zombieBite.SetEnraged(false);
        _isEnragedBurning = false;

        if (_morgensternToten) DebugManager.Log("Skipping Loot", this);

        _deathNotified = true;
        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    public void TakeDamage(int amount)
    {
        OnDamaged?.Invoke();
        _currentHealth = Mathf.Max(0, _currentHealth - Mathf.Max(0, amount));
        if (_currentHealth <= 0) OnDied();
    }

    private void OnDayStarted()
    {
        if (!isActiveAndEnabled) return;

        //if (_health != null && _health.isDead) return;

        if (_isEnragedBurning) return;

        EnterEnragedForm();
    }

    private void EnterEnragedForm()
    {
        _isEnragedBurning = true;

        _burnTimer = _burnDuration;
        //_moveSpeed = _baseMoveSpeed * _enrageSpeedMult;

        if (_zombieBite != null) _zombieBite.SetEnraged(true);

        if (_burnFxPrefab != null)
            Instantiate(_burnFxPrefab, transform.position, Quaternion.identity, transform);
    }

    private void OnStatsChanged(NPCStats stats)
    {
        //Setup enrage here
    }

    //private void DoBurnTick()
    //{
    //    _burnTimer -= Time.deltaTime;

    //    if (_health != null && !_health.isDead)
    //    {
    //        int burnDamage = Mathf.CeilToInt(_burnDPS * Time.deltaTime);
    //        if (burnDamage > 0) _health.TakeDamage(burnDamage, transform.position, gameObject);
    //    }

    //    if (_burnTimer <= 0f && _health != null && !_health.isDead)
    //    {
    //        _morgensternToten = true;
    //        _health.Die();
    //    }
    //}

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Visualize separation radius
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, _separationRadius);
        Gizmos.color = Color.clear;
    }

    private void OnValidate()
    {
        // Keep filter in sync when you tweak values in the Inspector
        _filter.useLayerMask = true;
        _filter.layerMask = _zombieMask;
        _filter.useTriggers = _includeTriggers;
    }
#endif
}