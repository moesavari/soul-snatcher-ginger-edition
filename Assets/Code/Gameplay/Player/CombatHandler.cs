using UnityEngine;
using System.Collections;

public class CombatHandler : MonoBehaviour
{
    [Header("Visual Root & Offsets")]
    [SerializeField] private Transform _body;                         // sprite root to flip (no rotation).
    [SerializeField] private SpriteRenderer _sprite;

    [Header("Refs")]
    [SerializeField] private GameObject _meleeHitBox;                 // child with trigger collider on Hitbox layer
    [SerializeField] private Transform _firePoint;                    // +X (right) is forward
    [SerializeField] private GameObject _arrowPrefab;

    [Header("Melee")]
    [SerializeField] private float _meleeActiveSeconds = 0.15f;

    [Header("Ranged")]
    [SerializeField] private float _arrowSpeed = 10f;
    [SerializeField] private float _projectileForwardOffsetDeg = 0f;  // set to -90 if your arrow art faces +Y

    private float _meleeTimer;
    private float _rangedTimer;
    private Camera _cam;
    private Animator _animator;
    private Vector2 _lastAimDir = Vector2.right;

    private const float _aimEpsilon = 0.0001f;

    private void Awake()
    {
        _cam = Camera.main;
        _animator = this.Require<Animator>();
        if (_body == null) _body = transform;
        // lock visual scale magnitude; only X sign will flip
        _body.localScale = new Vector3(Mathf.Sign(_body.localScale.x) * 0.2f, 0.2f, 1f);
    }

    private void Update()
    {
        _meleeTimer -= Time.deltaTime;
        _rangedTimer -= Time.deltaTime;

        UpdateAiming(); // single source of truth for facing
    }

    // ---------- Aiming ----------

    private void UpdateAiming()
    {
        Vector2 aim = GetAimDirToMouse();
        if (aim.sqrMagnitude <= _aimEpsilon)
            aim = GetMoveOrVelocityDir();

        if (aim.sqrMagnitude > _aimEpsilon)
            _lastAimDir = aim.normalized;

        if (_sprite != null)
            _sprite.flipX = (_lastAimDir.x < 0f);

        if (_firePoint != null)
        {
            _firePoint.right = _lastAimDir;

            // Editor-only guard: warns if any ancestor has negative scale
            AssertNoNegativeScaleInChain(_firePoint);
        }
    }

    private Vector2 GetAimDirToMouse()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return Vector2.zero;

        // Use firePoint as source if available; else use body
        Vector3 src = _firePoint != null ? _firePoint.position : _body.position;

        Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = src.z; // flatten to 2D plane
        Vector2 dir = (Vector2)(mouseWorld - src);
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.zero;
    }

    private Vector2 GetMoveOrVelocityDir()
    {
        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
#if UNITY_6000_0_OR_NEWER
            Vector2 v = rb.linearVelocity;
#else
            Vector2 v = rb.velocity;
#endif
            if (v.sqrMagnitude > 0.0001f) return v.normalized;
        }
        return _lastAimDir;
    }

    // ---------- Attacks ----------

    private void DoMeleeAttack()
    {
        if (_meleeHitBox == null)
        {
            DebugManager.LogWarning("[CombatHandler] Missing Melee hitbox.");
            return;
        }

        // final alignment at swing time
        if (_firePoint != null) _firePoint.right = _lastAimDir;

        _animator?.SetTrigger("Melee");
        StartCoroutine(EnableHitbox(_meleeHitBox, _meleeActiveSeconds));
    }

    private void DoRangedAttack(Vector2 aimDir)
    {
        if (_arrowPrefab == null || _firePoint == null)
        {
            DebugManager.LogWarning("[CombatHandler] Missing arrowPrefab or firePoint.");
            return;
        }

        // Rotate projectile so its +X points along aim (plus optional art offset)
        float baseAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0f, 0f, baseAngle + _projectileForwardOffsetDeg);

        var arrow = (SpawnManager.Instance != null)
            ? SpawnManager.Instance.Spawn(_arrowPrefab, _firePoint.position, rot)
            : Instantiate(_arrowPrefab, _firePoint.position, rot);

        if (arrow.TryGetComponent<Rigidbody2D>(out var rb))
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = aimDir * _arrowSpeed;
            rb.WakeUp();
#else
            rb.velocity = aimDir * _arrowSpeed;
#endif
        }
        else
        {
            DebugManager.LogWarning("[CombatHandler] Arrow has no Rigidbody2D.");
        }

        _animator?.SetTrigger("Bow");
    }

    // ---------- Utils ----------

    private IEnumerator EnableHitbox(GameObject hitbox, float duration)
    {
        if (hitbox.TryGetComponent<MeleeHitbox>(out var mh)) mh.Arm();

        hitbox.SetActive(true);
        yield return new WaitForSeconds(duration);
        hitbox.SetActive(false);

        if (mh != null) mh.Disarm();
    }

    private void OnDrawGizmosSelected()
    {
        if (_firePoint == null) return;
        var cam = _cam != null ? _cam : Camera.main;
        if (cam == null) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = _firePoint.position.z;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(_firePoint.position, mouseWorld);
    }

#if UNITY_EDITOR
    private void AssertNoNegativeScaleInChain(Transform t)
    {
        for (var p = t; p != null; p = p.parent)
        {
            if (p.localScale.x < 0f || p.localScale.y < 0f || p.localScale.z < 0f)
            {
                DebugManager.LogWarning($"[Facing] Negative scale detected on '{p.name}'. " +
                                 "This mirrors child local space and can flip hitbox sides.");
                break;
            }
        }
    }

#else
[System.Diagnostics.Conditional("FALSE")]
private static void AssertNoNegativeScaleInChain(Transform t){}
#endif
}
