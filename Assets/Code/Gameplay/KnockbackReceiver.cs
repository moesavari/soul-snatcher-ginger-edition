using UnityEngine;

public class KnockbackReceiver : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private float _drag = 10f;
    private Vector2 _vel;
    private float _stunUntil;

    public bool isStunned => Time.time < _stunUntil;

    private void Awake() { if (!_rb) _rb = GetComponentInParent<Rigidbody2D>(); }
    private void Update()
    {
        if (!isStunned) return;
        _vel = Vector2.MoveTowards(_vel, Vector2.zero, _drag * Time.deltaTime);

        if (_rb && _rb.bodyType == RigidbodyType2D.Dynamic)
            _rb.linearVelocity = _vel;
    }

    public void Apply(Vector2 impulse, float stunTime)
    {
        _stunUntil = Mathf.Max(_stunUntil, Time.time + stunTime);
        _vel = impulse;

        if (_rb && _rb.bodyType == RigidbodyType2D.Dynamic)
            _rb.linearVelocity = impulse;
    }

}
