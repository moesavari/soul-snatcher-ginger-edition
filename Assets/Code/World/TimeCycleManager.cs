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

        if (_dayDurationSeconds <= 0f)
        {
            Debug.LogWarning("TimeCycleManager: _dayDurationSeconds must be > 0. Defaulting to 60.");
            _dayDurationSeconds = 60f;
        }

        if (_nightDurationSeconds <= 0f)
        {
            Debug.LogWarning("TimeCycleManager: _nightDurationSeconds must be > 0. Defaulting to 60.");
            _nightDurationSeconds = 60f;
        }

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

        if (_runner != null)
        {
            StopCoroutine(_runner);
            _runner = null;
        }
    }

    private void Start()
    {
        if (_runner != null)
        {
            StopCoroutine(_runner);
        }

        _runner = StartCoroutine(StateLoop());
    }

    private IEnumerator StateLoop()
    {
        while (true)
        {
            if (_isNight)
            {
                // NIGHT PHASE
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
                // DAY PHASE
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

    public void ForceNight()
    {
        if (_runner != null)
        {
            StopCoroutine(_runner);
        }

        _isNight = true;
        _runner = StartCoroutine(StateLoop());
    }

    public void ForceDay()
    {
        if (_runner != null)
        {
            StopCoroutine(_runner);
        }

        _isNight = false;
        _runner = StartCoroutine(StateLoop());
    }

    private void OnNightSuccess()
    {
        if (!_isNight)
        {
            return;
        }

        StartDayImmediate();
    }

    private void OnNightFail()
    {
        if (!_isNight)
        {
            return;
        }

        if (_runner != null)
        {
            StopCoroutine(_runner);
            _runner = null;
        }
    }

    private void StartDayImmediate()
    {
        _isNight = false;
        _timer = _dayDurationSeconds;
        NightOverlay.Instance?.SetNight(false);
        GameEvents.RaiseDay();
    }
}
