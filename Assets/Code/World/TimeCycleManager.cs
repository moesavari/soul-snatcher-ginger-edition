using System;
using UnityEngine;

public class TimeCycleManager : MonoSingleton<TimeCycleManager>
{
    [SerializeField] private float _dayDurationSeconds = 90f;
    [SerializeField] private float _nightDurationSeconds = 90f;
    [SerializeField] private float _nightSpawnDelay = 5f;
    [SerializeField] private bool _startAtNight = false;

    private float _timer;
    private bool _isNight;

    public bool isNight => _isNight;
    public float cycleTimeRemaining => _timer;
    public float nightSpawnDelay => _nightSpawnDelay;

    protected override void Awake()
    {
        base.Awake();

        _isNight = _startAtNight;
        _timer = _isNight ? _nightDurationSeconds : _dayDurationSeconds;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0) return;

        if (_isNight) StartDay();
        else StartNight();
    }

    public void ForceNight()
    {
        _timer = 0;
        StartNight();
    }

    public void ForceDay()
    {
        StartDay();
    }

    private void StartDay()
    {
        NightOverlay.Instance.SetNight(false);
        _isNight = false;
        _timer = _dayDurationSeconds;
        GameEvents.RaiseDay();
    }

    private void StartNight()
    {
        NightOverlay.Instance.SetNight(true);
        _isNight = true;
        _timer = _nightDurationSeconds;
        GameEvents.RaiseNight();
    }
}
