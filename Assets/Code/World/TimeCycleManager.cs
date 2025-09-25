using System.Collections;
using UnityEngine;

public class TimeCycleManager : MonoSingleton<TimeCycleManager>
{
    [Header("Durations")]
    [SerializeField] private float _dayDurationSeconds = 90f;
    [SerializeField] private float _nightDurationSeconds = 90f;

    [Header("Night Prep")]
    [SerializeField] private float _nightSpawnDelay = 5f; // countdown/fade before waves begin

    [Header("Start State")]
    [SerializeField] private bool _startAtNight = false;

    private bool _isNight;
    private float _timer;               // time remaining in current state (excludes _nightSpawnDelay)
    private Coroutine _runner;

    public bool isNight => _isNight;
    public float cycleTimeRemaining => _timer;    // during day: time to night prep; during night: time to end
    public float nightSpawnDelay => _nightSpawnDelay;

    protected override void Awake()
    {
        base.Awake();
        _isNight = _startAtNight;
        _timer = _isNight ? _nightDurationSeconds : _dayDurationSeconds;
    }

    private void OnEnable()
    {
        // End conditions override the timer
        GameEvents.AllZombiesCleared += OnNightSuccess;
        GameEvents.RoundLost += OnNightFail;
    }

    private void OnDisable()
    {
        GameEvents.AllZombiesCleared -= OnNightSuccess;
        GameEvents.RoundLost -= OnNightFail;
        if (_runner != null) { StopCoroutine(_runner); _runner = null; }
    }

    private void Start()
    {
        // Start coroutine-based state machine
        if (_runner != null) StopCoroutine(_runner);
        _runner = StartCoroutine(StateLoop());
    }

    private IEnumerator StateLoop()
    {
        while (true)
        {
            if (_isNight)
            {
                // NIGHT STATE ------------------------------------------
                NightOverlay.Instance?.SetNight(true); // visual first
                _timer = Mathf.Max(0f, _nightDurationSeconds);

                // Kick off night after prep (spawns listen to RaiseNight)
                if (_nightSpawnDelay > 0f)
                    yield return new WaitForSeconds(_nightSpawnDelay);

                GameEvents.RaiseNight();

                // Tick night unless interrupted by win/lose handlers
                while (_isNight && _timer > 0f)
                {
                    _timer -= Time.deltaTime;
                    yield return null;
                }

                // If timer elapsed (no win/lose), transition to day
                if (_isNight) StartDayImmediate();
            }
            else
            {
                // DAY STATE --------------------------------------------
                NightOverlay.Instance?.SetNight(false);
                _timer = Mathf.Max(0f, _dayDurationSeconds);
                GameEvents.RaiseDay();

                // Tick day until countdown reaches zero (then go to night prep)
                while (!_isNight && _timer > 0f)
                {
                    _timer -= Time.deltaTime;
                    yield return null;
                }

                // Switch state; night prep + RaiseNight will happen at top of loop
                if (!_isNight) _isNight = true;
            }

            // next loop iteration continues in the new state
            yield return null;
        }
    }

    // ------------------------------------------------------------------
    // Public controls
    public void ForceNight()
    {
        if (_runner != null) StopCoroutine(_runner);
        _isNight = true;
        _runner = StartCoroutine(StateLoop());
    }

    public void ForceDay()
    {
        if (_runner != null) StopCoroutine(_runner);
        _isNight = false;
        _runner = StartCoroutine(StateLoop());
    }

    // ------------------------------------------------------------------
    // Outcome handling (ends night early)
    private void OnNightSuccess()
    {
        if (!_isNight) return;
        StartDayImmediate();
    }

    private void OnNightFail()
    {
        if (!_isNight) return;
        // Don’t auto-start day here if your lose flow goes to menu;
        // we just stop the loop and let the outcome controller own flow.
        if (_runner != null) { StopCoroutine(_runner); _runner = null; }
    }

    // Immediate cut to day (used by success or time-up)
    private void StartDayImmediate()
    {
        _isNight = false;
        _timer = _dayDurationSeconds;
        NightOverlay.Instance?.SetNight(false);
        GameEvents.RaiseDay();
    }
}
