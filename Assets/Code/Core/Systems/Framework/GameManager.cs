using System.Collections;
using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{
    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private float _restartDelaySeconds = 10f;

    [SerializeField] private WaveManager _waveManager;

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

    private void OnEnable() => GameEvents.RoundLost += OnRoundLost;
    private void OnDisable() => GameEvents.RoundLost -= OnRoundLost;

    private void OnDestroy()
    {
        GameEvents.DayStarted -= OnDayStarted;
        GameEvents.NightStarted -= OnNightStarted;
        GameEvents.PlayerDied -= OnPlayerDied;
        GameEvents.AllZombiesCleared -= OnAllZombiesCleared;
    }

    private void OnRoundLost()
    {
        StartCoroutine(RestartAfterDelay());
    }

    private void SpawnOrFindPlayer()
    {
        _player = GameObject.FindWithTag("Player");
        if (_player == null)
        {
            if (_playerPrefab == null || _playerSpawnPoint == null)
            {
                DebugManager.LogWarning("Missing Player prefab or spawn point.", this);
                return;
            }
            _player = SpawnManager.Instance.Spawn(_playerPrefab, _playerSpawnPoint.position, Quaternion.identity);
        }

        FindFirstObjectByType<CameraFollow2D>()?.SetTarget(_player.transform);

        GameEvents.RaisePlayerSpawned(_player);
    }

    private void OnDayStarted()
    {
        DebugManager.Log("Day started.", this);
    }

    private void OnNightStarted()
    {
        DebugManager.Log("Night started.", this);
    }

    private void OnPlayerDied()
    {
        DebugManager.Log("Player died. Restarting Scene...", this);
        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        _waveManager?.StopNight();
        _waveManager?.ClearAllSpawns();

        yield return new WaitForSeconds(_restartDelaySeconds);
        
        SpawnOrFindPlayer();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
    }

    private void OnAllZombiesCleared()
    {
        DebugManager.Log("Allzombies cleared. Forcing day.", this);
        TimeCycleManager.Instance?.ForceDay();
    }
}
