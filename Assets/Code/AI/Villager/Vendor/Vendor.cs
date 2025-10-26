using Game.Systems;
using UnityEngine;

[DisallowMultipleComponent]
public class Vendor : Villager
{
    [Header("Vendor")]
    [SerializeField] private string _vendorName;
    [SerializeField] private VendorInventory _inventory;
    [SerializeField] private VendorReputationRules _repRules;
    [SerializeField] private int _defaultAddQuantity = 1;

    [Header("Day settings")]
    [SerializeField] private bool _openAtNight = false;
    [SerializeField] private int _restockEveryNDays = 1;

    private int _dayCounter;

    private float _currentRepPriceMult = 1f;
    public float currentRepPriceMult => _currentRepPriceMult;

    private VendorInventory _runtimeInventory;
    public VendorInventory runtimeInventory => _runtimeInventory;

    public VendorInventory inventory => _inventory;

    public string vendorName => _vendorName;

    protected override void Awake()
    {
        base.Awake();
        _runtimeInventory = Instantiate(_inventory);
        ReputationSystem.Instance.OnReputationChanged += OnRepChanged;
    }

    private void OnEnable()
    {
        GameEvents.DayStarted += OnDayStart;
        GameEvents.NightStarted += OnNightStart;
    }

    private void OnDisable()
    {
        GameEvents.DayStarted -= OnDayStart;
        GameEvents.NightStarted -= OnNightStart;
    }

    private void OnDestroy()
    {
        if (ReputationSystem.Instance != null)
            ReputationSystem.Instance.OnReputationChanged -= OnRepChanged;
    }

    private void OnDayStart()
    {
        _isNight = false;
        _dayCounter++;
        if (_dayCounter % _restockEveryNDays == 0) Restock();
    }

    private void OnNightStart()
    {
        _isNight = true;
        if (!_openAtNight) CloseShop();
    }

    private void Restock()
    {
        foreach (var src in _inventory.stock)
        {
            if (!src.item) continue;
            _runtimeInventory.Ensure(src.item, src.quantity, src.overridePrice);
        }
    }

    public void OpenShop()
    {
        if(!isAlive || _inventory == null) return;
        if (_isNight && !_openAtNight) return;

        ShopController.Instance?.Open(this);
    }

    public void CloseShop()
    {
        if (ShopController.Instance != null && ShopController.Instance.activeVendor == this)
            ShopController.Instance.Close();
    }

    private void OnRepChanged(int rep) => ApplyRepRules(rep);

    private void ApplyRepRules(int rep)
    {
        if(_repRules == null) return;   

        var r = _repRules.GetRangeFor(rep);
        if(r == null) return;

        foreach (var item in r.remove)
        {
            if (item) _runtimeInventory.RemoveAll(item);
        }

        foreach (var item in r.add)
        {
            if (item) _runtimeInventory.Ensure(item, _defaultAddQuantity, -1);
        }

        foreach(var o in r.overrides)
        {
            if (o.item) _runtimeInventory.SetOverridePrice(o.item, o.price);
        }

        _currentRepPriceMult = Mathf.Max(0.01f, r.priceMultiplier);
    }
}
