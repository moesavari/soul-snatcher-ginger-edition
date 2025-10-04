using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ZombieBite : MonoBehaviour
{
    [Header("Base values")]
    [SerializeField] private int _damagePerTick = 1;
    [SerializeField] private float _tickInterval = 0.6f;
    [SerializeField] private string _targetTag = "Player"; // who we can bite

    [Header("Sunrise Enraged-Burning")]
    [SerializeField] private float _enrageDamageMult = 1.25f;
    [SerializeField] private float _enrageRateMult = 1.20f;

    private bool _isBiting;
    private Coroutine _biteRoutine;

    private bool _isEnraged;
    private int _baseDamagePerTick;
    private float _baseTickInterval;

    public bool isBiting => _isBiting;
    public bool isEnraged => _isEnraged;

    private void Reset()
    {
        // Ensure trigger
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        InitBaseValues();
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!IsTarget(col)) return;
        if (_isBiting) return;

        _biteRoutine = StartCoroutine(BiteLoop(col));
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (!IsTarget(col)) return;
        _isBiting = false;
        if (_biteRoutine != null)
        {
            StopCoroutine(_biteRoutine);
            _biteRoutine = null;
        }
    }

    private void OnDisable()
    {
        _isBiting = false;
        if (_biteRoutine != null)
        {
            StopCoroutine(_biteRoutine);
            _biteRoutine = null;
        }
    }

    public void InitBaseValues()
    {
        _baseDamagePerTick = _damagePerTick;
        _baseTickInterval = _tickInterval;
    }

    public void SetEnraged(bool value)
    {
        _isEnraged = value;

        if (_isEnraged)
        {
            _damagePerTick = Mathf.Max(1, Mathf.RoundToInt(_baseDamagePerTick * _enrageDamageMult));
            _tickInterval = Mathf.Max(0.05f, _baseTickInterval / _enrageRateMult);
        }
        else
        {
            _damagePerTick = _baseDamagePerTick;
            _tickInterval = _baseTickInterval;
        }
    }

    private IEnumerator BiteLoop(Collider2D targetCol)
    {
        _isBiting = true;

        while (_isBiting && targetCol != null)
        {
            Transform root = targetCol.attachedRigidbody
                ? targetCol.attachedRigidbody.transform
                : targetCol.transform.root;

            if (root.TryGetComponent<Health>(out var hp))
            {
                hp.TakeDamage(_damagePerTick, transform.position, gameObject);
            }
            else
            {
                DebugManager.LogWarning("Target has no Health component on root.", this);
                _isBiting = false;
                break;
            }

            yield return new WaitForSeconds(_tickInterval);
        }


        _biteRoutine = null;
    }

    private bool IsTarget(Collider2D col)
    {
        // Prefer the rigidbody root (true “body” of the thing we hit)
        var root = col.attachedRigidbody ? col.attachedRigidbody.transform : col.transform.root;
        return root.CompareTag(_targetTag);
    }
}
