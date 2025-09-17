using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private Health _playerHealth;
    private GameObject _player;
    private ISoulReadOnly _soulSource;
    private IHairStageReadOnly _hairSource;

    public CanvasGroup bannerGroup => _bannerGroup;

    private void Awake()
    {
        if (_healthFill == null) Debug.LogWarning("[HUD] _healthFill not set.");
        if (_soulsText == null) Debug.LogWarning("[HUD] _soulsText not set.");
        if (_dayNightText == null) Debug.LogWarning("[HUD] _dayNightText not set.");
        if (_hairPortrait == null) Debug.LogWarning("[HUD] _hairPortrait not set.");
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
        _playerHealth = _player != null ? _player.GetComponent<Health>() : null;

        if (_playerHealth == null)
        {
            Debug.LogWarning("[HUDRoot] Player has no Health component.");
            return;
        }

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

        _playerHealth = null;
        _player = null;
    }

    private void OnDayStarted()
    {
        _dayNightText?.SetText("DAY");
    }

    private void OnNightStarted()
    {
        _dayNightText?.SetText("NIGHT");
    }

    private void OnPlayerDamaged(Health h, int amount)
    {
        UpdateHealthBar();
    }

    private void OnPlayerDeath(Health h)
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

    private void UpdateHealthBar()
    {
        if (_playerHealth == null || _healthFill == null) return;
        float pct = (_playerHealth.max <= 0) ? 0f : Mathf.Clamp01((float)_playerHealth.current / _playerHealth.max);
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
