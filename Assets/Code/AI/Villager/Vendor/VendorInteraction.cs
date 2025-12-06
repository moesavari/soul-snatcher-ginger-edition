using UnityEngine;

[RequireComponent(typeof(Vendor))]
public class VendorInteraction : MonoBehaviour
{
    [SerializeField] private float _openCooldown = 0.2f;

    private Vendor _vendor;
    private float _cooldownUntil;

    private void Awake()
    {
        _vendor = GetComponent<Vendor>();
    }

    private void OnEnable()
    {
        InputManager.InteractPressed += OnInteract;
    }
    private void OnDisable()
    {
        InputManager.InteractPressed -= OnInteract;
    }

    private void OnInteract()
    {
        if (Time.time < _cooldownUntil) return;
        if (!_vendor || !_vendor.isAlive || _vendor.isHiding) return;

        // Optional: require player to be near (uses same radius as villagers)
        var pc = PlayerContext.Instance?.facade;
        if (pc)
        {
            var d2 = (pc.transform.position - transform.position).sqrMagnitude;
            if (d2 > 1.5f * 1.5f) return;
        }

        _cooldownUntil = Time.time + _openCooldown;
        _vendor.OpenShop();
    }
}
