using System.Collections;
using UnityEngine;

public class TimeCycleManager : MonoSingleton<TimeCycleManager>
{
    [Header("Durations")]
    [SerializeField] private float _dayDurationSeconds = 90f;
    [SerializeField] private float _nightDurationSeconds = 90f;

    [Header("Night Prep")]
    [SerializeField] private float _nightSpawnDelay = 5f;
    [SerializeField] private AudioCue _nightStartAudioCue;

    [Header("Start State")]
    [SerializeField] private bool _startAtNight = false;

    private bool _isNight;
    private float _timer;
    private Coroutine _runner;

    public bool isNight => _isNight;
    public float cycleTimeRemaining => _timer;
    public float nightSpawnDelay => _nightSpawnDelay;

    protected override void Awake()
    {
        base.Awake();

        _isNight = _startAtNight;
        _timer = _isNight ? _nightDurationSeconds : _dayDurationSeconds;
    }

    private void OnEnable()
    {
        GameEvents.AllZombiesCleared += OnNightSuccess;
        GameEvents.RoundLost += OnNightFail;
    }

    private void OnDisable()
    {
        GameEvents.AllZombiesCleared -= OnNightSuccess;
        GameEvents.RoundLost -= OnNightFail;

        StopCycle();
    }

    private void Start()
    {
        StartOrRestartLoop();
    }

    public void StopCycle()
    {
        if (_runner != null)
        {
            StopCoroutine(_runner);
            _runner = null;
        }
    }

    public void ResetToDay1()
    {
        _startAtNight = false;
        _isNight = false;
        _timer = Mathf.Max(0f, _dayDurationSeconds);

        NightOverlay.Instance?.TryRebindOverlay();
        NightOverlay.Instance?.SetNight(false);

        StartOrRestartLoop();

        // Make sure day listeners (NightDirector/WaveManager/etc.) get the signal.
        GameEvents.RaiseDay();
    }

    private void StartOrRestartLoop()
    {
        StopCycle();
        _runner = StartCoroutine(StateLoop());
    }

    private IEnumerator StateLoop()
    {
        while (true)
        {
            if (_isNight)
            {
                NightOverlay.Instance?.SetNight(true);
                _timer = Mathf.Max(0f, _nightDurationSeconds);

                if (_nightStartAudioCue != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayCue(_nightStartAudioCue, worldPos: transform.position);
                }

                if (_nightSpawnDelay > 0f)
                {
                    yield return new WaitForSeconds(_nightSpawnDelay);
                }

                GameEvents.RaiseNight();

                while (_isNight && _timer > 0f)
                {
                    _timer -= Time.deltaTime;
                    yield return null;
                }

                if (_isNight)
                {
                    StartDayImmediate();
                }
            }
            else
            {
                NightOverlay.Instance?.SetNight(false);
                _timer = Mathf.Max(0f, _dayDurationSeconds);

                GameEvents.RaiseDay();

                while (!_isNight && _timer > 0f)
                {
                    _timer -= Time.deltaTime;
                    yield return null;
                }

                if (!_isNight)
                {
                    _isNight = true;
                }
            }

            yield return null;
        }
    }

    private void OnNightSuccess()
    {
        if (!_isNight) return;
        StartDayImmediate();
    }

    private void OnNightFail()
    {
        // Instead of killing the loop forever, reset to Day 1.
        ResetToDay1();
    }

    private void StartDayImmediate()
    {
        _isNight = false;
        _timer = _dayDurationSeconds;
        NightOverlay.Instance?.SetNight(false);
        GameEvents.RaiseDay();
    }
}
