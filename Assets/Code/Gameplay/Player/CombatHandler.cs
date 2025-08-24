using UnityEngine;
using System.Collections;

public class CombatHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _meleeHitBox;
    [SerializeField] private GameObject _arrowPrefab;
    [SerializeField] private Transform _firePoint;

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

        if (_firePoint == null)
            Debug.LogWarning("[CombatHandler] _firePoint is not assigned.");
    }

    private void Update()
    {
        _meleeTimer -= Time.deltaTime;
        _rangedTimer -= Time.deltaTime;

        Vector2 aimDir = GetAimDir();
        RotateAimers(aimDir);

        if (Input.GetKeyDown(KeyCode.Z) && _meleeTimer <= 0f)
        {
            DoMeleeAttack();
            _meleeTimer = _meleeCooldown;
        }

        if (Input.GetKeyDown(KeyCode.X) && _rangedTimer <= 0f)
        {
            DoRangedAttack();
            _rangedTimer = _rangedCooldown;
        }
    }

    private Vector2 GetAimDir()
    {
        if (_firePoint == null)
            return Vector2.right;

        if(_cam == null) _cam = Camera.main;

        Vector3 mouseWorld = _cam != null
            ? _cam.ScreenToWorldPoint(Input.mousePosition)
            : new Vector3(_firePoint.position.x + 1f, _firePoint.position.y, _firePoint.position.z);

        mouseWorld.z = _firePoint.position.z;

        Vector2 dir = (mouseWorld - _firePoint.position);
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
    }

    private void RotateAimers(Vector2 aimDir)
    {
        if(_firePoint != null)
            _firePoint.right = aimDir;

        if(_meleeHitBox != null)
            _meleeHitBox.transform.right = aimDir;
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

    private void DoRangedAttack()
    {
        if (_arrowPrefab == null || _firePoint == null)
        {
            Debug.LogWarning("[CombatHandler] Missing arrowPrefab or firePoint prefabs");
            return;
        }

        var arrow = (SpawnManager.Instance != null)
            ? SpawnManager.Instance.Spawn(_arrowPrefab, _firePoint.position, _firePoint.rotation)
            : Instantiate(_arrowPrefab, _firePoint.position, _firePoint.rotation);
        
        if (arrow.TryGetComponent<Rigidbody2D>(out var rb))
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = _firePoint.right * _arrowSpeed;
#else
            rb.velocity = aimDir * _arrowSpeed;
#endif            
        _animator?.SetTrigger("Bow");
    }

    private IEnumerator EnableHitbox(GameObject hitbox, float duration)
    {
        hitbox.SetActive(true);
        yield return new WaitForSeconds(duration);
        hitbox.SetActive(false);
    }
}
