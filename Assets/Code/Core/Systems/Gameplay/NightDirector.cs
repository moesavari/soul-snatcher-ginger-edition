using UnityEngine;

public class NightDirector : MonoSingleton<NightDirector>
{
    [SerializeField] private WaveManager _waveManager;

    [SerializeField] private NightPresetSet _nightSet;
    [SerializeField] private bool _logDebug = true;

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

    public void ResetToDay1()
    {
        _nightIndex = 0;

        if (_logDebug)
        {
            DebugManager.Log("NightDirector: Reset to Day 1 (night index = 0).", this);
        }
    }

    private void OnNightStarted()
    {
        if (_waveManager == null)
        {
            _waveManager = FindFirstObjectByType<WaveManager>();
            if (_waveManager == null)
            {
                DebugManager.LogWarning("NightDirector: WaveManager is missing.", this);
                return;
            }
        }

        NightPreset preset = ResolvePresetForCurrentNight();
        if (preset == null)
        {
            DebugManager.LogWarning($"NightDirector: No preset resolved for night index {_nightIndex}.", this);
            return;
        }

        _waveManager.SetPreset(preset);
        _waveManager.StartNight();

        if (_logDebug)
        {
            DebugManager.Log($"NightDirector: Starting night {_nightIndex + 1} using preset '{preset.name}'.", this);
        }
    }

    private void OnDayStarted()
    {
        if (_nightSet == null) return;

        if (_nightIndex < _nightSet.nightCount - 1)
        {
            _nightIndex++;
        }

        if (_logDebug)
        {
            DebugManager.Log($"NightDirector: Advancing to night index {_nightIndex}.", this);
        }
    }

    private NightPreset ResolvePresetForCurrentNight()
    {
        if (_nightSet == null || _nightSet.nightCount == 0)
        {
            DebugManager.LogWarning("NightDirector: NightPresetSet is missing or empty.", this);
            return null;
        }

        return _nightSet.GetRandomPresetForNight(_nightIndex);
    }
}
