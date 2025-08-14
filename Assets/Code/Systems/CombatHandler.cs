using UnityEngine;
using System.Collections;

public class CombatHandler : MonoBehaviour
{
    [SerializeField] private GameObject _meleeHitBox;
    [SerializeField] private float _meleeActiveSeconds = 0.15f;
    [SerializeField] private float _meleeCooldown = 0.45f;

    [SerializeField] private GameObject _arrowPrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _arrowSpeed = 10f;
    [SerializeField] private float _rangedCooldown = 0.8f;

    private float _meleeTimer;
    private float _rangedTimer;

    private void Update()
    {
        _meleeTimer -= Time.deltaTime;
        _rangedTimer -= Time.deltaTime;

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

    private void DoMeleeAttack()
    {
        if (_meleeHitBox == null)
        {
            Debug.LogWarning("[CombatHandler] Missing Melee hitbox.");
            return;
        }

        StartCoroutine(EnableHitbox(_meleeHitBox, _meleeActiveSeconds));
    }

    private void DoRangedAttack()
    {
        if (_arrowPrefab == null || _firePoint == null)
        {
            Debug.LogWarning("[CombatHandler] Missing arrowPrefab or firePoint prefabs");
            return;
        }

        var arrow = Instantiate(_arrowPrefab, _firePoint.position, _firePoint.rotation);
        if (arrow.TryGetComponent<Rigidbody2D>(out var rb))
            rb.velocity = _firePoint.right * _arrowSpeed;
    }
    private IEnumerator EnableHitbox(GameObject hitbox, float duration)
    {
        hitbox.SetActive(true);
        yield return new WaitForSeconds(duration);
        hitbox.SetActive(false);
    }
}
