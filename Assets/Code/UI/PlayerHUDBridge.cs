using UnityEngine;

[DisallowMultipleComponent]

public class PlayerHUDBridge : MonoBehaviour, ISoulReadOnly, IHairStageReadOnly
{
    [SerializeField] private SoulSystem _soulSystem;
    [SerializeField] private HairVisuals _hairVisuals;

    public int souls => _soulSystem != null ? _soulSystem.souls : 0;
    public event System.Action<int> SoulsChanged;

    public int hairStage => _hairVisuals != null ? _hairVisuals.CurrentStage : 0;
    public event System.Action<int> HairStageChanged;

    private void Awake()
    {
        if(_soulSystem == null) _soulSystem = SoulSystem.Instance;
        if(_hairVisuals == null) _hairVisuals = GetComponentInChildren<HairVisuals>();
    }

    private void OnEnable()
    {
        if (_soulSystem != null) _soulSystem.OnSoulsChanged += HandleSoulsChanged;
        if (_hairVisuals != null) _hairVisuals.OnHairStageChanged += HandleHairStageChanged;
    }

    private void OnDisable()
    {
        if (_soulSystem != null) _soulSystem.OnSoulsChanged -= HandleSoulsChanged;
        if (_hairVisuals != null) _hairVisuals.OnHairStageChanged -= HandleHairStageChanged;
    }

    private void HandleSoulsChanged(int newSouls) => SoulsChanged?.Invoke(newSouls);
    private void HandleHairStageChanged(int newStage) => HairStageChanged?.Invoke(newStage);
}
