using System.Collections;
using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{
    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private float _restartDelaySeconds = 10f;

    private GameObject _player;
    public GameObject player => _player;

    protected override void Awake()
    {
        base.Awake();

        SpawnOrFindPlayer();
        GameEvents.DayStarted += OnDayStarted;
        GameEvents.NightStarted += OnNightStarted;
        GameEvents.PlayerDied += OnPlayerDied;
        GameEvents.AllZombiesCleared += OnAllZombiesCleared;
    }

    private void OnDestroy()
    {
        GameEvents.DayStarted -= OnDayStarted;
        GameEvents.NightStarted -= OnNightStarted;
        GameEvents.PlayerDied -= OnPlayerDied;
        GameEvents.AllZombiesCleared -= OnAllZombiesCleared;
    }

    private void SpawnOrFindPlayer()
    {
        _player = GameObject.FindWithTag("Player");
        if (_player == null)
        {
            if (_playerPrefab == null || _playerSpawnPoint == null)
            {
                Debug.LogWarning("[GameManager] Missing Player prefab or spawn point.");
                return;
            }
            _player = SpawnManager.Instance.Spawn(_playerPrefab, _playerSpawnPoint.position, Quaternion.identity);
        }

        FindFirstObjectByType<CameraFollow2D>()?.SetTarget(_player.transform);

        GameEvents.RaisePlayerSpawned(_player);
    }


    private void OnDayStarted()
    {
        Debug.Log("[GameManager] Day started.");
    }

    private void OnNightStarted()
    {
        Debug.Log("[GameManager] Night started.");
    }

    private void OnPlayerDied()
    {
        Debug.Log("[GameManager] Player died. Restarting Scene...");
        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(_restartDelaySeconds);
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
    }

    private void OnAllZombiesCleared()
    {
        Debug.Log("[GameManager] Allzombies cleared. Forcing day.");
        TimeCycleManager.Instance?.ForceDay();
    }
}
