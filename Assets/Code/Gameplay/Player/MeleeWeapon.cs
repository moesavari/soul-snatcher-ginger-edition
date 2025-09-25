using System.Collections;
using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    [SerializeField] private GameObject _hitbox;     // child of FirePoint
    [SerializeField] private float _activeSeconds = 0.15f;
    [SerializeField] private float _cooldown = 0.45f;

    [SerializeField] private MouseFacing2D _facing;
    [SerializeField] private Animator _anim;

    private float _timer;
    private void OnEnable() { InputManager.MeleePressed += Attack; }
    private void OnDisable() { InputManager.MeleePressed -= Attack; }

    private void Update() => _timer -= Time.deltaTime;

    public void Attack()
    {
        if (_timer > 0f) return;
        _timer = _cooldown;

        // hitbox already sits in front via FirePoint/AimRig; just enable
        //_anim?.SetTrigger("Melee");
        StartCoroutine(Swing());
    }

    private IEnumerator Swing()
    {
        if (_hitbox == null) yield break;

        if (_hitbox.TryGetComponent<MeleeHitbox>(out var mh)) mh.Arm();
        _hitbox.SetActive(true);
        yield return new WaitForSeconds(_activeSeconds);
        _hitbox.SetActive(false);
        if (mh != null) mh.Disarm();
    }
}
