using UnityEngine;

[DisallowMultipleComponent]
public class DeathFeedback : MonoBehaviour
{
    [SerializeField] private GameObject _vfxPrefab;
    [SerializeField] private float _vfxLifetime = 1.5f;
    [SerializeField] private AudioCue _deathCue;
    [SerializeField] private bool _attachToBody = false;
    
    private Stats _stats;

    private void Awake()
    {
        _stats = GetComponent<Stats>();
    }

    private void OnEnable()
    {
        if (_stats.tag == "Player") _stats.GetComponent<PlayerController>().OnDeath += PlayFeedback;
        if (_stats.tag == "Enemy") _stats.GetComponent<Zombie>().OnDeath += PlayFeedback;
    }

    private void OnDisable()
    {
        if (_stats.tag == "Player") _stats.GetComponent<PlayerController>().OnDeath -= PlayFeedback;
        if (_stats.tag == "Enemy") _stats.GetComponent<Zombie>().OnDeath -= PlayFeedback;
    }

    private void PlayFeedback()
    {
        if (_deathCue != null)
            AudioManager.Instance.PlayCue(_deathCue, worldPos: transform.position);

        if (_vfxPrefab != null)
        {
            var vfx = Instantiate(_vfxPrefab, transform.position, Quaternion.identity);
            if (_attachToBody) vfx.transform.SetParent(transform, true);
            Destroy(vfx, _vfxLifetime);
        }
    }
}
