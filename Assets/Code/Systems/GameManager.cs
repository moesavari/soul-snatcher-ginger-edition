using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{
    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private GameObject _playerPrefab;

    private GameObject _player;
    public GameObject player => _player;

    protected override void Awake()
    {
        base.Awake();

        SpawnOrFindPlayer();
        TimeCycleManager.OnDayStarted += OnDayStarted;
        TimeCycleManager.OnNightStarted += OnNightStarted;
    }

    private void OnDestroy()
    {
        TimeCycleManager.OnDayStarted -= OnDayStarted;
        TimeCycleManager.OnNightStarted -= OnNightStarted;
    }
    private void SpawnOrFindPlayer()
    {
        _player = GameObject.FindWithTag("Player");
        if (_player != null) return;

        if(_playerPrefab == null || _playerSpawnPoint == null)
        {
            Debug.LogWarning("[GameManager] Missing Player prefab or spawn point.");
            return;
        }

        _player = Instantiate(_playerPrefab, _playerSpawnPoint.position, Quaternion.identity);
    }

    private void OnDayStarted()
    {
        Debug.Log("[GameManager] Day started.");
    }

    private void OnNightStarted()
    {
        Debug.Log("[GameManager] Night started.");
    }

}
