using UnityEngine;
using System.Collections;

public class CombatHandler : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject _meleeHitBox;
    [SerializeField] private Transform _firePoint;         // +X (right) must be the forward of your arrow sprite
    [SerializeField] private GameObject _arrowPrefab;

    [Header("Melee")]
    [SerializeField] private float _meleeActiveSeconds = 0.15f;
    [SerializeField] private float _meleeCooldown = 0.45f;

    [Header("Ranged")]
    [SerializeField] private float _arrowSpeed = 10f;
    [SerializeField] private float _rangedCooldown = 0.8f;

    private float _meleeTimer;
    private float _rangedTimer;
    private Camera _cam;
    private Animator _animator;

    private void Awake()
    {
        _cam = Camera.main;
        _animator = this.Require<Animator>();
    }

    private void Update()
    {
        _meleeTimer -= Time.deltaTime;
        _rangedTimer -= Time.deltaTime;

        // Always keep aimers facing the mouse
        Vector2 aimDir = GetAimDir();
        RotateAimers(aimDir);

        if (Input.GetKeyDown(KeyCode.Z) && _meleeTimer <= 0f)
        {
            DoMeleeAttack();
            _meleeTimer = _meleeCooldown;
        }

        if (Input.GetKeyDown(KeyCode.X) && _rangedTimer <= 0f)
        {
            DoRangedAttack(aimDir);          // <<< pass the computed aim
            _rangedTimer = _rangedCooldown;
        }
    }

    private Vector2 GetAimDir()
    {
        if (_firePoint == null) return Vector2.right;
        if (_cam == null) _cam = Camera.main;

        Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
        // Flatten to XY plane BEFORE normalizing
        Vector2 dir = ((Vector2)mouseWorld - (Vector2)_firePoint.position);
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
    }

    private void RotateAimers(Vector2 aimDir)
    {
        if (_firePoint != null)
        {
            // Arrow sprite must face +X
            _firePoint.right = aimDir;
        }
        if (_meleeHitBox != null)
        {
            _meleeHitBox.transform.right = aimDir;
        }
    }

    private void DoMeleeAttack()
    {
        if (_meleeHitBox == null)
        {
            Debug.LogWarning("[CombatHandler] Missing Melee hitbox.");
            return;
        }

        _animator?.SetTrigger("Melee");
        StartCoroutine(EnableHitbox(_meleeHitBox, _meleeActiveSeconds));
    }

    private void DoRangedAttack(Vector2 aimDir)
    {
        if (_arrowPrefab == null || _firePoint == null)
        {
            Debug.LogWarning("[CombatHandler] Missing arrowPrefab or firePoint.");
            return;
        }

        // Rotate projectile so its +X points along aim
        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0f, 0f, angle);

        var arrow = (SpawnManager.Instance != null)
            ? SpawnManager.Instance.Spawn(_arrowPrefab, _firePoint.position, rot)
            : Instantiate(_arrowPrefab, _firePoint.position, rot);

        if (arrow.TryGetComponent<Rigidbody2D>(out var rb))
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = aimDir * _arrowSpeed;
            rb.WakeUp(); // just in case it spawned asleep
#else
            rb.velocity = aimDir * _arrowSpeed;
#endif
            // Debug to confirm
            Debug.Log($"[CombatHandler] Shot -> aimDir={aimDir}, vel={rb.linearVelocity}");
        }
        else
        {
            Debug.LogWarning("[CombatHandler] Arrow has no Rigidbody2D.");
        }

        _animator?.SetTrigger("Bow");
    }

    private IEnumerator EnableHitbox(GameObject hitbox, float duration)
    {
        hitbox.SetActive(true);
        yield return new WaitForSeconds(duration);
        hitbox.SetActive(false);
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
}
