using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 4f;
    [SerializeField] private Animator _animator;
    [SerializeField] private Rigidbody2D _rb;

    private Vector2 _moveInput;

    private void OnEnable() { InputManager.Move += SetMoveInput; }
    private void OnDisable() { InputManager.Move -= SetMoveInput; }

    private void FixedUpdate()
    {
        _rb.MovePosition(_rb.position + _moveInput * _moveSpeed * Time.fixedDeltaTime);
    }

    public void SetMoveInput(Vector2 v) { _moveInput = v; }

}
