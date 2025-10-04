using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemContextMenuUI : MonoBehaviour
{
    [Header("Scene refs")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _root;

    [Header("UI")]
    [SerializeField] private TMP_Text _title;
    [SerializeField] private Button _equipBtn;
    [SerializeField] private Button _unequipBtn;
    [SerializeField] private Button _useBtn;
    [SerializeField] private Button _splitBtn;
    [SerializeField] private Button _destroyBtn;

    [Header("Behavior")]
    [SerializeField] private Vector2 _spawnOffset = new(12f, -12f);

    // callbacks provided by systems (for Show)
    private System.Action<ItemDef> _onEquip, _onUnequip, _onUse, _onDestroy, _onSplit;

    // state for button callbacks
    [SerializeField] private ItemDef _item;

    private UIPanelID _ownerPanel = UIPanelID.None;
    private Component _ownerWidget;
    private bool _isOpen = false;

    private void Awake()
    {
        if (!_canvas) _canvas = GetComponentInParent<Canvas>(true);
        Hide();
        WireDefaultListeners();
    }

    private void OnEnable()
    {
        InventoryBagUI.OnVisibilityChanged += OnInventoryVisibility;
        CharacterSheetUI.OnVisibilityChanged += OnEquipmentVisibility;
    }

    private void OnDisable()
    {
        InventoryBagUI.OnVisibilityChanged -= OnInventoryVisibility;
        CharacterSheetUI.OnVisibilityChanged -= OnEquipmentVisibility;
    }

    private void LateUpdate()
    {
        if (_isOpen && _ownerWidget && !_ownerWidget.gameObject.activeInHierarchy)
            Hide();
    }

    private void OnValidate()
    {
        if (_root && _canvas && _root == _canvas.transform as RectTransform)
            DebugManager.LogError("Root must be the menu panel, not the Canvas.", this);
    }

    private void OnInventoryVisibility(bool visible)
    {
        if (!visible && _isOpen && _ownerPanel == UIPanelID.Inventory)
            Hide();
    }

    private void OnEquipmentVisibility(bool visible)
    {
        if (!visible && _isOpen && _ownerPanel == UIPanelID.Equipment)
            Hide();
    }

    private void WireDefaultListeners()
    {
        _equipBtn.onClick.RemoveAllListeners();
        _unequipBtn.onClick.RemoveAllListeners();
        _useBtn.onClick.RemoveAllListeners();
        _splitBtn.onClick.RemoveAllListeners();
        _destroyBtn.onClick.RemoveAllListeners();

        _equipBtn.onClick.AddListener(()        => { _onEquip?.Invoke(_item);       Hide(); });
        _unequipBtn.onClick.AddListener(()      => { _onUnequip?.Invoke(_item);     Hide(); });
        _useBtn.onClick.AddListener(()          => { _onUse?.Invoke(_item);         Hide(); });
        _splitBtn.onClick.AddListener(()        => { _onSplit?.Invoke(_item);       Hide(); });
        _destroyBtn.onClick.AddListener(()      => { _onDestroy?.Invoke(_item);     Hide(); });
    }

    public void Init(System.Action<ItemDef> onEquip,
                     System.Action<ItemDef> onUnequip,
                     System.Action<ItemDef> onUse,
                     System.Action<ItemDef> onDestroy,
                     System.Action<ItemDef> onSplit)
    {
        _onEquip = onEquip;
        _onUnequip = onUnequip;
        _onUse = onUse;
        _onDestroy = onDestroy;
        _onSplit = onSplit;
    }

    public void ShowFrom(UIPanelID ownerPanel, Component ownerWidget, ItemDef def, bool isEquipped, Vector2 screenPos)
    {
        _ownerPanel = ownerPanel;
        _ownerWidget = ownerWidget;
        _isOpen = true;

        Show(def, isEquipped, screenPos);
    }

    public void Show(ItemDef def, bool isEquipped, Vector2 screenPos)
    {
        _isOpen = true;
        if (_ownerPanel == UIPanelID.None) _ownerPanel = UIPanelID.Inventory;

        _item = def;
        _title.text = def ? def.displayName : string.Empty;

        _equipBtn.gameObject.SetActive(def && !isEquipped && def.equipSlot != EquipmentSlotType.None);
        _unequipBtn.gameObject.SetActive(def && isEquipped);
        _useBtn.gameObject.SetActive(def && def.kind == ItemKind.Consumable);
        _splitBtn.gameObject.SetActive(def && def.stackable);
        _destroyBtn.gameObject.SetActive(def);

        WireDefaultListeners();

        _root.gameObject.SetActive(true);
        Position(screenPos + _spawnOffset);
        _root.SetAsLastSibling();
    }

    public void Hide()
    {
        _isOpen = false;
        _ownerPanel = UIPanelID.None;
        _ownerWidget = null;

        _root.gameObject.SetActive(false);
    }

    private void Position(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_canvas.transform, screenPos,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
            out var local);

        var canvasRect = (RectTransform)_canvas.transform;
        var r = _root.rect;
        var bounds = canvasRect.rect;

        float halfW = r.width * 0.5f, halfH = r.height * 0.5f;
        local.x = Mathf.Clamp(local.x, bounds.xMin + halfW, bounds.xMax - halfW);
        local.y = Mathf.Clamp(local.y, bounds.yMin + halfH, bounds.yMax - halfH);

        _root.anchoredPosition = local;
    }

    public void ShowForEquipped(EquipmentSlotType slot, ItemDef def, Vector2 screenPos,
                                System.Action<string, EquipmentSlotType, ItemDef> onAction)
    {
        _isOpen = true;
        if (_ownerPanel == UIPanelID.None) _ownerPanel = UIPanelID.Equipment;

        _item = def;
        _title.text = def ? def.displayName : string.Empty;

        _equipBtn.gameObject.SetActive(false);
        _splitBtn.gameObject.SetActive(false);
        _unequipBtn.gameObject.SetActive(def);
        _useBtn.gameObject.SetActive(def && def.kind == ItemKind.Consumable);
        _destroyBtn.gameObject.SetActive(def);

        _equipBtn.onClick.RemoveAllListeners();
        _unequipBtn.onClick.RemoveAllListeners();
        _useBtn.onClick.RemoveAllListeners();
        _splitBtn.onClick.RemoveAllListeners();
        _destroyBtn.onClick.RemoveAllListeners();

        _unequipBtn.onClick.AddListener(() =>
        {
            onAction?.Invoke("Unequip", slot, def);
            Hide();
        });

        _useBtn.onClick.AddListener(() =>
        {
            onAction?.Invoke("Use", slot, def);
            Hide();
        });

        _destroyBtn.onClick.AddListener(() =>
        {
            onAction?.Invoke("Destroy", slot, def);
            Hide();
        });

        _root.gameObject.SetActive(true);
        Position(screenPos + _spawnOffset);
        _root.SetAsLastSibling();
    }
}
