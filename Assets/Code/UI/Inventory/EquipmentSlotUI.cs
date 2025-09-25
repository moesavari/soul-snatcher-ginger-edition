using Game.Core.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private EquipmentSlotType _slotType;
    [SerializeField] private Image _icon;

    private Equipment _equipment;
    private CharacterSheetUI _sheet;

    public void Bind(Equipment equipment, CharacterSheetUI sheet)
    {
        _equipment = equipment;
        _sheet = sheet;
        Refresh();
    }

    public void Refresh()
    {
        if (_icon == null || _equipment == null) return;
        var def = _equipment.GetEquipped(_slotType);
        _icon.enabled = def != null && def.icon != null;
        _icon.sprite = def != null ? def.icon : null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null) return;
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Left: _sheet?.TryEquipTo(_slotType); break;
            case PointerEventData.InputButton.Right: _sheet?.TryUnequip(_slotType); break;
            default: Debug.LogWarning("[EquipmentSlotUI] Unsupported button."); break;
        }
    }
}