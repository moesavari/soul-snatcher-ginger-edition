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
    [SerializeField] private float _betweenWavesGrace = 12f;  // max seconds to wait for cleanup before next wave
    [SerializeField] private float _mixStaggerMs = 150f;      // small initial offsets between entry streams

    [Header("Testing")]
    [SerializeField] private bool _autoStartOnPlay = false;
    [SerializeField] private bool _logDebug = true;

    private int _alive;
    private readonly HashSet<DeathRelay> _tracked = new HashSet<DeathRelay>();
    private Coroutine _runner;
    private bool _nightActive;

    public int alive => _alive;

    private void Awake()
    {
        if (_nightPreset == null) Debug.LogWarning("[WaveManager] No NightPreset assigned.");
        if (_spawnPoints == null || _spawnPoints.Length == 0) Debug.LogWarning("[WaveManager] No spawn points assigned.");
    }

    private void Start()
    {
        if (_autoStartOnPlay) StartNight();
    }

    private void OnEnable()
    {
        GameEvents.NightStarted += OnNightStarted;
        GameEvents.DayStarted += OnDayStarted;
        if (_autoStartOnPlay) StartNight();
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
        _runner = StartCoroutine(RunNight());
    }

    private IEnumerator RunNight()
    {
        if (_nightPreset == null || _nightPreset.waves == null || _nightPreset.waves.Count == 0) yield break;

        var waves = _nightPreset.waves;

        for (int w = 0; w < waves.Count; w++)
        {
            var wave = waves[w];
            if (_logDebug) Debug.Log($"[WaveManager] Starting wave {w + 1}/{waves.Count}: {wave.name}");

            if (wave.startDelay > 0f) yield return new WaitForSeconds(wave.startDelay);

            yield return StartCoroutine(SpawnMix(wave));

            if (wave.endDelay > 0f) yield return new WaitForSeconds(wave.endDelay);

            // Cleanup window: wait until the field is mostly clear or we time out
            float t0 = Time.time;
            int target = Mathf.Min(4, Mathf.Max(0, _maxAlive / 5));
            while (_alive > target && (Time.time - t0) < _betweenWavesGrace)
                yield return null;
        }

        if (_logDebug) Debug.Log("[WaveManager] All waves dispatched.");
        _runner = null;
    }

    public void StopNight()
    {
        _nightActive = false;
        if (_runner != null)
        {
            StopCoroutine(_runner);
            _runner = null;
        }
    }

    private IEnumerator SpawnMix(NightPreset.Wave wave)
    {
        if (wave.entries == null || wave.entries.Count == 0) yield break;
        if (_spawnPoints == null || _spawnPoints.Length == 0) yield break;

        int n = wave.entries.Count;

        var spawnedPerEntry = new int[n];
        var totalPerEntry = new int[n];
        var nextTime = new float[n];

        float now = Time.time;
        int totalToSpawn = 0;

        for (int i = 0; i < n; i++)
        {
            var e = wave.entries[i];
            int c = Mathf.Max(0, e.count);
            totalPerEntry[i] = c;
            totalToSpawn += c;

            float initialOffset = (i * (_mixStaggerMs * 0.001f));
            nextTime[i] = now + initialOffset;
        }

        int totalSpawned = 0;

        while (totalSpawned < totalToSpawn)
        {
            // Respect alive cap
            while (_alive >= _maxAlive) yield return null;

            // Pick the entry whose next fire time is soonest (and not finished)
            int pick = -1;
            float soonest = float.MaxValue;
            float t = Time.time;

            for (int i = 0; i < n; i++)
            {
                if (spawnedPerEntry[i] >= totalPerEntry[i]) continue;
                if (nextTime[i] < soonest)
                {
                    soonest = nextTime[i];
                    pick = i;
                }
            }

            if (pick == -1) break;

            // Wait until it is time
            float wait = Mathf.Max(0f, soonest - t);
            if (wait > 0f) yield return new WaitForSeconds(wait);

            // Spawn one of the picked entry
            var entry = wave.entries[pick];
            if (entry.prefab == null)
            {
                Debug.LogWarning("[WaveManager] Missing prefab in an EnemyEntry; skipping.");
            }
            else
            {
                var p = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
                var spawnPos = p.position;

                // Optional safety: align Z to SpawnManager.targetZ before spawn
                if (SpawnManager.Instance != null)
                {
                    spawnPos.z = SpawnManager.Instance.targetZ;
                }

                // No direct calls to SpawnZFixer here — it runs in Awake on the prefab
                var go = Instantiate(entry.prefab, spawnPos, p.rotation);

                // Keep DeathRelay so alive counts are correct
                var relay = go.GetComponent<DeathRelay>();
                if (relay == null) relay = go.AddComponent<DeathRelay>();
                relay.Init(this);

                _tracked.Add(relay);
                _alive++;
                if (_logDebug) Debug.Log($"[WaveManager] Alive -> {_alive}");
            }

            spawnedPerEntry[pick]++;
            totalSpawned++;

            // schedule next for this entry
            float interval = Mathf.Max(0.02f, entry.interval);
            nextTime[pick] = Time.time + interval;
        }
    }

    internal void NotifyRelayDisabled(DeathRelay relay)
    {
        if (relay == null) return;
        if (_tracked.Contains(relay) && !relay.countedDead)
        {
            relay.countedDead = true;
            _tracked.Remove(relay);
            _alive = Mathf.Max(0, _alive - 1);
            if (_logDebug) Debug.Log($"[WaveManager] Alive -> {_alive}");
        }
    }

    // Debug gizmos
    private void OnDrawGizmosSelected()
    {
        if (_spawnPoints == null) return;
        Gizmos.color = Color.cyan;
        foreach (var t in _spawnPoints)
        {
            if (t == null) continue;
            Gizmos.DrawWireSphere(t.position, 0.3f);
        }
    }
}

/// <summary>
/// Attached to each spawned enemy to reliably decrement alive on Disable/Destroy (pooling-friendly).
/// </summary>
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
        if (_logDebug) Debug.Log("[DeathRelay] OnDisable -> notified manager");
    }

    private void OnDestroy()
    {
        _manager?.NotifyRelayDisabled(this);
        if (_logDebug) Debug.Log("[DeathRelay] OnDestroy -> notified manager");
    }
}
