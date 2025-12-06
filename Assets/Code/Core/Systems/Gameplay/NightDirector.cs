using UnityEngine;

public class NightDirector : MonoSingleton<NightDirector>
{
    [SerializeField] private WaveManager _waveManager;
    [SerializeField] private NightPreset[] _nights;
    [SerializeField] private int _nightIndex = 0;

    public int nightIndex => _nightIndex;

    private void OnEnable()
    {
        GameEvents.NightStarted += OnNightStarted;
        GameEvents.DayStarted += OnDayStarted;
    }

    private void OnDisable()
    {
        GameEvents.NightStarted -= OnNightStarted;
        GameEvents.DayStarted -= OnDayStarted;
    }

    private void OnNightStarted()
    {
        if (_waveManager == null || _nights == null || _nights.Length == 0) return;

        _nightIndex = Mathf.Clamp(_nightIndex, 0, _nights.Length - 1);
        _waveManager.SetPreset(_nights[_nightIndex]);
        _waveManager.StartNight();
    }

    private void OnDayStarted()
    {
        // TODO: progress to next night after a successful night
        // _nightIndex = Mathf.Min(_nightIndex + 1, _nights.Length - 1);
    }
}
