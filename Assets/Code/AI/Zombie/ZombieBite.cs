using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ZombieBite : MonoBehaviour
{
    [Header("Base Values")]
    [SerializeField] private int _damagePerTick = 1;
    [SerializeField] private float _tickInterval = 0.6f;
    [SerializeField] private string _targetTag = "Player"; 

    [Header("Enrage/Burn")]
    [SerializeField] private float _enrageDamageMult = 1.25f;
    [SerializeField] private float _enrageRateMult = 1.20f;

    private bool _isBiting;
    private Coroutine _biteRoutine;
    private bool _isEnraged;

    private int _baseDamagePerTick;
    private float _baseTickInterval;

    // Link the attacker! (The zombie's Stats component)
    private Stats _ownerStats;

    public void SetOwner(Stats owner) => _ownerStats = owner;

    public bool IsBiting => _isBiting;
    public bool IsEnraged => _isEnraged;

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
            _tickInterval = Mathf.Max(0.05f, _baseTickInterval * _enrageRateMult);
        }
        else
        {
            _damagePerTick = _baseDamagePerTick;
            _tickInterval = _baseTickInterval;
        }
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

    private IEnumerator BiteLoop(Collider2D targetCol)
    {
        _isBiting = true;
        while (_isBiting && targetCol != null)
        {
            Transform root = targetCol.attachedRigidbody ? targetCol.attachedRigidbody.transform : targetCol.transform.root;
            Stats defenderStats = root.GetComponent<Stats>();
            if (defenderStats != null && _ownerStats != null)
            {
                int damage = DamageCalculator.CalculateDamage(
                    attackerLevel: _ownerStats.Level,
                    power: _damagePerTick,
                    attackStat: _ownerStats.AttackPower,
                    defenderArmor: defenderStats.Armor,
                    critChance: _ownerStats.CritChance
                );
                defenderStats.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning("ZombieBite: Target has no Stats component on root.");
                _isBiting = false;
                break;
            }
            yield return new WaitForSeconds(_tickInterval);
        }
        _biteRoutine = null;
    }

    private bool IsTarget(Collider2D col)
    {
        Transform root = col.attachedRigidbody ? col.attachedRigidbody.transform : col.transform.root;
        return root.CompareTag(_targetTag);
    }
}
