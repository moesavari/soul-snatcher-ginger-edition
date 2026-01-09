using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoSingleton<GameManager>
{
    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private float _restartDelaySeconds = 3f;

    [SerializeField] private WaveManager _waveManager;

    private GameObject _player;
    public GameObject player => _player;

    private bool _isRestarting;
    private bool _pendingPostLoadReset;

    private static bool _globalRestartLock;

    protected override void Awake()
    {
        base.Awake();

        SceneManager.sceneLoaded += OnSceneLoaded;

        GameEvents.DayStarted += OnDayStarted;
        GameEvents.NightStarted += OnNightStarted;

        SpawnOrFindPlayer();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        GameEvents.DayStarted -= OnDayStarted;
        GameEvents.NightStarted -= OnNightStarted;
    }

    private void OnEnable()
    {
        GameEvents.RoundLost += OnRoundLost;
    }

    private void OnDisable()
    {
        GameEvents.RoundLost -= OnRoundLost;
    }

    private void OnRoundLost()
    {
        if (_globalRestartLock) return;
        _globalRestartLock = true;

        if (_isRestarting)
        {
            DebugManager.LogWarning("GameManager: RoundLost received while already restarting. Ignoring.", this);
            return;
        }

        _isRestarting = true;

        // Freeze time cycle immediately so it can't transition mid-delay.
        TimeCycleManager.Instance?.StopCycle();

        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        _waveManager ??= FindFirstObjectByType<WaveManager>();
        _waveManager?.StopNight();
        _waveManager?.ClearAllSpawns();

        yield return new WaitForSeconds(_restartDelaySeconds);

        _pendingPostLoadReset = true;

        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_pendingPostLoadReset) return;
        _pendingPostLoadReset = false;

        RebindSceneReferences();

        // Reset EVERYTHING to Day 1 (this restarts the cycle + raises DayStarted).
        NightDirector.Instance?.ResetToDay1();
        TimeCycleManager.Instance?.ResetToDay1();

        SpawnOrFindPlayer();

        _globalRestartLock = false;
        _isRestarting = false;
    }

    private void RebindSceneReferences()
    {
        _waveManager ??= FindFirstObjectByType<WaveManager>();

        if (_playerSpawnPoint == null)
        {
            // Prefer a tagged spawn, fallback to a name if needed.
            var spawnGo = GameObject.FindWithTag("PlayerSpawn");
            if (spawnGo == null) spawnGo = GameObject.Find("PlayerSpawnPoint");
            if (spawnGo != null) _playerSpawnPoint = spawnGo.transform;
        }

        // If your overlay is scene UI, it must rebind after reload.
        NightOverlay.Instance?.TryRebindOverlay();
    }

    private void SpawnOrFindPlayer()
    {
        _player = GameObject.FindWithTag("Player");
        if (_player == null)
        {
            if (_playerPrefab == null || _playerSpawnPoint == null)
            {
                DebugManager.LogWarning("GameManager: Missing player prefab or spawn point.", this);
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
}
