using Game.Core.Inventory;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerFacade : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerController _controller;
    [SerializeField] private CombatInput _combat;
    [SerializeField] private Health _health;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private Equipment _equipment;
    [SerializeField] private CharacterStats _stats;
    [SerializeField] private MeleeWeapon _melee;
    [SerializeField] private RangedWeapon _ranged;
    [SerializeField] private Transform _aimRig;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Collider2D _meleeHitbox;

    public PlayerController controller => _controller;
    public CombatInput combat => _combat;
    public Health health => _health;
    public Inventory inventory => _inventory;
    public Equipment equipment => _equipment;
    public CharacterStats stats => _stats;
    public MeleeWeapon melee => _melee;
    public RangedWeapon ranged => _ranged;
    public Transform aimRig => _aimRig;
    public Transform firePoint => _firePoint;
    public Collider2D meleeHitbox => _meleeHitbox;
}