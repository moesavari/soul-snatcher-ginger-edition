using UnityEngine;

[DisallowMultipleComponent]

public class MouseFacing2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform _aimRig;        // assign AimRig (sibling of Visual)
    [SerializeField] private Transform _firePoint;     // child of AimRig, forward = +X
    [SerializeField] private SpriteRenderer _visual;   // Visual's SpriteRenderer (sibling)
    [SerializeField] private Transform _fallback;      // optional; defaults to transform

    [Header("Debug")]
    [SerializeField] private bool _warnOnMirror = true;

    private Camera _cam;
    private Vector2 _aimDir = Vector2.right;

    public Vector2 AimDir => _aimDir;

    private void Awake()
    {
        _cam = Camera.main;
        if (_fallback == null) _fallback = transform;

        // Enforce no negative scale above AimRig
        if (_aimRig == null) _aimRig = transform;
        ForcePositiveScaleChain(_aimRig);
    }

    private void Update()
    {
        if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }

        // 1) compute aim strictly from mouse
        Vector3 src = _firePoint ? _firePoint.position : _fallback.position;
        Vector3 mw = _cam.ScreenToWorldPoint(Input.mousePosition);
        mw.z = src.z;
        Vector2 dir = (Vector2)(mw - src);
        if (dir.sqrMagnitude < 0.000001f) return;

        _aimDir = dir.normalized;

        // 2) rotate AimRig to point its +X toward mouse
        if (_aimRig != null)
        {
            float ang = Mathf.Atan2(_aimDir.y, _aimDir.x) * Mathf.Rad2Deg;
            _aimRig.SetPositionAndRotation(_aimRig.position, Quaternion.Euler(0, 0, ang));
        }

        // 3) flip ONLY the sprite; never scale/flip parents
        if (_visual != null)
            _visual.flipX = (_aimDir.x < 0f);

#if UNITY_EDITOR
        if (_warnOnMirror) WarnIfNegativeScaleInChain(_aimRig);
#endif
    }

    private static void ForcePositiveScaleChain(Transform t)
    {
        for (var p = t; p != null; p = p.parent)
        {
            Vector3 s = p.localScale;
            if (s.x < 0f || s.y < 0f || s.z < 0f)
                p.localScale = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        }
    }

#if UNITY_EDITOR
    private static void WarnIfNegativeScaleInChain(Transform t)
    {
        for (var p = t; p != null; p = p.parent)
        {
            Vector3 s = p.localScale;
            if (s.x < 0f || s.y < 0f || s.z < 0f)
            {
                DebugManager.LogWarning($"[MouseFacing2D] Negative scale on '{p.name}'. " +
                                 "This mirrors child space and causes hitbox side swaps.");
                break;
            }
        }
    }
#endif

    private void OnDrawGizmosSelected()
    {
        if (_firePoint == null || _cam == null) return;
        var mw = _cam.ScreenToWorldPoint(Input.mousePosition); mw.z = _firePoint.position.z;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(_firePoint.position, mw);
    }
}
