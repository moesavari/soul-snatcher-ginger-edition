using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private NightPreset _nightPreset;

    [Header("Spawn")]
    [SerializeField] private Transform[] _spawnPoints;

    [Header("Pacing")]
    [SerializeField] private int _maxAlive = 35;
    [SerializeField] private float _betweenWavesGrace = 12f;
    [SerializeField] private float _mixStaggerMs = 150f;

    [Header("Testing")]
    [SerializeField] private bool _autoStartOnPlay = false;
    [SerializeField] private bool _logDebug = true;

    private int _alive;
    private int _currentWaveIndex;
    private readonly HashSet<DeathRelay> _tracked = new HashSet<DeathRelay>();
    private Coroutine _runner;
    private bool _nightActive;
    private bool _successRaised;

    public int alive => _alive;

    private void Start()
    {
        if (_nightPreset == null)
        {
            DebugManager.LogError("NightPreset is missing.", this);
            enabled = false; return;
        }
        if (_nightPreset.waves == null || _nightPreset.waves.Count == 0)
        {
            DebugManager.LogError("NightPreset has no waves.", this);
            enabled = false; return;
        }
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            DebugManager.LogError("No spawn points assigned.", this);
            enabled = false; return;
        }

        if (_autoStartOnPlay) StartNight();
    }

    private void OnEnable()
    {
        GameEvents.NightStarted += OnNightStarted;
        GameEvents.DayStarted += OnDayStarted;
    }

    private void OnDisable()
    {
        GameEvents.NightStarted -= OnNightStarted;
        GameEvents.DayStarted -= OnDayStarted;
        StopNight();
    }

    private void OnNightStarted()
    {
        if (_nightActive) return;
        StartNight();
    }

    private void OnDayStarted()
    {
        StopNight();
    }

    public void StartNight()
    {
        if (_runner != null) StopCoroutine(_runner);

        _successRaised = false;
        _alive = 0;
        _currentWaveIndex = 0;
        _nightActive = true;
        _runner = StartCoroutine(RunNight());

        if (_logDebug) DebugManager.Log("Night started.", this);
    }

    public void StopNight()
    {
        _nightActive = false;
        if (_runner != null)
        {
            StopCoroutine(_runner);
            _runner = null;
        }
        if (_logDebug) DebugManager.Log("Night stopped.", this);
    }

    private IEnumerator RunNight()
    {
        var waves = _nightPreset.waves;
        for (int w = 0; w < waves.Count; w++)
        {
            if (!_nightActive) yield break;

            var wave = waves[w];
            if (_logDebug) DebugManager.Log($"Starting wave {w + 1}/{waves.Count}: {wave.name}", this);

            if (wave.startDelay > 0f) yield return new WaitForSeconds(wave.startDelay);

            yield return StartCoroutine(SpawnMix(wave));

            if (wave.endDelay > 0f) yield return new WaitForSeconds(wave.endDelay);

            float t0 = Time.time;
            int target = Mathf.Min(4, Mathf.Max(0, AdjustedMaxAlive / 5));
            while (_alive > target && (Time.time - t0) < _betweenWavesGrace)
                yield return null;
        }

        if (_logDebug) DebugManager.Log("All waves dispatched. Waiting for last enemies to die...", this);

        while (_alive > 0) yield return null;

        _runner = null;
        _nightActive = false;
        if (!_successRaised)
        {
            _successRaised = true;
            if (_logDebug) DebugManager.Log("All waves cleared. Raising AllZombiesCleared.", this);
            GameEvents.RaiseAllZombiesCleared();
        }
    }

    private int AdjustedMaxAlive
    {
        get
        {
            int cap = Mathf.Max(1, _maxAlive);

            if (_nightPreset != null &&
                _currentWaveIndex >= 0 &&
                _currentWaveIndex < _nightPreset.waves.Count)
            {
                var wave = _nightPreset.waves[_currentWaveIndex];
                if (wave.maxAliveOverride > 0)
                    cap = wave.maxAliveOverride;
            }

            return cap;
        }
    }
    private IEnumerator SpawnMix(NightPreset.Wave wave)
    {
        if (wave.entries == null || wave.entries.Count == 0) yield break;
        if (_spawnPoints == null || _spawnPoints.Length == 0) yield break;

        int entryCount = wave.entries.Count;
        int[] spawnedPerEntry = new int[entryCount];
        int[] totalPerEntry = new int[entryCount];
        float[] nextTime = new float[entryCount];

        float now = Time.time;
        int totalToSpawn = 0;
        for (int i = 0; i < entryCount; i++)
        {
            var e = wave.entries[i];
            totalPerEntry[i] = Mathf.Max(0, e.count);
            totalToSpawn += totalPerEntry[i];
            nextTime[i] = now + (i * (_mixStaggerMs * 0.001f));
        }

        int totalSpawned = 0;
        while (totalSpawned < totalToSpawn)
        {
            while (_alive >= AdjustedMaxAlive) yield return null;

            int pick = -1;
            float soonest = float.MaxValue;
            float t = Time.time;
            for (int i = 0; i < entryCount; i++)
            {
                if (spawnedPerEntry[i] >= totalPerEntry[i]) continue;
                if (nextTime[i] < soonest) { soonest = nextTime[i]; pick = i; }
            }
            if (pick == -1) break;

            float wait = Mathf.Max(0f, soonest - t);
            if (wait > 0f) yield return new WaitForSeconds(wait);

            var entry = wave.entries[pick];
            if (entry.prefab != null)
            {
                var p = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
                var spawnPos = p.position;
                if (SpawnManager.Instance != null) spawnPos.z = SpawnManager.Instance.targetZ;

                var go = Instantiate(entry.prefab, spawnPos, p.rotation);

                var relay = go.GetComponent<DeathRelay>() ?? go.AddComponent<DeathRelay>();
                relay.Init(this);
                _tracked.Add(relay);

                _alive++;
                if (_logDebug) DebugManager.Log($"Alive -> {_alive}", this);
            }

            spawnedPerEntry[pick]++;
            totalSpawned++;
            nextTime[pick] = Time.time + Mathf.Max(0.02f, entry.interval);
        }
    }

    public void ClearAllSpawns()
    {
        DebugManager.LogWarning("ClearAllSpawns is reserved for game over or resets. " +
                                "Sunrise should NOT clear enemies.", this);

        StopAllCoroutines();
        var snapshot = new List<DeathRelay>(_tracked);
        foreach (var r in snapshot)
            if (r && r.gameObject) r.gameObject.SetActive(false);
        _tracked.Clear();
        _alive = 0;
    }

    public void SetPreset(NightPreset preset) => _nightPreset = preset;

    internal void NotifyRelayDisabled(DeathRelay relay)
    {
        if (relay == null || !_tracked.Contains(relay) || relay.countedDead) return;

        relay.countedDead = true;
        _tracked.Remove(relay);
        _alive = Mathf.Max(0, _alive - 1);

        if (_logDebug) DebugManager.Log($"Alive -> {_alive}", this);
    }

    private void OnDrawGizmosSelected()
    {
        if (_spawnPoints == null) return;
        Gizmos.color = Color.cyan;
        foreach (var t in _spawnPoints)
            if (t != null) Gizmos.DrawWireSphere(t.position, 0.3f);
    }
}

public class DeathRelay : MonoBehaviour
{
    [SerializeField] private bool _logDebug = false;

    private WaveManager _manager;
    [HideInInspector] public bool countedDead;

    public void Init(WaveManager manager)
    {
        _manager = manager;
        countedDead = false;
    }

    private void OnDisable()
    {
        _manager?.NotifyRelayDisabled(this);
        if (_logDebug) DebugManager.Log("OnDisable -> notified manager", this);
    }

    private void OnDestroy()
    {
        _manager?.NotifyRelayDisabled(this);
        if (_logDebug) DebugManager.Log("OnDestroy -> notified manager", this);
    }
}
