using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 4f;

    private Vector2 _moveInput;
    private Rigidbody2D _rb;
    private Animator _animator;
    private Camera _cam;

    private void Awake()
    {
        _rb = this.Require<Rigidbody2D>();
        _animator = this.Require<Animator>();
        _cam = Camera.main;

        transform.localScale = new Vector3(0.2f, 0.2f, 1f);
    }

    private void Update()
    {
        _moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        if (_moveInput.x != 0f)
            transform.localScale = new Vector3(0.2f * Mathf.Sign(_moveInput.x), 0.2f, 1f);
        else
        {
            Vector3 mouseWorld = _cam.ScreenToViewportPoint(Input.mousePosition);
            float dirX = Mathf.Sign(mouseWorld.x - transform.position.x);
            if (!float.IsNaN(dirX) && !float.IsInfinity(dirX))
                transform.localScale = new Vector3(0.2f * dirX, 0.2f, 1f);
        }

        _animator?.SetFloat("MoveX", _moveInput.x);
        _animator?.SetFloat("MoveY", _moveInput.y);
        _animator?.SetBool("IsMoving", _moveInput != Vector2.zero);
    }

    private void FixedUpdate()
    {
        _rb.MovePosition(_rb.position + _moveInput * _moveSpeed * Time.fixedDeltaTime);
    }
}
