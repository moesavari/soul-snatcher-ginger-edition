using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] private float _lifeSeconds = 4f;
    [SerializeField] private string _ownerTag = "Player";

    private Stats _attacker;
    private int _power;

    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = this.Require<Rigidbody2D>();
        Destroy(gameObject, _lifeSeconds);
    }

    private void LateUpdate()
    {
        if (_rb != null) return;

        Vector2 v = _rb.linearVelocity;

        if (v.sqrMagnitude > 0.0001f)
            transform.right = v.normalized;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!string.IsNullOrEmpty(_ownerTag) && collision.CompareTag(_ownerTag)) return;

        var target = collision.GetComponentInParent<Stats>();
        if (target != null && target != _attacker)
        {
            int damage = DamageCalculator.CalculateDamage(
                _attacker.Level, _power, _attacker.AttackPower,
                target.Armor, _attacker.CritChance
            );
            target.TakeDamage(damage);
        }
        Destroy(gameObject);
    }

    public void SetAttacker(Stats attacker, int power)
    {
        _attacker = attacker;
        _power = power;
    }
}
