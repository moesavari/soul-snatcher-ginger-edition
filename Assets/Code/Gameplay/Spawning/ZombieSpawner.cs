using System.Collections;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _zombiePrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _spawnInterval = 3f;
    [SerializeField] private int _maxAlive = 25;
    [SerializeField] private AudioCue _spawnCue;

    private int _aliveCount;
    private Coroutine _spawnRoutine;

    private void OnEnable()
    {
        TimeCycleManager.OnNightStarted += OnNightStarted;
        TimeCycleManager.OnDayStarted += OnDayStarted;
    }

    private void OnDisable()
    {
        TimeCycleManager.OnNightStarted -= OnNightStarted;
        TimeCycleManager.OnDayStarted -= OnDayStarted;

        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
    }

    private void OnDayStarted()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
    }

    private void OnNightStarted()
    {
        if (_spawnRoutine == null)
            _spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        float delay = TimeCycleManager.Instance != null ? TimeCycleManager.Instance.nightSpawnDelay : 3f;
        if (delay > 0f) yield return new WaitForSeconds(delay);

        while (TimeCycleManager.Instance != null && TimeCycleManager.Instance.isNight)
        {
            if (_aliveCount < _maxAlive)
                SpawnOne();

            yield return new WaitForSeconds(_spawnInterval);
        }

        _spawnRoutine = null;
    }

    private void SpawnOne()
    {
        if (_zombiePrefab == null || _spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogWarning("[ZombieSpawner] Missing prefab or spawn points.");
            return;
        }

        Transform point = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
        var zombie = SpawnManager.Instance.Spawn(_zombiePrefab, point.position, Quaternion.identity);

        if (_spawnCue != null)
            AudioManager.Instance.PlayCue(_spawnCue, worldPos: point.position);

        if (zombie != null && zombie.TryGetComponent<Zombie>(out var z))
        {
            z.RegisterSpawner(this);
            NotifyZombieSpawned();
        }
    }

    public void NotifyZombieDied() { _aliveCount = Mathf.Max(0, _aliveCount - 1); }
    public void NotifyZombieSpawned() { _aliveCount++; }
}
