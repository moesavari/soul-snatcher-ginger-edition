using System.Text;
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
        if (_visible && _ownerWidget != null && !_ownerWidget.gameObject.activeInHierarchy)
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
        _ownerPanel = ownerPanel == UIPanelID.None ? UIPanelID.Inventory : ownerPanel;
        _ownerWidget = ownerWidget;

        SetContent(def);
        OpenAt(screenPos);
    }

    public void Show(ItemDef def, Vector2 screenPos)
    {
        if (_ownerPanel == UIPanelID.None) _ownerPanel = UIPanelID.Inventory;

        SetContent(def);
        OpenAt(screenPos);
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

    private void SetContent(ItemDef def)
    {
        if (!def || !_root || !_canvas) { Hide(); return; }

        if (_title) _title.text = def.displayName;

        if (_icon)
        {
            _icon.enabled = def.icon != null;
            _icon.sprite = def.icon;
        }

        if (_desc)
        {
            _desc.text = BuildDescriptionWithStats(def);
        }
    }

    private void OpenAt(Vector2 screenPos)
    {
        _visible = true;
        _root.gameObject.SetActive(true);
        Position(screenPos);
    }

    private string BuildDescriptionWithStats(ItemDef def)
    {
        if (def == null) return string.Empty;

        var sb = new StringBuilder(256);

        // 1) Description (top)
        if (!string.IsNullOrWhiteSpace(def.description))
            sb.AppendLine(def.description.Trim());

        // 2) Early out if no gear info to show
        bool hasStats = def.stats != null && def.stats.Count > 0;
        bool isGear = def.kind == ItemKind.Weapon || def.kind == ItemKind.Armor || def.kind == ItemKind.Accessory;

        if (!isGear && !hasStats && !def.hasEnchantment)
            return sb.ToString();

        // Spacer between description and the gear block
        if (sb.Length > 0) sb.AppendLine();

        // 3) Gear header row (optional but helpful context)
        if (isGear)
        {
            // Quality • Slot • Two-Handed
            sb.Append("<b>")
              .Append(def.quality);

            if (def.equipSlot != EquipmentSlotType.None)
                sb.Append(" • ").Append(def.equipSlot);

            if (def.twoHanded)
                sb.Append(" • Two-Handed");

            sb.AppendLine("</b>");
        }

        // 4) Stats list
        if (hasStats)
        {
            for (int i = 0; i < def.stats.Count; i++)
            {
                var s = def.stats[i];
                if (s == null) continue;

                // % vs flat formatting
                bool asPercent = s.statType == StatType.AttackSpeed
                              || s.statType == StatType.CritChance
                              || s.statType == StatType.MoveSpeed
                              || s.statType == StatType.CooldownReduction;

                sb.Append("• ");

                // Friendly names (customize to taste)
                string statName = s.statType switch
                {
                    StatType.AttackPower => "Attack Power",
                    StatType.SpellPower => "Spell Power",
                    StatType.AttackSpeed => "Attack Speed",
                    StatType.CritChance => "Crit Chance",
                    StatType.CooldownReduction => "Cooldown Reduction",
                    StatType.MoveSpeed => "Move Speed",
                    _ => s.statType.ToString()
                };

                if (asPercent)
                    sb.Append(statName).Append(": +").Append(s.value).Append('%');
                else
                    sb.Append(statName).Append(": +").Append(s.value);

                sb.AppendLine();
            }
        }

        // 5) Enchantment (Rare optional, Legendary guaranteed by editor rules)
        if (def.hasEnchantment && !string.IsNullOrWhiteSpace(def.enchantmentDescription))
        {
            sb.AppendLine()
              .Append("<i>")
              .Append(def.enchantmentDescription.Trim())
              .Append("</i>");
        }

        return sb.ToString().TrimEnd();
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
