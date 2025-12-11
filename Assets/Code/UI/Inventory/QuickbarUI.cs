using Game.Core.Inventory;
using UnityEngine;
using UnityEngine.UI;

public class QuickbarUI : MonoBehaviour
{
    [SerializeField] private Equipment _equipment;
    [SerializeField] private Button[] _buttons;
    [SerializeField] private Image[] _icons;
    [SerializeField] private CanvasGroup _dockGroup;

    public Equipment equipment => _equipment;

    private void Awake() { WireButtons(); }
    private void OnEnable() { EquipmentEvents.QuickbarChanged += HandleQuickbarChanged; RefreshIcons(); }
    private void OnDisable() { EquipmentEvents.QuickbarChanged -= HandleQuickbarChanged; }

    public void BindEquipment(Equipment eq) { _equipment = eq; RefreshIcons(); }

    public void SetDockVisible(bool show)
    {
        if (!_dockGroup) return;
        _dockGroup.alpha = show ? 1f : 0f;
        _dockGroup.blocksRaycasts = show;
        _dockGroup.interactable = show;
    }

    public void RefreshIcons()
    {
        if (_icons == null || _equipment == null) return;
        for (int i = 0; i < _icons.Length; i++)
        {
            var def = _equipment.GetQuickItem(i);
            var img = _icons[i];
            if (!img) continue;
            img.enabled = def != null && def.icon != null;
            img.sprite = def != null ? def.icon : null;
        }
    }

    private void WireButtons()
    {
        if (_buttons == null) return;
        for (int i = 0; i < _buttons.Length; i++)
        {
            int ix = i;
            _buttons[i]?.onClick.AddListener(() => TryAssignFromSelection(ix));
        }
    }

    private void TryAssignFromSelection(int index)
    {
        var sheet = GetComponentInParent<EquipmentUI>(true);
        if (!sheet) sheet = FindFirstObjectByType<EquipmentUI>(FindObjectsInactive.Include);
        if (!sheet) return;

    }

    private void HandleQuickbarChanged(int index, ItemDef def) => RefreshIcons();
}
