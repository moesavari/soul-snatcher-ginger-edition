using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 4f;

    private Vector2 _moveInput;
    private Rigidbody2D _rb;
    private Animator _animator;

    private void Awake()
    {
        _rb = this.Require<Rigidbody2D>();
        _animator = this.Require<Animator>();
    }

    private void Update()
    {
        _moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        _animator?.SetFloat("MoveX", _moveInput.x);
        _animator?.SetFloat("MoveY", _moveInput.y);
        _animator?.SetBool("IsMoving", _moveInput != Vector2.zero);
    }

    private void FixedUpdate()
    {
        _rb.MovePosition(_rb.position + _moveInput * _moveSpeed * Time.fixedDeltaTime);
    }
}
