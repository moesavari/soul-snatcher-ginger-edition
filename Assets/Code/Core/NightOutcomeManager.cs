using System.Collections;
using UnityEngine;
// using UnityEngine.SceneManagement; // keep commented until you wire main menu

public class NightOutcomeManager : MonoBehaviour
{
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
        // Example inputs to RewardCalculator; wire real data as needed:
        var rewards = RewardCalculator.Compute(
            wavesCleared: 1,              // replace with your actual value
            villagersAlive: CountVillagersAlive(),
            difficultyTier: 0);

        _hud?.ShowWinBanner(rewards.souls, rewards.reputation, rewards.extra);

        if (_winCue) AudioManager.Instance?.PlayCue(_winCue);

        yield return CanvasGroupFader.Fade(_hud.bannerGroup, 0f, 1f, _fadeIn);
        yield return new WaitForSeconds(_hold);
        yield return CanvasGroupFader.Fade(_hud.bannerGroup, 1f, 0f, _fadeOut);

        // Apply rewards (stub – replace with your wallet/rep systems)
        if (_logDebug) DebugManager.Log($"[Outcome] Rewards: +{rewards.souls} souls, +{rewards.reputation} rep ({rewards.extra})");
        // Wallet.AddSouls(rewards.souls);
        // Reputation.Add(rewards.reputation);

        yield return new WaitForSeconds(_postWinDelayToDay);
        GameEvents.RaiseDay();

        _sequence = null;
    }

    private IEnumerator RunLose()
    {
        _hud?.ShowLoseBanner();

        if (_loseCue) AudioManager.Instance?.PlayCue(_loseCue);

        yield return CanvasGroupFader.Fade(_hud.bannerGroup, 0f, 1f, _fadeIn);
        yield return new WaitForSeconds(_hold);
        // Keep the lose banner up until menu, or fade out if you prefer:
        // yield return CanvasGroupFader.Fade(_hud.bannerGroup, 1f, 0f, _fadeOut);

        yield return new WaitForSeconds(_postLoseDelayToMenu);

        // TODO: implement later
        // SceneManager.LoadScene("MainMenu");
        if (_logDebug) DebugManager.Log("[Outcome] Would return to Main Menu here.");

        _sequence = null;
    }

    private int CountVillagersAlive()
    {
        var villies = GameObject.FindGameObjectsWithTag("Villager");
        int alive = 0;
        foreach (var v in villies)
        {
            var h = v.GetComponent<Health>();
            if (h != null && !h.isDead) alive++;
        }
        return alive;
    }
}
