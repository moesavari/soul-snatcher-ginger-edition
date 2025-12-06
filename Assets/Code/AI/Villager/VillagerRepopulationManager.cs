using UnityEngine;
using System.Collections.Generic;

public class VillagerRepopulationManager : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private GameObject _villagerPrefab;
    [SerializeField] private int _targetCount = 6;
    [SerializeField] private string _spawnPointTag = "VillagerSpawn";
    [SerializeField] private float _spawnJitter = 0.8f;

    private readonly List<Transform> _spawns = new();

    private void Awake()
    {
        var gos = GameObject.FindGameObjectsWithTag(_spawnPointTag);
        foreach (var go in gos) _spawns.Add(go.transform);

        GameEvents.DayStarted += OnDayStarted;
    }
    private void OnDestroy() => GameEvents.DayStarted -= OnDayStarted;

    private void OnDayStarted()
    {
        if (_villagerPrefab == null || _spawns.Count == 0) return;

        int current = CountAliveVillagers();
        int needed = Mathf.Max(0, _targetCount - current);
        for (int i = 0; i < needed; i++) SpawnOne(i);
    }

    private int CountAliveVillagers()
    {
        var villagers = Object.FindObjectsByType<Villager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int alive = 0;
        foreach (var v in villagers) if (v.isAlive) alive++;
        return alive;
    }

    private void SpawnOne(int idx)
    {
        var t = _spawns[idx % _spawns.Count];
        Vector2 j = Random.insideUnitCircle * _spawnJitter;
        Vector3 pos = t.position + new Vector3(j.x, j.y, 0f);
        Instantiate(_villagerPrefab, pos, Quaternion.identity);
    }
}
