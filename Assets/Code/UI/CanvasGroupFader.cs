using System.Collections;
using UnityEngine;

public class CanvasGroupFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private float _duration = 0.15f;

    private Coroutine _co;

    // Instance API (used by your UI windows)
    public void Fade(bool show)
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(FadeCo(_group, show ? 0f : 1f, show ? 1f : 0f, _duration, show, show));
    }

    // ------------------------------
    // Static overloads (compat layer)
    // ------------------------------

    // Matches calls like: CanvasGroupFader.Fade(group, from, to, duration)
    public static Coroutine Fade(CanvasGroup group, float from, float to, float duration)
    {
        return Runner.Run(group, from, to, duration, to > from, to > from);
    }

    // Slightly higher-level: specify end alpha + duration
    public static Coroutine FadeTo(CanvasGroup group, float to, float duration)
    {
        float from = group ? group.alpha : 0f;
        return Runner.Run(group, from, to, duration, to > from, to > from);
    }

    // Instance -> static bridge if someone passes an instance
    public static Coroutine Fade(CanvasGroupFader fader, bool show)
    {
        if (!fader) return null;
        if (fader._co != null) fader.StopCoroutine(fader._co);
        fader._co = Runner.Run(fader._group, show ? 0f : 1f, show ? 1f : 0f, fader._duration, show, show);
        return fader._co;
    }

    // Shared coroutine
    private static IEnumerator FadeCo(CanvasGroup group, float from, float to, float duration, bool blocks, bool interact)
    {
        if (!group) yield break;
        float t = 0f;
        group.alpha = from;
        group.blocksRaycasts = true;
        group.interactable = true;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / Mathf.Max(0.0001f, duration));
            yield return null;
        }

        group.alpha = to;
        group.blocksRaycasts = blocks;
        group.interactable = interact;
    }

    private static class Runner
    {
        private static CanvasGroupFaderRunner _runner;

        public static Coroutine Run(CanvasGroup group, float from, float to, float duration, bool blocks, bool interact)
        {
            if (!_runner)
            {
                var go = new GameObject("");
                Object.DontDestroyOnLoad(go);
                _runner = go.AddComponent<CanvasGroupFaderRunner>();
            }
            return _runner.StartCoroutine(FadeCo(group, from, to, duration, blocks, interact));
        }
    }

    private class CanvasGroupFaderRunner : MonoBehaviour { }
}
