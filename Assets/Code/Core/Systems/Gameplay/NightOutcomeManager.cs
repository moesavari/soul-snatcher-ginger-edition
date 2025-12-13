using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NightOutcomeManager : MonoBehaviour
{
    [SerializeField] private string _menuSceneName = "MainMenu";
    [SerializeField] private bool _reloadIfMenuMissing = true;

    [Header("HUD / Systems")]
    [SerializeField] private HUDRoot _hud;
    [SerializeField] private SoulSystem _soulSystem;
    [SerializeField] private MonoBehaviour _reputationSource;
    [SerializeField] private NightRewardProfile _rewardProfile;
    [SerializeField] private CurrencyWallet _currencyWallet; 

    [Header("Timing")]
    [SerializeField] private float _fadeIn = 0.35f;
    [SerializeField] private float _hold = 1.6f;
    [SerializeField] private float _fadeOut = 0.45f;
    [SerializeField] private float _postWinDelayToDay = 0.6f;
    [SerializeField] private float _postLoseDelayToMenu = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioCue _winCue;
    [SerializeField] private AudioCue _loseCue;

    [Header("Debug")]
    [SerializeField] private bool _logDebug = true;

    private Coroutine _sequence;
    private IReputationReadOnly _repReadOnly;

    private int _soulsAtNightStart;
    private int _repAtNightStart;
    private int _displayNightNumber = 1;

    private void Awake()
    {
        if (_hud == null) _hud = FindFirstObjectByType<HUDRoot>();
        if (_soulSystem == null) _soulSystem = FindFirstObjectByType<SoulSystem>();

        if (_reputationSource != null)
        {
            _repReadOnly = _reputationSource as IReputationReadOnly;
            if (_repReadOnly == null)
            {
                DebugManager.LogWarning(
                    "NightOutcomeManager: _reputationSource does not implement IReputationReadOnly.",
                    this);
            }
        }

        if (_currencyWallet == null)
            _currencyWallet = CurrencyWallet.Instance;
    }

    private void OnEnable()
    {
        GameEvents.AllZombiesCleared += OnNightSuccess;
        GameEvents.RoundLost += OnNightFail;
        GameEvents.NightStarted += OnNightStarted;
    }

    private void OnDisable()
    {
        GameEvents.AllZombiesCleared -= OnNightSuccess;
        GameEvents.RoundLost -= OnNightFail;
        GameEvents.NightStarted -= OnNightStarted;
    }

    private void OnNightStarted()
    {
        int index = NightDirector.Instance != null ? NightDirector.Instance.nightIndex : 0;
        _displayNightNumber = index + 1;

        _soulsAtNightStart = _soulSystem != null ? _soulSystem.souls : 0;
        _repAtNightStart = _repReadOnly != null ? _repReadOnly.reputation : 0;

        if (_logDebug)
        {
            DebugManager.Log(
                $"NightOutcomeManager: Night start snapshot → souls={_soulsAtNightStart}, rep={_repAtNightStart}",
                this);
        }
    }

    private void OnNightSuccess()
    {
        if (_sequence != null) StopCoroutine(_sequence);
        _sequence = StartCoroutine(RunWin());
    }

    private void OnNightFail()
    {
        if (_sequence != null) StopCoroutine(_sequence);
        _sequence = StartCoroutine(RunLose());
    }

    private int GetSoulsDelta()
    {
        if (_soulSystem == null) return 0;
        return _soulSystem.souls - _soulsAtNightStart;
    }

    private int GetRepDelta()
    {
        if (_repReadOnly == null) return 0;
        return _repReadOnly.reputation - _repAtNightStart;
    }

    private IEnumerator RunWin()
    {
        int nightIndex = NightDirector.Instance != null ? NightDirector.Instance.nightIndex : 0;
        int villagersAlive = CountVillagersAlive();

        var rewards = RewardCalculator.ComputeTierRewards(_rewardProfile, nightIndex, villagersAlive);

        if (_currencyWallet != null)
        {
            _currencyWallet.AddGold(rewards.gold);

            DebugManager.LogWarning("TODO: Hook CurrencyWallet gold add method here.", this);
        }

        int soulsDelta = GetSoulsDelta();
        int repDelta = GetRepDelta();

        if (_logDebug)
        {
            DebugManager.Log(
                $"Night Win → Night {nightIndex + 1}, villagersAlive={villagersAlive}, " +
                $"gold={rewards.gold}, soulsΔ={soulsDelta}, repΔ={repDelta}.",
                this);
        }

        _hud?.ShowWinBanner(soulsDelta, repDelta, rewards.summary);

        if (_winCue && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCue(_winCue);
        }

        if (_hud != null && _hud.bannerGroup != null)
        {
            yield return CanvasGroupFader.Fade(_hud.bannerGroup, 0f, 1f, _fadeIn);
            yield return new WaitForSeconds(_hold);
            yield return CanvasGroupFader.Fade(_hud.bannerGroup, 1f, 0f, _fadeOut);
        }
        else
        {
            yield return new WaitForSeconds(_hold);
        }

        yield return new WaitForSeconds(_postWinDelayToDay);

        _sequence = null;
    }

    private IEnumerator RunLose()
    {
        int nightIndex = NightDirector.Instance != null ? NightDirector.Instance.nightIndex : 0;
        int villagersAlive = CountVillagersAlive();

        int soulsDelta = GetSoulsDelta();
        int repDelta = GetRepDelta();

        string summary =
            $"Night {_displayNightNumber} failed.\n" +
            $"Villagers alive: {villagersAlive}.";

        if (_logDebug)
        {
            DebugManager.Log(
                $"Night Lose → Night {nightIndex + 1}, villagersAlive={villagersAlive}, " +
                $"soulsΔ={soulsDelta}, repΔ={repDelta}.",
                this);
        }

        _hud?.ShowLoseBanner(soulsDelta, repDelta, summary);

        if (_loseCue && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCue(_loseCue);
        }

        if (_hud != null && _hud.bannerGroup != null)
        {
            yield return CanvasGroupFader.Fade(_hud.bannerGroup, 0f, 1f, _fadeIn);
            yield return new WaitForSeconds(_hold);
        }
        else
        {
            yield return new WaitForSeconds(_hold);
        }

        yield return new WaitForSeconds(_postLoseDelayToMenu);
        ReturnToMenuOrReload();

        _sequence = null;
    }

    private int CountVillagersAlive()
    {
        var villagers = FindObjectsByType<Villager>(FindObjectsSortMode.None);
        int alive = 0;
        for (int i = 0; i < villagers.Length; i++)
        {
            if (villagers[i].isAlive)
            {
                alive++;
            }
        }

        return alive;
    }

    private void ReturnToMenuOrReload()
    {
        if (!string.IsNullOrEmpty(_menuSceneName))
        {
            if (Application.CanStreamedLevelBeLoaded(_menuSceneName))
            {
                SceneManager.LoadScene(_menuSceneName);
                return;
            }

            if (_logDebug)
            {
                DebugManager.LogWarning(
                    $"NightOutcomeManager: Menu scene '{_menuSceneName}' not found.",
                    this);
            }
        }

        if (_reloadIfMenuMissing)
        {
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
        }
    }
}
