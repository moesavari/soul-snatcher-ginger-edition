using System.Linq;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]

public class HairVisuals : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] private GenderType _gender = GenderType.Male;
    [SerializeField] private HairProfile _maleProfile;
    [SerializeField] private HairProfile _femaleProfile;

    [Header("Stage Mapping")]
    [Tooltip("Soul thresholds per stage")]
    [SerializeField] private int[] _soulThresholds = new[] { 0, 10, 20 };

    private SpriteRenderer _sr;
    private HairProfile _activeProfile;
    private int _lastStage = -1;

    public event System.Action<int> OnHairStageChanged;
    public int CurrentStage => _lastStage;

    private void Awake()
    {
        _sr = this.Require<SpriteRenderer>();
        ResolveActiveProfile();
        if (_soulThresholds == null || _soulThresholds.Length == 0)
            _soulThresholds = new[] { 0 };
    }

    private void OnEnable()
    {
        var ss = SoulSystem.Instance;
        if (ss != null)
        {
            ss.OnSoulsChanged += HandleSoulsChanged;
            HandleSoulsChanged(ss.souls);
        }
        else Invoke(nameof(TryLateSubscribe), 0.05f);
    }

    private void OnDisable()
    {
        var ss = SoulSystem.Instance;
        if (ss != null) ss.OnSoulsChanged -= HandleSoulsChanged;
    }

    private void TryLateSubscribe()
    {
        var ss = SoulSystem.Instance;
        if (ss != null)
        {
            ss.OnSoulsChanged += HandleSoulsChanged;
            HandleSoulsChanged(ss.souls);
        }
    }

    private void HandleSoulsChanged(int soulCount)
    {
        SetHairStageBySouls(soulCount);
    }

    private void OnValidate()
    {
        if (_soulThresholds != null && _soulThresholds.Length > 1)
            _soulThresholds = _soulThresholds.Distinct().OrderBy(v => v).ToArray();

        ResolveActiveProfile();
    }

    public void SetGender(GenderType gender)
    {
        _gender = gender;
        ResolveActiveProfile();
        ForceRefreshStage(_lastStage < 1 ? 1 : _lastStage);
    }

    public void SetHairStageBySouls(int soulCount)
    {
        int stage = 1;
        for (int i = 0; i < _soulThresholds.Length; i++)
            if (soulCount >= _soulThresholds[i])
                stage = i + 1;

        SetHairStage(stage);
    }

    public void SetHairStage(int stage)
    {
        if (_activeProfile == null || _activeProfile.stageCount == 0)
        {
            Debug.LogWarning("[HairVisuals] Active profile missing or empty.");
            return;
        }

        int clamped = Mathf.Clamp(stage, 1, _activeProfile.stageCount);
        if (clamped == _lastStage) return;

        Sprite sprite = _activeProfile.GetStageSprite(clamped);
        if (sprite == null)
        {
            Debug.LogWarning($"[HairVisuals] No sprite for stage {clamped} on profile {_activeProfile.name}.");
            return;
        }

        _sr.sprite = sprite;
        _lastStage = clamped;
        OnHairStageChanged?.Invoke(_lastStage);
    }

    private void ResolveActiveProfile()
    {
        _activeProfile = _gender switch
        { 
            GenderType.Male     => _maleProfile,
            GenderType.Female   => _femaleProfile,
            _                   => null
        };

        if (_activeProfile == null)
            Debug.LogWarning($"[HairVisuals] No profile assigned for {_gender}. Assign in inspector");
    }

    private void ForceRefreshStage(int stage)
    {
        _lastStage = -1;
        SetHairStage(stage);
    }
}
