using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltipUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Canvas _canvas;        // Screen Space - Overlay
    [SerializeField] private RectTransform _root;   // Panel
    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _desc;
    [SerializeField] private Image _icon;

    private void Awake()
    {
        if (!_canvas) _canvas = GetComponentInParent<Canvas>(true);
        Hide();
    }

    public void Show(ItemDef def, Vector2 screenPos)
    {
        if (!def) { Hide(); return; }
        _title.text = def.displayName;
        _desc.text = BuildDesc(def);
        _icon.enabled = def.icon; _icon.sprite = def.icon;

        _root.gameObject.SetActive(true);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform, screenPos, _canvas.worldCamera, out var local);
        _root.anchoredPosition = local + new Vector2(16, -16);
    }

    public void Hide() => _root.gameObject.SetActive(false);

    private string BuildDesc(ItemDef d)
    {
        // quick, readable stat line
        string s = "";
        if (d.kind == ItemKind.Weapon) s += $"ATK +{Mathf.Max(0, d.baseAttack)}";
        if (d.kind == ItemKind.Armor) s += $"ARM +{Mathf.Max(0, d.baseArmor)}";
        if (d.kind == ItemKind.Consumable) s += d.stackable ? $"Stack x{Mathf.Max(1, d.maxStack)}" : "Consumable";
        if (d.twoHanded) s += (s == "" ? "" : " • ") + "Two-Handed";
        if (d.equipSlot != EquipmentSlotType.None) s += (s == "" ? "" : " • ") + d.equipSlot;
        return s == "" ? "Misc item" : s;
    }
}
