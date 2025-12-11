using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    [Header("Swing")]
    [SerializeField] private float _swingAngle = 100f;
    [SerializeField] private float _swingDuration = 0.18f;
    [SerializeField] private float _cooldown = 0.45f;
    [SerializeField] private float _recoverTime = 0.08f;

    [Header("Arc Sampling")]
    [SerializeField] private float _range = 1.2f;
    [SerializeField] private float _arcRadius = 0.85f;
    [SerializeField] private int _samples = 5;
    [SerializeField] private LayerMask _hitMask;

    [Header("Damage / Knockback")]
    [SerializeField] private float _knockback = 6f;
    [SerializeField] private float _stunTime = 0.08f;
    [SerializeField] private string _ownerTag = "Player";

    [Header("Stat References")]
    [SerializeField] private Stats _owner;
    [SerializeField] private int _power = 10;

    [Header("Optional")]
    [SerializeField] private MouseFacing2D _facing;
    [SerializeField] private Animator _anim;
    [Tooltip("Optional sprite root (e.g., sword). If null, we'll rotate this Transform.")]
    [SerializeField] private Transform _weaponVisual;
    [SerializeField] private bool _hideWhenIdle = true;
    [SerializeField] private Transform _pivot;

    private CharacterStats _stats;

    private float _cooldownTimer;
    private bool _attacking;
    private readonly HashSet<Collider2D> _alreadyHit = new();
    private ContactFilter2D _hitFilter;
    private bool _filterInit;
    private Quaternion _baseLocalRot;

    private const int MaxHits = 16;
    private readonly Collider2D[] _hitBuf = new Collider2D[MaxHits];
    private Renderer[] _visualRenderers;

    private Transform pivot => _pivot ? _pivot : transform;

    public bool isAttacking => _attacking;

    private void Awake()
    {
        _stats = GetComponent<CharacterStats>();

        if (!_pivot) _pivot = transform;
        if (!_weaponVisual) _weaponVisual = transform;

        _baseLocalRot = _weaponVisual.localRotation;
        _visualRenderers = _weaponVisual.GetComponentsInChildren<Renderer>(true);
        if (_hideWhenIdle) SetVisual(false);
    }

    private void OnEnable() { InputManager.MeleePressed += Attack; }
    private void OnDisable() { InputManager.MeleePressed -= Attack; }

    private void Update()
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
    }

    public void SetOwner(Stats owner)
    {
        _owner = owner;
    }

    public void Attack()
    {
        if (_attacking || _cooldownTimer > 0f) return;
        _cooldownTimer = _cooldown + _recoverTime;

        StartCoroutine(SwingRoutine());
    }

    private int GetFacingSign()
    {
        if (_facing != null)
            return (_facing.AimDir.x >= 0f) ? 1 : -1;

        return (pivot.right.x >= 0f) ? 1 : -1;
    }

    private bool IsHierarchyFlipped()
    {
        var s = _weaponVisual.lossyScale;
        int negs = (s.x < 0 ? 1 : 0) + (s.y < 0 ? 1 : 0);
        return (negs % 2) == 1;
    }

    private IEnumerator SwingRoutine()
    {
        _attacking = true;
        _alreadyHit.Clear();

        if (_hideWhenIdle) SetVisual(true);

        bool hadFacing = _facing != null;
        if (hadFacing) _facing.SetAimLocked(true);

        float half = _swingAngle * 0.5f;
        float start = +half;
        float end = -half;

        int dirSign = GetFacingSign();

        if (IsHierarchyFlipped())
            dirSign *= -1;

        _weaponVisual.localRotation = _baseLocalRot * Quaternion.Euler(0f, 0f, start * dirSign);

        float stepTime = (_samples <= 1) ? _swingDuration : _swingDuration / (_samples - 1);
        for (int i = 0; i < _samples; i++)
        {
            float u = (_samples <= 1) ? 1f : (float)i / (_samples - 1);
            float angle = Mathf.Lerp(start, end, u) * dirSign;
            _weaponVisual.localRotation = _baseLocalRot * Quaternion.Euler(0f, 0f, angle);

            DoHitSample();

            if (i < _samples - 1)
                yield return new WaitForSeconds(stepTime);
        }

        if (hadFacing) _facing.SetAimLocked(false);

        _attacking = false;
        if (_hideWhenIdle) SetVisual(false);
        if (_recoverTime > 0f) yield return new WaitForSeconds(_recoverTime);
    }

    private void EnsureFilter()
    {
        if (_filterInit) return;
        _hitFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = _hitMask,
            useTriggers = true
        };
        _filterInit = true;
    }

    private void DoHitSample()
    {
        EnsureFilter();

        Vector2 center = pivot.position + pivot.right * _range;
        int count = Physics2D.OverlapCircle(center, _arcRadius, _hitFilter, _hitBuf);

        for (int i = 0; i < count; i++)
        {
            var c = _hitBuf[i];
            if (!c) continue;
            if (_alreadyHit.Contains(c)) continue;
            if (c.CompareTag(_ownerTag)) continue;

            _alreadyHit.Add(c);

            var target = c.GetComponentInParent<Stats>();
            if (target)
            {
                int damage = DamageCalculator.CalculateDamage(
                    attackerLevel: _owner.Level,
                    power: _power,
                    attackStat: _owner.AttackPower,
                    defenderArmor: target.Armor,
                    critChance: _owner.CritChance
                );
                target.TakeDamage(damage);
            }

            var kb = c.GetComponentInParent<KnockbackReceiver>();
            Vector2 dir = ((Vector2)c.transform.position - (Vector2)pivot.position).normalized;
            if (kb != null)
                kb.Apply(dir * _knockback, _stunTime);
            else
            {
                var rb = c.attachedRigidbody;
                if (rb && rb.bodyType == RigidbodyType2D.Dynamic)
                    rb.linearVelocity = dir * _knockback;
            }
        }
    }

    private void SetVisual(bool on)
    {
        if (_visualRenderers == null) return;
        for (int i = 0; i < _visualRenderers.Length; i++)
            _visualRenderers[i].enabled = on;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var p = pivot ? pivot : transform;

        Gizmos.color = Color.yellow;
        Vector3 center = p.position + p.right * _range;
        Gizmos.DrawWireSphere(center, _arcRadius);

        float half = _swingAngle * 0.5f;
        UnityEditor.Handles.color = new Color(1f, 0.9f, 0.1f, 0.25f);
        UnityEditor.Handles.DrawSolidArc(p.position, Vector3.forward,
            Quaternion.Euler(0, 0, -half) * p.right, _swingAngle, _range + _arcRadius * 0.5f);
    }
#endif
}
