using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemContextMenuUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _root;
    [SerializeField] private Button _equipBtn;
    [SerializeField] private Button _unequipBtn;
    [SerializeField] private Button _useBtn;
    [SerializeField] private Button _splitBtn;
    [SerializeField] private Button _destroyBtn;
    [SerializeField] private TMP_Text _title;

    private ItemDef _item;
    private bool _isEquipped;
    private Action<ItemDef> _onEquip, _onUnequip, _onUse, _onDestroy, _onSplit;

    private void Awake()
    {
        if (!_canvas) _canvas = GetComponentInParent<Canvas>(true);

        Hide();
    }

    public void Init(Action<ItemDef> onEquip, Action<ItemDef> onUnequip, Action<ItemDef> onUse, Action<ItemDef> onDestroy, Action<ItemDef> onSplit)
    {
        _onEquip = onEquip;
        _onUnequip = onUnequip;
        _onUse = onUse;
        _onDestroy = onDestroy;
        _onSplit = onSplit;

        _equipBtn.onClick.AddListener(() => { _onEquip?.Invoke(_item); Hide(); });
        _unequipBtn.onClick.AddListener(() => { _onUnequip?.Invoke(_item); Hide(); });
        _useBtn.onClick.AddListener(() => { _onUse?.Invoke(_item); Hide(); });
        _destroyBtn.onClick.AddListener(() => { _onDestroy?.Invoke(_item); Hide(); });
        _splitBtn.onClick.AddListener(() => { _onSplit?.Invoke(_item); Hide(); });
    }

    public void Show(ItemDef def, bool isEquipped, Vector2 screenPos)
    {
        _item = def;
        _isEquipped = isEquipped;
        _title.text = def ? def.displayName : "";

        _equipBtn.gameObject.SetActive(def && !isEquipped && def.equipSlot != EquipmentSlotType.None);
        _unequipBtn.gameObject.SetActive(def && isEquipped);
        _useBtn.gameObject.SetActive(def && def.kind == ItemKind.Consumable);
        _destroyBtn.gameObject.SetActive(def);
        _splitBtn.gameObject.SetActive(def && def.stackable);

        _root.gameObject.SetActive(true);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform, screenPos, _canvas.worldCamera, out var local);
        _root.anchoredPosition = local;
    }

    private void Hide() => _root.gameObject.SetActive(false);
}
