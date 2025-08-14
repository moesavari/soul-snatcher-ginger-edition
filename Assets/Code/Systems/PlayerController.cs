using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 4f;

    private Vector2 _moveInput;
    private Rigidbody2D _rb;
    private Animator _animator;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        transform.localScale = new Vector3(0.2f, 0.2f, 1f);
    }

    private void Update()
    {
        _moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        if (_moveInput.x != 0f)
            transform.localScale = new Vector3(0.2f * Mathf.Sign(_moveInput.x), 0.2f, 1f);

        _animator?.SetFloat("MoveX", _moveInput.x);
        _animator?.SetFloat("MoveY", _moveInput.y);
        _animator?.SetBool("IsMoving", _moveInput != Vector2.zero);
    }

    private void FixedUpdate()
    {
        _rb.MovePosition(_rb.position + _moveInput * _moveSpeed * Time.fixedDeltaTime);
    }
}
