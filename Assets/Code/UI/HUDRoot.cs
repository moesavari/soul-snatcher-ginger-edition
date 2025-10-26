using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum UIPanelID { None, Inventory, Equipment, Shop }

[DisallowMultipleComponent]

public class HUDRoot : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image _healthFill;
    [SerializeField] private TMP_Text _soulsText;
    [SerializeField] private TMP_Text _dayNightText;
    [SerializeField] private TMP_Text _waveText;
    [SerializeField] private Image _hairPortrait;

    [Header("Hair Portraits")]
    [SerializeField] private Sprite _hairStageOne;
    [SerializeField] private Sprite _hairStageTwo;
    [SerializeField] private Sprite _hairStageThree;

    [Header("Night Banner")]
    [SerializeField] private CanvasGroup _bannerGroup;
    [SerializeField] private TMP_Text _bannerTitle;
    [SerializeField] private TMP_Text _bannerSubtitle;

    [Header("Rewards")]
    [SerializeField] private GameObject _rewardsRoot;
    [SerializeField] private TMP_Text _soulsValueText;
    [SerializeField] private TMP_Text _repValueText;
    [SerializeField] private TMP_Text _extraText;

    private PlayerController _playerHealth;
    private CharacterStats _stats;
    private GameObject _player;
    private ISoulReadOnly _soulSource;
    private IHairStageReadOnly _hairSource;

    public CanvasGroup bannerGroup => _bannerGroup;

    private void Awake()
    {
        if (_healthFill == null) DebugManager.LogWarning("_healthFill not set.", this);
        if (_soulsText == null) DebugManager.LogWarning("_soulsText not set.", this);
        if (_dayNightText == null) DebugManager.LogWarning("_dayNightText not set.", this);
        if (_hairPortrait == null) DebugManager.LogWarning("_hairPortrait not set.", this);
    }

    private void OnEnable()
    {
        GameEvents.DayStarted += OnDayStarted;
        GameEvents.NightStarted += OnNightStarted;
        GameEvents.PlayerSpawned += OnPlayerSpawned;

        TryBindExistingPlayer();
    }

    private void OnDisable()
    {
        GameEvents.DayStarted -= OnDayStarted;
        GameEvents.NightStarted -= OnNightStarted;

        if (_playerHealth != null)
        {
            _playerHealth.OnDamaged -= OnPlayerDamaged;
            _playerHealth.OnDeath -= OnPlayerDeath;
        }

        if (_soulSource != null) _soulSource.SoulsChanged -= OnSoulsChanged;
        if (_hairSource != null) _hairSource.HairStageChanged -= OnHairStageChanged;
    }

    private void TryBindExistingPlayer()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.player != null)
            BindPlayer(gm.player);
    }

    private void OnPlayerSpawned(GameObject go) => BindPlayer(go);

    private void BindPlayer(GameObject go)
    {
        UnbindPlayer();

        _player = go;
        _playerHealth = _player != null ? _player.GetComponent<PlayerController>() : null;
        _stats = _player != null ? _player.GetComponent<CharacterStats>() : null;

        if (_playerHealth == null)
        {
            DebugManager.LogWarning("Player has no Health component.", this);
            return;
        }

        if (_stats != null)
            _stats.OnStatsChanged += OnStatsChanged;

        _playerHealth.OnDamaged += OnPlayerDamaged;
        _playerHealth.OnDeath += OnPlayerDeath;

        UpdateHealthBar();

        _soulSource = _player.GetComponent<ISoulReadOnly>();
        if (_soulSource != null)
        {
            _soulSource.SoulsChanged += OnSoulsChanged;
            UpdateSouls(_soulSource.souls);
        }
        else _soulsText?.SetText("Souls: 0");

        _hairSource = _player.GetComponent<IHairStageReadOnly>();
        if (_hairSource != null)
        {
            _hairSource.HairStageChanged += OnHairStageChanged;
            ApplyHairStage(_hairSource.hairStage);
        }
        else ApplyHairStage(1);
    }

    private void UnbindPlayer()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDamaged -= OnPlayerDamaged;
            _playerHealth.OnDeath -= OnPlayerDeath;
        }

        if (_stats != null)
            _stats.OnStatsChanged -= OnStatsChanged;

        _playerHealth = null;
        _player = null;
        _stats = null;
    }

    private void OnDayStarted()
    {
        _dayNightText?.SetText("DAY");
    }

    private void OnNightStarted()
    {
        _dayNightText?.SetText("NIGHT");
    }

    private void OnPlayerDamaged(int amount)
    {
        UpdateHealthBar();
    }

    private void OnPlayerDeath()
    {
        UpdateHealthBar();
    }

    private void OnSoulsChanged(int souls)
    {
        UpdateSouls(souls);
    }

    private void OnHairStageChanged(int stage)
    {
        ApplyHairStage(stage);
    }

    private void OnStatsChanged(CharacterStats stats)
    {
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (_playerHealth == null || _healthFill == null || _stats == null) return;

        float pct = (_stats.Health <= 0) ? 0f : Mathf.Clamp01((float)_playerHealth.currentHealth / _stats.Health);
        _healthFill.fillAmount = pct;
    }


    private void UpdateSouls(int souls)
    {
        _soulsText?.SetText($"Souls: {souls}");
    }

    private void ApplyHairStage(int stage)
    {
        if (_hairPortrait == null) return;
        Sprite s = stage switch
        {
            <= 1 => _hairStageOne,
            2 => _hairStageTwo,
            _ => _hairStageThree
        };

        _hairPortrait.sprite = s;
    }

    public void SetWaveInfo(string text)
    {
        _waveText?.SetText(text);
    }

    public void ShowLoseBanner()
    {
        SetActive(_rewardsRoot, false);
        SetText(_bannerTitle, "You lose!");
        SetText(_bannerSubtitle, "Village is destroyed kekw");

        if (_bannerGroup != null) _bannerGroup.alpha = 0;
        _bannerGroup?.gameObject.SetActive(true);
    }

    public void ShowWinBanner(int souls, int rep, string extra = null)
    {
        SetActive(_rewardsRoot, true);
        SetText(_bannerTitle, "Night Survived!");
        SetText(_bannerSubtitle, "All threats neutralized");

        SetText(_soulsValueText, $"+{souls} Souls");
        SetText(_repValueText, $"+{rep} Reputation");
        SetText(_extraText, string.IsNullOrEmpty(extra) ? "" : extra);

        if (_bannerGroup != null) _bannerGroup.alpha = 0f;
        _bannerGroup?.gameObject.SetActive(true);
    }

    private void SetText(TMP_Text t, string s) { if (t) t.text = s; }
    private void SetActive(GameObject go, bool v) {  if(go && go.activeSelf != v) { go.SetActive(v); } }
}
