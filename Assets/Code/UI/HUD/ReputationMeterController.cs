using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ReputationMeterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider _slider;
    [SerializeField] private Image _background;          // optional, not modified
    [SerializeField] private Image _leftOverlay;         // NOT modified anymore
    [SerializeField] private Image _rightOverlay;        // NOT modified anymore
    [SerializeField] private RectTransform _handle;      // optional pulse
    [SerializeField] private Image _centerIcon;          // swaps by tier

    [Header("Tier Sprites (order matches tiers)")]
    [SerializeField] private Sprite[] _tierSprites;

    [Header("Config")]
    [SerializeField] private float _min = -100f;
    [SerializeField] private float _max = 100f;
    [SerializeField] private float _lerpSpeed = 10f;
    [SerializeField] private bool _snapWholeNumbers = false;

    [Header("Tiers")]
    [Tooltip("Edges BETWEEN tiers, sorted low→high. Example (7): -70,-40,-10,10,40,70")]
    [SerializeField] private float[] _tierEdges = new float[] { -70f, -40f, -10f, 10f, 40f, 70f };
    [SerializeField] private float _tierHysteresis = 2f;

    [Header("Handle FX")]
    [SerializeField] private float _edgePulseStart = 0.9f;
    [SerializeField] private float _edgePulseScale = 0.08f;

    public float value { get; private set; }
    public float normalized { get; private set; }
    public int tier { get; private set; } = -1;

    private float _targetValue;
    private int _lastTier = int.MinValue;

    private void Reset()
    {
        _slider = GetComponent<Slider>();
    }

    private void Awake()
    {
        if (_slider == null) _slider = GetComponent<Slider>();
        _slider.minValue = _min;
        _slider.maxValue = _max;
        _slider.wholeNumbers = false;
        SetImmediate(0f);
    }

    private void Update()
    {
        if (!Mathf.Approximately(value, _targetValue))
        {
            value = Mathf.MoveTowards(value, _targetValue, _lerpSpeed * Time.unscaledDeltaTime);
            if (_snapWholeNumbers) value = Mathf.Round(value);
            _slider.SetValueWithoutNotify(value);
            ApplyVisuals(value);
        }
    }

    // --- Public API ----------------------------------------------------------
    public void SetImmediate(float v)
    {
        _targetValue = Mathf.Clamp(v, _min, _max);
        value = _targetValue;
        _slider.SetValueWithoutNotify(value);
        ApplyVisuals(value);
    }

    public void AnimateTo(float v)
    {
        _targetValue = Mathf.Clamp(v, _min, _max);
    }

    public void AnimateToNormalized(float n)
    {
        n = Mathf.Clamp01(n);
        AnimateTo(Mathf.Lerp(_min, _max, n));
    }

    // --- Internals -----------------------------------------------------------
    private void ApplyVisuals(float v)
    {
        normalized = Mathf.InverseLerp(_min, _max, v);

        // Handle pulse near extremes (visual only)
        if (_handle)
        {
            float edge = Mathf.InverseLerp(_edgePulseStart, 1f, Mathf.Abs(normalized - 0.5f) * 2f);
            float scale = 1f + _edgePulseScale * edge;
            _handle.localScale = new Vector3(scale, scale, 1f);
        }

        // Center icon tier selection
        int t = GetTierIndexWithHysteresis(v);
        if (t != _lastTier)
        {
            SwapTierIcon(t);
            _lastTier = t;
            tier = t;
        }
    }

    private int GetTierIndexWithHysteresis(float v)
    {
        int n = (_tierEdges != null) ? _tierEdges.Length : 0;
        if (n == 0) return 0;

        // keep in current tier unless we move past widened bounds
        if (_lastTier >= 0 && _lastTier <= n)
        {
            float leftBound = _lastTier == 0 ? float.NegativeInfinity : _tierEdges[_lastTier - 1];
            float rightBound = _lastTier == n ? float.PositiveInfinity : _tierEdges[_lastTier];
            leftBound -= _tierHysteresis;
            rightBound += _tierHysteresis;
            if (v > leftBound && v <= rightBound) return _lastTier;
        }

        for (int i = 0; i < n; i++)
            if (v <= _tierEdges[i]) return i;

        return n;
    }

    private void SwapTierIcon(int t)
    {
        if (_centerIcon == null) return;

        var sprite = (t >= 0 && _tierSprites != null && t < _tierSprites.Length)
            ? _tierSprites[t]
            : null;

        _centerIcon.sprite = sprite;

        if (sprite == null)
        {
            Debug.LogWarning($"ReputationMeterController: Missing sprite for tier {t}. " +
                             $"Provided sprites = {(_tierSprites == null ? 0 : _tierSprites.Length)}");
        }
    }

    private void OnValidate()
    {
        if (_slider)
        {
            _slider.minValue = _min;
            _slider.maxValue = _max;
            _slider.wholeNumbers = false;
        }
    }
}
