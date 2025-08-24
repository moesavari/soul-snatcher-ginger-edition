using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NightOverlay : MonoSingleton<NightOverlay>
{
    [SerializeField] private Image _overlay;
    [SerializeField] private float _transitionSeconds = 1f;

    public void SetNight(bool isNight)
    {
        StopAllCoroutines();
        StartCoroutine(LerpAlpha(isNight ? 0.6f : 0f));
    }

    private IEnumerator LerpAlpha(float target)
    {
        float start = _overlay.color.a;
        float t = 0f;

        var c = _overlay.color;

        while (t < 1f)
        {
            t += Time.deltaTime / _transitionSeconds;
            c.a = Mathf.Lerp(start, target, t);
            _overlay.color = c;

            yield return null;
        }

        c.a = target;
        _overlay.color = c;
    }
}
