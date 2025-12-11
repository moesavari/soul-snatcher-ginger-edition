using UnityEngine;

[RequireComponent(typeof(Villager))]
public class VillagerInteraction : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float _interactRadius = 1.5f;
    [SerializeField] private LayerMask _playerMask;
    [SerializeField] private float _interactCooldown = 0.35f;

    [Header("UI Prompt (optional)")]
    [SerializeField] private GameObject _promptRoot;

    private float _cooldownUntil = 0f;

    private Villager _villager;
    private bool _playerInRange;

    private void Awake()
    {
        _villager = GetComponent<Villager>();
    }

    private void OnEnable()
    {
        InputManager.InteractPressed += OnRescuePressed;
        InputManager.SiphonPressed += OnSiphonPressed;
    }

    private void OnDisable()
    {
        InputManager.InteractPressed -= OnRescuePressed;
        InputManager.SiphonPressed -= OnSiphonPressed;
    }

    private void Update()
    {
        _playerInRange = Physics2D.OverlapCircle(transform.position, _interactRadius, _playerMask) != null;
        if (_promptRoot) _promptRoot.SetActive(_playerInRange && _villager.isAlive && !_villager.isHiding);
    }

    private bool CanAct() => _playerInRange && _villager.isAlive && Time.time >= _cooldownUntil && !_villager.isHiding;

    private void OnRescuePressed()
    {
        if (GetComponent<Vendor>()) return;

        if (!_playerInRange) return;
        if (_villager.isHiding) return;
        _villager.OnRescued();
        _cooldownUntil = Time.time + _interactCooldown;
    }

    private void OnSiphonPressed()
    {
        if (GetComponent<Vendor>()) return;

        if (!_playerInRange) return;
        if (_villager.isHiding) return;
        _villager.OnSoulAbsorb();
        _cooldownUntil = Time.time + _interactCooldown;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _interactRadius);
    }
#endif
}
