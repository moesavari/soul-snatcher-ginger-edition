using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]

public class DeathFeedback : MonoBehaviour
{
    [SerializeField] private GameObject _vfxPrefab;
    [SerializeField] private float _vfxLifetime = 1.5f;
    [SerializeField] private AudioCue _deathCue;
    [SerializeField] private bool _attachToBody = false;

    private Health _health;

    private void Awake()
    {
        _health = this.Require<Health>();
    }

    private void OnEnable()
    {
        _health.OnDeath += OnDied;
    }

    private void OnDisable()
    {
        _health.OnDeath -= OnDied;
    }

    private void OnDied(Health h)
    {
        if (_deathCue != null)
            AudioManager.Instance.PlayCue(_deathCue, worldPos: transform.position);

        if(_vfxPrefab != null)
        {
            var vfx = Instantiate(_vfxPrefab, transform.position, Quaternion.identity);
            if (_attachToBody) vfx.transform.SetParent(transform, true);
            Destroy(vfx, _vfxLifetime);
        }
    }
}
