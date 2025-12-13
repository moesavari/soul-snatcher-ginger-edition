using Game.Systems;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ReputationMeterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider _slider;
    [SerializeField] private Image _background;
    [SerializeField] private Image _leftOverlay;
    [SerializeField] private Image _rightOverlay;
    [SerializeField] private RectTransform _handle;
    [SerializeField] private Image _centerIcon;

    [Header("Tier Sprites (order matches tiers)")]
    [SerializeField] private Sprite[] _tierSprites;

    [Header("Config")]
    [SerializeField] private float _lerpSpeed = 10f;
    [SerializeField] private bool _snapWholeNumbers = false;

    [Header("Tiers")]
    [Tooltip("Edges BETWEEN tiers, sorted low→high.")]
    [SerializeField] private float[] _tierEdges = new float[] { -70f, -40f, -10f, 10f, 40f, 70f };
    [SerializeField] private float _tierHysteresis = 2f;

    [Header("Handle FX")]
    [SerializeField] private float _edgePulseStart = 0.9f;
    [SerializeField] private float _edgePulseScale = 0.08f;

    [Header("Slider Disabled Colors (by tier)")]
    [Tooltip("Inspector-friendly palette applied to Slider.colors.disabledColor based on the current tier.")]
    [SerializeField]
    private Color[] _tierDisabledColors = new Color[]
    {
        new Color(0.45f,0.10f,0.10f,1f),
        new Color(0.55f,0.20f,0.15f,1f),
        new Color(0.60f,0.45f,0.25f,1f),
        new Color(0.35f,0.35f,0.35f,1f),
        new Color(0.45f,0.65f,0.35f,1f),
        new Color(0.35f,0.75f,0.50f,1f),
        new Color(0.30f,0.85f,0.60f,1f)
    };

    public float value { get; private set; }
    public float normalized { get; private set; }
    public int tier { get; private set; } = -1;

    private float _targetValue;
    private int _lastTier = int.MinValue;
    private int _lastAppliedDisabledColorTier = int.MinValue;
    private bool _warnedColorCount;

    private void Reset()
    {
        _slider = GetComponent<Slider>();
    }

    private void Awake()
    {
        if (_slider == null) _slider = GetComponent<Slider>();

        _slider.minValue = ReputationSystem.MIN_REPUTATION;
        _slider.maxValue = ReputationSystem.MAX_REPUTATION;

        _slider.wholeNumbers = false;
        SetImmediate(0f);

        ReputationSystem.Instance.OnReputationChanged += HandleReputationChanged;
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

    private void OnDestroy()
    {
        if (ReputationSystem.Instance != null)
        {
            ReputationSystem.Instance.OnReputationChanged -= HandleReputationChanged;
        }
    }

    public void SetImmediate(float v)
    {
        _targetValue = Mathf.Clamp(v, _slider.minValue, _slider.maxValue);
        value = _targetValue;
        _slider.SetValueWithoutNotify(value);
        ApplyVisuals(value);
    }

    public void AnimateTo(float v)
    {
        _targetValue = Mathf.Clamp(v, _slider.minValue, _slider.maxValue);
    }

    public void AnimateToNormalized(float n)
    {
        n = Mathf.Clamp01(n);
        AnimateTo(Mathf.Lerp(_slider.minValue, _slider.maxValue, n));
    }

    private void HandleReputationChanged(int newRep)
    {
        AnimateTo(newRep);
    }

    private void ApplyVisuals(float v)
    {
        normalized = Mathf.InverseLerp(_slider.minValue, _slider.maxValue, v);

        if (_handle)
        {
            float edge = Mathf.InverseLerp(_edgePulseStart, 1f, Mathf.Abs(normalized - 0.5f) * 2f);
            float scale = 1f + _edgePulseScale * edge;
            _handle.localScale = new Vector3(scale, scale, 1f);
        }

        int t = GetTierIndexWithHysteresis(v);

        if (t != _lastTier)
        {
            SwapTierIcon(t);
            _lastTier = t;
            tier = t;
        }

        ApplyDisabledColorByTier(t);
    }

    private int GetTierIndexWithHysteresis(float v)
    {
        int n = (_tierEdges != null) ? _tierEdges.Length : 0;
        if (n == 0) return 0;

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
            DebugManager.LogWarning($"Missing sprite for tier {t}. " +
                             $"Provided sprites = {(_tierSprites == null ? 0 : _tierSprites.Length)}", this);
        }
    }

    private void ApplyDisabledColorByTier(int t)
    {
        if (_slider == null) return;
        if (t == _lastAppliedDisabledColorTier) return;

        int colorIndex = t;
        if (_tierDisabledColors == null || _tierDisabledColors.Length == 0)
        {
            if (!_warnedColorCount)
            {
                DebugManager.LogWarning("No _tierDisabledColors set; disabled color will not change.", this);
                _warnedColorCount = true;
            }
            return;
        }
        if (colorIndex < 0) colorIndex = 0;
        if (colorIndex >= _tierDisabledColors.Length)
        {
            if (!_warnedColorCount)
            {
                DebugManager.LogWarning($"Tier {t} exceeds color array length {_tierDisabledColors.Length}. " +
                                 $"Clamping to last color.", this);
                _warnedColorCount = true;
            }
            colorIndex = _tierDisabledColors.Length - 1;
        }

        var cb = _slider.colors;
        cb.disabledColor = _tierDisabledColors[colorIndex];
        _slider.colors = cb;

        _lastAppliedDisabledColorTier = t;
    }

    private void OnValidate()
    {
        if (_slider)
        {
            _slider.minValue = ReputationSystem.MIN_REPUTATION;
            _slider.maxValue = ReputationSystem.MAX_REPUTATION;

            _slider.wholeNumbers = false;
        }
    }
}
