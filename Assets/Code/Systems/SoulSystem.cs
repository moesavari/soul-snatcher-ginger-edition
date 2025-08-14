using UnityEngine;

public class SoulSystem : MonoSingleton<SoulSystem>
{
    [SerializeField] private int _maxSouls = 30;
    [SerializeField] private HairVisuals _hairVisuals;

    private int _soulCount;
    public int soulCount => _soulCount;

    public delegate void SoulChangeEvent(int newSoulCount);
    public static event SoulChangeEvent OnSoulChange;

    protected override void Awake()
    {
        base.Awake();
    }

    public void AbsorbSoul(int amount)
    {
        _soulCount = Mathf.Clamp(_soulCount + amount, 0, _maxSouls);
        UpdateHairVisuals();
        OnSoulChange?.Invoke(_soulCount);
    }

    public void SpendSouls(int amount)
    {
        if (_soulCount < amount) return;

        _soulCount -= amount;
        UpdateHairVisuals();
        OnSoulChange?.Invoke(_soulCount);
    }

    public bool CanUnlock(int cost) => _soulCount >= cost;

    private void UpdateHairVisuals()
    {
        if (_hairVisuals == null) return;
        _hairVisuals.SetHairStage(_soulCount >= 20 ? 3 : _soulCount >= 10 ? 2 : 1);
    }
}
