using TMPro;
using UnityEngine;

public class PlayerUIBridge : MonoBehaviour
{
    [SerializeField] private TMP_Text _soulsText;
    [SerializeField] private TMP_Text _waveText;
    [SerializeField] private HealthbarUI _healthbar;

    private void OnEnable()
    {
        // HealthEvents is ours—safe to use
        HealthEvents.HealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        HealthEvents.HealthChanged -= OnHealthChanged;
    }

    // --- External updaters (call these from your existing systems) ---
    public void SetSouls(int souls)
    {
        if (_soulsText) _soulsText.text = $"SOULS: {souls}";
    }

    public void SetWave(int current, int total)
    {
        if (_waveText) _waveText.text = $"Wave {current}/{total}";
    }

    // --- Internal handlers ---
    private void OnHealthChanged(int current, int max)
    {
        _healthbar?.Set(current, max);
    }
}
