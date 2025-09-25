using UnityEngine;

public class RangedWeapon : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject _arrowPrefab;
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _cooldown = 0.8f;
    [SerializeField] private float _artForwardOffsetDeg = 0f; // -90 if art faces +Y

    [SerializeField] private MouseFacing2D _facing;
    [SerializeField] private Animator _anim;

    private float _timer;

    private void OnEnable() { InputManager.RangedPressed += Shoot; }
    private void OnDisable() { InputManager.RangedPressed -= Shoot; }

    private void Update() => _timer -= Time.deltaTime;

    public void Shoot()
    {
        if (_timer > 0f || _arrowPrefab == null || _firePoint == null) return;
        _timer = _cooldown;

        Vector2 dir = _facing.AimDir;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + _artForwardOffsetDeg;
        var rot = Quaternion.Euler(0, 0, ang);

        var go = (SpawnManager.Instance != null)
            ? SpawnManager.Instance.Spawn(_arrowPrefab, _firePoint.position, rot)
            : Object.Instantiate(_arrowPrefab, _firePoint.position, rot);

        if (go.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = dir * _speed; rb.WakeUp();
        }

        //_anim?.SetTrigger("Bow");
    }
}
