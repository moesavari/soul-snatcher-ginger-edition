using TMPro;
using UnityEngine;

public sealed class EquipmentStatsUI : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private CharacterStats _stats;

    [Header("Value Texts (right column)")]
    [SerializeField] private TMP_Text _health;
    [SerializeField] private TMP_Text _armor;
    [SerializeField] private TMP_Text _attackPower;
    [SerializeField] private TMP_Text _spellPower;
    [SerializeField] private TMP_Text _attackSpeed;
    [SerializeField] private TMP_Text _critChance;
    [SerializeField] private TMP_Text _moveSpeed;
    [SerializeField] private TMP_Text _cooldown;

    private void Awake()
    {
        if (!_stats) _stats = FindFirstObjectByType<CharacterStats>();

        if (_stats) WriteFromStats();
    }

    private void OnEnable()
    {
        if (_stats != null)
        {
            _stats.OnStatsChanged += HandleStatsChanged;
        }

        WriteFromStats();
    }

    private void OnDisable()
    {
        if (_stats != null)
            _stats.OnStatsChanged -= HandleStatsChanged;
    }

    private void HandleStatsChanged(CharacterStats _)
    {
        WriteFromStats();
    }

    private void WriteFromStats()
    {
        if (!_stats) return;

        Set(_health, _stats.Health);
        Set(_armor, _stats.Armor);
        Set(_attackPower, _stats.AttackPower);
        Set(_spellPower, _stats.SpellPower);
        Set(_attackSpeed, _stats.AttackSpeed);
        Set(_critChance, _stats.CritChance);
        Set(_moveSpeed, _stats.MoveSpeed);
        Set(_cooldown, _stats.CooldownReduction);
    }

    private static void Set(TMP_Text t, int v)
    {
        if (t) t.text = v.ToString();
    }

    private static void Set(TMP_Text t, float v)
    {
        if (t) t.text = v.ToString();
    }
}
