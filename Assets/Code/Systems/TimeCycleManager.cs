using System;
using UnityEngine;

public class TimeCycleManager : MonoSingleton<TimeCycleManager>
{
    [SerializeField] private float _dayDurationSeconds = 90f;
    [SerializeField] private float _nightDurationSeconds = 90f;
    [SerializeField] private bool _startAtNight = false;

    private float _timer;
    private bool _isNight;

    public bool isNight => _isNight;
    public float cycleTimeRemaining => _timer;

    public static event Action OnDayStarted;
    public static event Action OnNightStarted;

    protected override void Awake()
    {
        base.Awake();

        _isNight = _startAtNight;
        _timer = _isNight ? _nightDurationSeconds : _dayDurationSeconds;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer < 0) return;

        if (_isNight) StartDay();
        else StartNight();
    }

    private void ForceNight()
    {
        StartNight();
    }

    private void StartDay()
    {
        _isNight = false;
        _timer = _dayDurationSeconds;
        OnDayStarted?.Invoke();
    }

    private void StartNight()
    {
        _isNight = true;
        _timer = _nightDurationSeconds;
        OnNightStarted?.Invoke();
    }
}
