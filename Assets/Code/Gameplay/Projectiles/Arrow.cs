using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] private int _damage = 1;
    [SerializeField] private float _lifeSeconds = 4f;
    [SerializeField] private string _ownerTag = "Player";

    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = this.Require<Rigidbody2D>();
        Destroy(gameObject, _lifeSeconds);
    }

    private void LateUpdate()
    {
        if (_rb != null) return;
#if UNITY_6000_0_OR_NEWER
        Vector2 v = _rb.linearVelocity;
#else
        Vector2 v = _rb.velocity;
#endif
        if (v.sqrMagnitude > 0.0001f)
            transform.right = v.normalized;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!string.IsNullOrEmpty(_ownerTag) && collision.CompareTag(_ownerTag)) return;

        if (collision.TryGetComponent<Health>(out var hp))
            hp.TakeDamage(_damage, transform.position, gameObject);
        
        Destroy(gameObject);
    }
}
