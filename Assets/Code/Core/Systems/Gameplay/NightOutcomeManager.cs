using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NightOutcomeManager : MonoBehaviour
{
    [SerializeField] private string _menuSceneName = "MainMenu";
    [SerializeField] private bool _reloadIfMenuMissing = true;

    [Header("UI")]
    [SerializeField] private HUDRoot _hud;

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

    private void OnEnable()
    {
        GameEvents.AllZombiesCleared += OnNightSuccess;
        GameEvents.RoundLost += OnNightFail;
    }
    private void OnDisable()
    {
        GameEvents.AllZombiesCleared -= OnNightSuccess;
        GameEvents.RoundLost -= OnNightFail;
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

    private IEnumerator RunWin()
    {
        var rewards = RewardCalculator.Compute(
            wavesCleared: 1,
            villagersAlive: CountVillagersAlive(),
            difficultyTier: 0);

        _hud?.ShowWinBanner(rewards.souls, rewards.reputation, rewards.extra);

        if (_winCue) AudioManager.Instance?.PlayCue(_winCue);

        yield return CanvasGroupFader.Fade(_hud.bannerGroup, 0f, 1f, _fadeIn);
        yield return new WaitForSeconds(_hold);
        yield return CanvasGroupFader.Fade(_hud.bannerGroup, 1f, 0f, _fadeOut);

        if (_logDebug)
        {
            DebugManager.Log(
                $"Rewards: +{rewards.souls} souls, +{rewards.reputation} rep ({rewards.extra})",
                this);
        }

        yield return new WaitForSeconds(_postWinDelayToDay);

        _sequence = null;
    }

    private IEnumerator RunLose()
    {
        _hud?.ShowLoseBanner();

        if (_loseCue) AudioManager.Instance?.PlayCue(_loseCue);

        yield return CanvasGroupFader.Fade(_hud.bannerGroup, 0f, 1f, _fadeIn);
        yield return new WaitForSeconds(_hold);

        if (DebugManager.useSoftResetOnLose)
        {
            if (_logDebug) DebugManager.Log("Debug override active → soft reset instead of menu.", this);
            yield return new WaitForSeconds(_postLoseDelayToMenu);
            DebugManager.TriggerSoftReset();
            _sequence = null;
            yield break;
        }

        yield return new WaitForSeconds(_postLoseDelayToMenu);
        ReturnToMenuOrReload();

        _sequence = null;
    }

    private void ReturnToMenuOrReload()
    {
        if (!string.IsNullOrEmpty(_menuSceneName) && Application.CanStreamedLevelBeLoaded(_menuSceneName))
        {
            if(_logDebug) DebugManager.Log($"Loading menu scene '{_menuSceneName}'.", this);
            SceneManager.LoadScene(_menuSceneName);
            return;
        }

        if (Application.CanStreamedLevelBeLoaded(0))
        {
            if (_logDebug) DebugManager.Log("Loading build index 0 (fallback, this).");
            SceneManager.LoadScene(0);
            return;
        }

        if (_reloadIfMenuMissing)
        {
            var scene = SceneManager.GetActiveScene();
            if (_logDebug) DebugManager.Log($"Reloading active scene '{scene.name}'.", this);
            SceneManager.LoadScene(scene.name);
        }
        else
        {
            DebugManager.LogWarning("No menu scene found and reload disabled. Staying on Lose Banner.", this);
        }
    }

    private int CountVillagersAlive()
    {
        var villagers = FindObjectsByType<Villager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int alive = 0;
        foreach (var v in villagers) if (v && v.isAlive) alive++;
        return alive;
    }
}
