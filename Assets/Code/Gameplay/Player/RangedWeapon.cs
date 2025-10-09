using UnityEngine;

public class RangedWeapon : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject _arrowPrefab;
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _cooldown = 0.8f;
    [SerializeField] private float _artForwardOffsetDeg = 0f;

    [Header("Mouse Settings")]
    [SerializeField] private MouseFacing2D _facing;
    [SerializeField] private Animator _anim;

    [Header("Movement Settings")]
    [SerializeField] private float _shootRootSeconds = 0.18f;
    private float _shootRootTimer;

    [Header("Stat References")]
    [SerializeField] private Stats _owner; 
    [SerializeField] private int _power = 8;

    public bool isShooting => _shootRootTimer > 0f;

    private float _timer;

    private void OnEnable() { InputManager.RangedPressed += Shoot; }
    private void OnDisable() { InputManager.RangedPressed -= Shoot; }

    private void Update()
    {
        if (_timer > 0f) _timer -= Time.deltaTime;
        if (_shootRootTimer > 0f) _shootRootTimer -= Time.deltaTime;
    }

    public void SetOwner(Stats owner)
    {
        _owner = owner;
    }

    public void Shoot()
    {
        if (_timer > 0f) return;
        _timer = _cooldown;

        // NEW: brief lock + root window
        _shootRootTimer = _shootRootSeconds;
        if (_facing != null) _facing.SetAimLocked(true);
        if (_shootRootSeconds > 0f) Invoke(nameof(ReleaseAimLock), _shootRootSeconds);

        Vector2 dir = (_firePoint.right).normalized;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + _artForwardOffsetDeg;
        var rot = Quaternion.Euler(0, 0, ang);

        var go = (SpawnManager.Instance != null)
            ? SpawnManager.Instance.Spawn(_arrowPrefab, _firePoint.position, rot)
            : Instantiate(_arrowPrefab, _firePoint.position, rot);

        if (go.TryGetComponent(out Arrow proj))
        {
            proj.SetAttacker(_owner, _power);
        }

        if (go.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = dir * _speed; rb.WakeUp();
        }

        //_anim?.SetTrigger("Bow");
    }

    private void ReleaseAimLock()
    {
        if (_facing != null) _facing.SetAimLocked(false);
    }
}
