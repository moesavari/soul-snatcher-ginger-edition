using UnityEngine;

[RequireComponent(typeof(MouseFacing2D))]

public class CombatInput : MonoBehaviour
{
    [SerializeField] private MeleeWeapon _melee;
    [SerializeField] private RangedWeapon _ranged;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z)) _melee?.Attack();
        if (Input.GetKeyDown(KeyCode.X)) _ranged.Shoot();
    }
}
