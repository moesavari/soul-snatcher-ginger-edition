using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltipUI : MonoBehaviour
{
    [Header("Scene refs")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _root;

    [Header("UI")]
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _desc;

    [Header("Behavior")]
    [SerializeField] private Vector2 _mouseOffset = new(18f, -18f);
    [SerializeField] private bool _followMouse = true;

    private bool _visible;

    private UIPanelID _ownerPanel = UIPanelID.None;
    private Component _ownerWidget;

    private void Awake()
    {
        if (!_canvas) _canvas = GetComponentInParent<Canvas>(true);
        Hide();
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

    private void OnValidate()
    {
        if (_root && _canvas && _root == _canvas.transform as RectTransform)
            DebugManager.LogError("Root must be the tooltip panel, not the Canvas.", this);
    }

    private void Update()
    {
        if (_followMouse && _visible) Position(Input.mousePosition);
    }


    private void LateUpdate()
    {
        if (_visible && _ownerWidget & !_ownerWidget.gameObject.activeInHierarchy)
            Hide();
    }

    private void OnInventoryVisibility(bool visible)
    {
        if (!visible && _visible && _ownerPanel == UIPanelID.Inventory)
            Hide();
    }

    private void OnEquipmentVisibility(bool visible)
    {
        if (!visible && _visible && _ownerPanel == UIPanelID.Equipment)
            Hide();
    }

    public void ShowFrom(UIPanelID ownerPanel, Component ownerWidget, ItemDef def, Vector2 screenPos)
    {
        _ownerPanel = ownerPanel;
        _ownerWidget = ownerWidget;

        Show(def, screenPos);
    }

    public void Show(ItemDef def, Vector2 screenPos)
    {
        if (_ownerPanel == UIPanelID.None) _ownerPanel = UIPanelID.Inventory;

        if (!def || !_root || !_canvas) { Hide(); return; }
        _title.text = def.displayName;
        _desc.text = BuildDesc(def);
        _icon.enabled = def.icon;
        _icon.sprite = def.icon;

        _visible = true;
        _root.gameObject.SetActive(true);
        Position(screenPos);
    }

    public void Move(Vector2 screenPos)
    {
        if (_visible) Position(screenPos);
    }

    public void Hide()
    {
        _visible = false;

        _ownerPanel = UIPanelID.None;
        _ownerWidget = null;

        if (_root) _root.gameObject.SetActive(false);
    }

    private void Position(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_canvas.transform, screenPos, _canvas.worldCamera, out var local);

        local += _mouseOffset;

        var canvasRect = (RectTransform)_canvas.transform;
        var r = _root.rect;
        var bounds = canvasRect.rect;

        float halfW = r.width * 0.5f, halfH = r.height * 0.5f;
        local.x = Mathf.Clamp(local.x, bounds.xMin + halfW, bounds.xMax - halfW);
        local.y = Mathf.Clamp(local.y, bounds.yMin + halfH, bounds.yMax - halfH);

        _root.anchoredPosition = local;
    }

    private string BuildDesc(ItemDef def)
    {
        return def.description;
    }
}
