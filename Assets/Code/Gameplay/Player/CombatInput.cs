using UnityEngine;

[RequireComponent(typeof(MouseFacing2D))]

public class CombatInput : MonoBehaviour
{
    [SerializeField] private MeleeWeapon _melee;
    [SerializeField] private RangedWeapon _ranged;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) _melee?.Attack();
        if (Input.GetMouseButtonDown(1)) _ranged.Shoot();
    }
}
