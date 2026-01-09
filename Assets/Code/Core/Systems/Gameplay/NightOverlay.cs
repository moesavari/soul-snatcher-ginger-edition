using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NightOverlay : MonoSingleton<NightOverlay>
{
    [SerializeField] private Image _nightTimeSheet;
    [SerializeField] private float _fadeSeconds = 0.5f;

    [Header("Rebind")]
    [SerializeField] private string _overlayTag = "NightOverlay";
    [SerializeField] private string _overlayNameFallback = "NightTimeSheet";

    private Coroutine _fadeRoutine;

    protected override void Awake()
    {
        base.Awake();
        TryRebindOverlay();
    }

    public void TryRebindOverlay()
    {
        if (_nightTimeSheet != null) return;

        // 1) Prefer tag
        if (!string.IsNullOrWhiteSpace(_overlayTag))
        {
            var tagged = GameObject.FindWithTag(_overlayTag);
            if (tagged != null)
            {
                _nightTimeSheet = tagged.GetComponent<Image>();
            }
        }

        // 2) Fallback by name
        if (_nightTimeSheet == null && !string.IsNullOrWhiteSpace(_overlayNameFallback))
        {
            var go = GameObject.Find(_overlayNameFallback);
            if (go != null)
            {
                _nightTimeSheet = go.GetComponent<Image>();
            }
        }

        // 3) Final fallback (slow, but safe once)
        if (_nightTimeSheet == null)
        {
            _nightTimeSheet = FindFirstObjectByType<Image>(FindObjectsInactive.Include);
        }

        if (_nightTimeSheet == null)
        {
            Debug.LogWarning("NightOverlay: Could not find overlay Image to bind (tag/name).");
        }
    }

    public void SetNight(bool isNight)
    {
        if (_nightTimeSheet == null)
        {
            TryRebindOverlay();
        }

        if (_nightTimeSheet == null) return;

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeTo(isNight ? 0.6f : 0f));
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        var c = _nightTimeSheet.color;
        var startAlpha = c.a;

        if (Mathf.Approximately(startAlpha, targetAlpha))
            yield break;

        float t = 0f;
        float dur = Mathf.Max(0.0001f, _fadeSeconds);

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            _nightTimeSheet.color = c;
            yield return null;
        }

        c.a = targetAlpha;
        _nightTimeSheet.color = c;
    }
}
