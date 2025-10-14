using Game.Systems;
using UnityEngine;

public class Villager : MonoBehaviour
{
    [SerializeField] private NPCStats _stats;

    [Header("Navigation")]
    [SerializeField] private Transform _hideSpot;
    [SerializeField] private float _wanderRadius = 1.5f;

    [Header("Night Behavior")]
    [SerializeField] private float _panicRadius = 6f;          // if zombie closer than this → panic
    [SerializeField] private float _panicSpeedMult = 1.6f;     // faster when panicking
    [SerializeField] private float _emergeScatter = 1.0f;      // when leaving hide spot at day
    [SerializeField] private LayerMask _zombieMask;

    [Header("Player Interaction Rewards")]
    [SerializeField] private int _repAmount = 10;              // rescue reward (positive)
    [SerializeField] private int _soulAmount = 1;              // siphon reward (souls)
    [SerializeField] private int _rescueRepBonus = 15;         // extra on rescue

    private readonly Collider2D[] _overlap = new Collider2D[8];

    private float _currentSpeed;
    private int _currentHealth;

    private bool _isNight;
    private bool _isPanicking;
    private bool _isHiding;
    public bool isHiding => _isHiding;
    private bool _isAlive = true;
    public bool isAlive => _isAlive;

    private Vector3 _startPos;
    private Vector3 _wanderTarget;

    private ContactFilter2D _zombieFilter;
    
    protected virtual void Awake()
    {
        _startPos = transform.position;
        PickNewWanderTarget();
        _currentSpeed = _stats.MoveSpeed;

        _zombieFilter = new ContactFilter2D();
        _zombieFilter.SetLayerMask(_zombieMask);
        _zombieFilter.useLayerMask = true;
        _zombieFilter.useTriggers = true;

        GameEvents.DayStarted += OnDay;      
        GameEvents.NightStarted += OnNight; 
    }

    private void OnDestroy()
    {
        GameEvents.DayStarted -= OnDay;
        GameEvents.NightStarted -= OnNight;
    }

    protected virtual void Update()
    {
        // Decide target based on state
        if (_isNight)
        {
            Vector3 dest = (_hideSpot != null ? _hideSpot.position : _startPos);

            // Panic check: any zombie within radius?
            _isPanicking = ZombieNearby(_panicRadius);
            _currentSpeed = _stats.MoveSpeed * (_isPanicking ? _panicSpeedMult : 1f);

            // Move toward hide, mark hidden if close
            MoveTo(dest, _currentSpeed);
            if ((transform.position - dest).sqrMagnitude <= 0.08f)
                _isHiding = true;
        }
        else
        {
            // Daytime: wander
            if ((transform.position - _wanderTarget).sqrMagnitude < 0.05f) PickNewWanderTarget();
            MoveTo(_wanderTarget, _stats.MoveSpeed);
        }

        // if (Input.GetKeyDown(KeyCode.R)) { OnSoulAbsorb(); GetComponent<Health>().Die(); }
    }

    private void OnDay()
    {
        _isNight = false;
        _isHiding = false;
        _isPanicking = false;

        if (_hideSpot != null)
        {
            var jitter = (Vector2)Random.insideUnitCircle * _emergeScatter;
            _startPos = _hideSpot.position + new Vector3(jitter.x, jitter.y, 0f);
        }
        PickNewWanderTarget();
    }

    private void OnNight()
    {
        _isNight = true;
        _isHiding = false;
        _isPanicking = false;
    }

    private void PickNewWanderTarget()
    {
        var p = Random.insideUnitCircle * _wanderRadius;
        _wanderTarget = _startPos + new Vector3(p.x, p.y, 0f);
    }

    private bool ZombieNearby(float radius)
    {
        if (radius <= 0f) return false;
        int hits = Physics2D.OverlapCircle(transform.position, radius, _zombieFilter, _overlap);
        for (int i = 0; i < hits; i++)
        {
            var c = _overlap[i];
            if (c != null && c.GetComponentInParent<Zombie>() != null) return true;
        }
        return false;
    }

    public void OnSoulAbsorb()
    {
        if(!_isAlive) return;

        SoulSystem.Instance.AddSouls(_soulAmount);
        ReputationSystem.Instance.AddReputation(-_repAmount);

        KillSelf();
    }

    public void OnRescued()
    {
        if (!_isAlive) return;

        ReputationSystem.Instance.AddReputation(_rescueRepBonus);
        // Optional: small heal, item, or VFX
    }

    private void MoveTo(Vector3 dest, float speed)
    {
        transform.position = Vector3.MoveTowards(transform.position, dest, speed * Time.deltaTime);
    }

    private void KillSelf()
    {
        if (!_isAlive) return;
        _isAlive = false;

        _stats?.RaiseDeath();

        Destroy(gameObject, 0.02f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, _panicRadius);
    }
#endif
}
