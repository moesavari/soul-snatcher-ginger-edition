using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private Vector2 _offset = new Vector2(0f, 0f);
    [SerializeField] private float _smoothTime = 0.15f;
    [SerializeField] private float _zDepth = -10f;

    private Vector3 _vel;

    private void LateUpdate()
    {
        if (_target == null) return;

        Vector3 targetPos = _target.position + (Vector3)_offset;
        targetPos.z = _zDepth;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _vel, _smoothTime);
    }

    public void SetTarget(Transform t) => _target = t;
}
